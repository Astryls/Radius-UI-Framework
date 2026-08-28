using UnityEngine;
using Verse;

namespace RadiusUI.Framework
{
    /// <summary>
    /// The suite scroll view: vanilla <c>Widgets.BeginScrollView</c> semantics (wheel, drag,
    /// clipping, mouse handling untouched) with a flat, dark scrollbar instead of the tan
    /// Unity one. Consumers must use this instead of raw BeginScrollView so the whole suite
    /// scrolls with one look.
    ///
    /// How it works: GUI.skin's vertical scrollbar styles are swapped for flat ones between
    /// Begin and End. GUI.skin is ONE shared object for the whole game and RimWorld never
    /// reassigns it, so the swap is guarded by a re-entrant depth counter and must always
    /// be unwound - a leaked swap would restyle every other mod's scrollbars for the rest
    /// of the session. If your draw body can throw, wrap it:
    /// <code>
    ///   int d = FlatScroll.Depth;
    ///   FlatScroll.Begin(outRect, ref pos, viewRect);
    ///   try { ...rows... }
    ///   finally { FlatScroll.EndOrUnwind(d); }
    /// </code>
    ///
    /// Contract: OnGUI main thread only. No per-call allocation after first use. Always
    /// reserve <see cref="Metrics.ScrollGutter"/> of width for the bar UNCONDITIONALLY -
    /// sizing content by "is the bar showing" creates a measure/reflow feedback flicker.
    /// </summary>
    // [StaticConstructorOnStartup] is required by Verse's checker, NOT by this code.
    // StaticConstructorOnStartupUtility.ReportProbablyMissingAttributes is purely
    // STRUCTURAL: it warns about any type declaring a static Texture/Material field,
    // without ever looking at whether the field is null or how it is assigned. The
    // textures below are created lazily inside a null check during OnGUI - already main
    // thread, already correct - so this attribute changes no behaviour (there are no
    // static initializers to run) and exists to keep a false-positive warning out of
    // every player's log.
    [StaticConstructorOnStartup]
    public static class FlatScroll
    {
        private static int depth;

        // Saved vanilla styles while a swap is active (restored at depth 0).
        private static GUIStyle? savedBar;
        private static GUIStyle? savedThumb;
        private static GUIStyle? savedUp;
        private static GUIStyle? savedDown;

        // Our flat styles + their backing textures. Textures are runtime-generated solids;
        // they carry HideAndDontSave and are re-validated on every Begin because
        // Resources.UnloadUnusedAssets (new game / load / main menu) can still destroy
        // them - a destroyed texture compares == null through Unity's operator overload.
        private static GUIStyle? flatBar;
        private static GUIStyle? flatThumb;
        private static GUIStyle? noButton;
        private static Texture2D? trackTex;
        private static Texture2D? thumbTex;

        /// <summary>Current swap depth. Snapshot before Begin for exception-safe unwinding.</summary>
        public static int Depth => depth;

        /// <summary>Begin a suite-styled scroll view. Pair with <see cref="End"/>.</summary>
        public static void Begin(Rect outRect, ref Vector2 scrollPosition, Rect viewRect, bool showScrollbars = true)
        {
            Push();
            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect, showScrollbars);
        }

        /// <summary>End the scroll view begun with <see cref="Begin"/>.</summary>
        public static void End()
        {
            Widgets.EndScrollView();
            Pop();
        }

        /// <summary>
        /// Exception-safe scroll view as a <c>using</c> block - the recommended form, because
        /// it is the one a caller cannot forget to unwind:
        /// <code>
        ///   using (FlatScroll.Scope(outRect, ref scrollPos, viewRect))
        ///   {
        ///       // rows; an early return or a throw in here is safe
        ///   }
        /// </code>
        /// Equivalent to capturing <see cref="Depth"/>, calling <see cref="Begin"/>, and
        /// calling <see cref="EndOrUnwind"/> from a <c>finally</c>.
        /// <para>Cost: a stack-only struct, no allocation. Thread affinity: OnGUI main thread.</para>
        /// </summary>
        public static ScrollScope Scope(Rect outRect, ref Vector2 scrollPosition, Rect viewRect,
            bool showScrollbars = true)
        {
            var scope = new ScrollScope(depth);
            Begin(outRect, ref scrollPosition, viewRect, showScrollbars);
            return scope;
        }

        /// <summary>
        /// Disposable returned by <see cref="Scope"/>. Unwinds the <c>GUI.skin</c> style swap
        /// on exit however the block is left - normally, by early return, or by exception.
        /// A leaked swap would restyle every other mod's scrollbars for the rest of the
        /// session, so this is the difference between a bug in one panel and a bug in
        /// everyone's UI.
        /// </summary>
        public readonly struct ScrollScope : System.IDisposable
        {
            private readonly int depthBeforeBegin;

            internal ScrollScope(int depthBeforeBegin) { this.depthBeforeBegin = depthBeforeBegin; }

            /// <summary>Closes the scroll view and unwinds to the captured depth.</summary>
            public void Dispose() => EndOrUnwind(depthBeforeBegin);
        }

        /// <summary>
        /// Exception-safe End: closes the scroll view and unwinds the style swap to the
        /// depth captured before Begin. Call from a finally block.
        /// </summary>
        public static void EndOrUnwind(int depthBeforeBegin)
        {
            if (depth > depthBeforeBegin)
            {
                Widgets.EndScrollView();
                UnwindTo(depthBeforeBegin);
            }
        }

        // ------------------------------------------------------------------ swap internals

        private static void Push()
        {
            EnsureStyles();
            if (depth++ == 0)
            {
                savedBar = GUI.skin.verticalScrollbar;
                savedThumb = GUI.skin.verticalScrollbarThumb;
                savedUp = GUI.skin.verticalScrollbarUpButton;
                savedDown = GUI.skin.verticalScrollbarDownButton;
            }
            // Install every time (a nested vanilla view could have restored them).
            GUI.skin.verticalScrollbar = flatBar;
            GUI.skin.verticalScrollbarThumb = flatThumb;
            GUI.skin.verticalScrollbarUpButton = noButton;
            GUI.skin.verticalScrollbarDownButton = noButton;
        }

        private static void Pop()
        {
            if (depth == 0)
            {
                return; // Unbalanced Pop - ignore rather than corrupt the skin.
            }
            if (--depth == 0)
            {
                Restore();
            }
        }

        private static void UnwindTo(int target)
        {
            if (target < 0)
            {
                target = 0;
            }
            if (depth > target)
            {
                depth = target;
                if (depth == 0)
                {
                    Restore();
                }
            }
        }

        private static void Restore()
        {
            if (savedBar != null) GUI.skin.verticalScrollbar = savedBar;
            if (savedThumb != null) GUI.skin.verticalScrollbarThumb = savedThumb;
            if (savedUp != null) GUI.skin.verticalScrollbarUpButton = savedUp;
            if (savedDown != null) GUI.skin.verticalScrollbarDownButton = savedDown;
        }

        // ------------------------------------------------------------------ style building

        private static void EnsureStyles()
        {
            // Unity's overloaded == reports destroyed textures as null, so this self-heals
            // after any UnloadUnusedAssets sweep.
            if (trackTex == null)
            {
                // Palette token, not a literal: the track is a WashFaint surface, and forking
                // the value here would silently split it from the ladder on a future re-value.
                trackTex = SolidColorMaterials.NewSolidColorTexture(Palette.WashFaint);
                trackTex.hideFlags = HideFlags.HideAndDontSave;
                if (flatBar != null)
                {
                    flatBar.normal.background = trackTex;
                }
            }
            if (thumbTex == null)
            {
                thumbTex = SolidColorMaterials.NewSolidColorTexture(Palette.Surface2);
                thumbTex.hideFlags = HideFlags.HideAndDontSave;
                if (flatThumb != null)
                {
                    ApplyThumbTex();
                }
            }
            if (flatBar == null)
            {
                flatBar = new GUIStyle
                {
                    // Slim gutter (2026-06-10): consumers reserving the old 16px ScrollGutter
                    // simply gain 6px of slack; new layouts reserve ScrollGutterSlim.
                    fixedWidth = Metrics.ScrollGutterSlim,
                    // Track padding insets the thumb to a slim 6px bar centred in the gutter.
                    padding = new RectOffset(2, 2, 0, 0),
                };
                flatBar.normal.background = trackTex;
            }
            if (flatThumb == null)
            {
                flatThumb = new GUIStyle
                {
                    // Vertical padding is the thumb's MINIMUM size floor: Unity sizes the
                    // thumb as proportional + padding.vertical, and a padding-less style
                    // shrinks to 0px (invisible) on very tall content.
                    padding = new RectOffset(0, 0, 12, 12),
                };
                ApplyThumbTex();
            }
            noButton ??= new GUIStyle { fixedWidth = 0f, fixedHeight = 0f };
        }

        private static void ApplyThumbTex()
        {
            if (flatThumb == null)
            {
                return;
            }
            flatThumb.normal.background = thumbTex;
            flatThumb.hover.background = thumbTex;
            flatThumb.active.background = thumbTex;
        }
    }
}
