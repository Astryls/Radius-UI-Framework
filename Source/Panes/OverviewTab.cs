// Radius UI Framework - Panes/OverviewTab.cs
//
// ADDED 2026-08-20. ADDITIVE: nothing existing is touched.
//
// Geometry for a Radius UI module that REPLACES a vanilla overview tab (Health, Needs,
// Social, Bio/Character, Gear, Log, Training...). See ARCHITECTURE §17.
//
// WHY THIS IS FRAMEWORK-OWNED.
// The `MaxWindowH` clamp below had been forked THREE times verbatim - Health Tab
// (UI/HealthTab.cs), Needs Tab (UI/NeedsTab.cs), Social Tab (UI/SocialTabDrawer.cs).
// GLOBAL_RULES §9 makes the third occurrence a shared helper. But the real argument is
// stronger than tidiness: the SIZE CONSTANTS drifted while nobody was looking. Social Tab
// transposed its width and height (790x890 against the other two at 890x790) and shipped a
// different default scale, while its own comment stated the goal was to land on the Health
// Tab's footprint. Three private copies of "the standard size" is not a standard.
//
// This file is deliberately NOT in Tokens/: it reads live game state (the inspect pane), and
// Tokens is pure data (§2). The numbers live in Metrics.Tabs; the clamping lives here.
//
// Thread affinity: OnGUI / main thread.

using RimWorld;
using UnityEngine;
using Verse;

namespace RadiusUI.Framework
{
    /// <summary>
    /// The standard footprint for a pane-replacing overview tab, clamped to what the screen
    /// can actually show.
    ///
    /// <para>Use <see cref="Size"/> for the value handed to <c>InspectTabBase.size</c> /
    /// <c>RequestedTabSize</c>. Re-read it every frame - it is a property, not a cached
    /// constant, so a tab resizes live when the player moves a scale slider, when a flyout
    /// opens, or when another mod moves the pane.</para>
    /// </summary>
    public static class OverviewTab
    {
        /// <summary>Height of vanilla's tab-button row above the tab body.</summary>
        private const float TabButtonRowH = 30f;

        /// <summary>Breathing room kept between the tab's top edge and the screen edge.</summary>
        private const float TopBuffer = 20f;

        /// <summary>Vanilla `MainTabWindow_Inspect`'s pane top, used when the live one is
        /// unavailable (main menu, world view, a null UIRoot during load).</summary>
        private const float VanillaPaneTopOffset = 165f;

        /// <summary>
        /// Tallest a pane-anchored tab may be right now.
        ///
        /// <para>A tab is anchored to the inspect pane at <c>top = PaneTopY - 30 - height</c>,
        /// so the limit must come from the LIVE pane top. RimHUD and other pane mods RAISE
        /// <c>PaneTopY</c>, and a static <c>screenHeight</c> offset overflows the top of the
        /// screen on those setups. Note <c>UI.screenHeight</c> is ALREADY divided by
        /// <c>Prefs.UIScale</c>, so never mix it with unscaled constants.</para>
        ///
        /// <para>Fails safe: any exception or non-playing state falls back to vanilla's
        /// offset rather than throwing inside a draw path.</para>
        /// </summary>
        public static float MaxHeight
        {
            get
            {
                float paneTop = UI.screenHeight - VanillaPaneTopOffset;
                try
                {
                    if (Current.ProgramState == ProgramState.Playing
                        && Find.UIRoot is UIRoot_Play
                        && MainButtonDefOf.Inspect?.TabWindow is IInspectPane pane)
                    {
                        paneTop = pane.PaneTopY;
                    }
                }
                catch
                {
                    // Fall through to the vanilla offset. A geometry helper must never be the
                    // thing that throws inside OnGUI - a root-level exception aborts the rest
                    // of the frame's UI, which presents as unrelated panels breaking.
                }
                return paneTop - TabButtonRowH - TopBuffer;
            }
        }

        /// <summary>
        /// Tallest a tab may be when ANOTHER mod hosts its rect (Radius UI - Inspector docks
        /// tabs inside its own panel) rather than the tab being anchored to the pane.
        ///
        /// <para><b>A hosted tab must not use <see cref="MaxHeight"/>.</b> The host sizes its
        /// panel from the tab size we declare, then patches <c>PaneTopY</c> to report that
        /// panel's top. So a hosted tab that clamps against <c>PaneTopY</c> is clamping
        /// against a value derived from its own output: the two chase each other and settle
        /// wherever the animation happens to land, so two tabs with identical constants end up
        /// different heights. Worse, it changes value on the exact frame hosting begins, which
        /// is the flicker. The host caps its own panel and clamps the tab rect to the hole it
        /// provides, so it cannot overflow - all a hosted tab owes it is a height that does not
        /// depend on the pane.</para>
        /// </summary>
        /// <param name="hostChromeH">Screen height the host's own chrome needs above and below
        /// the hosted tab.</param>
        public static float MaxHeightHosted(float hostChromeH) => UI.screenHeight - hostChromeH;

        /// <summary>
        /// Clamp a user's tab-scale setting into the sanctioned range. Route every settings
        /// read through this - a slider whose range disagrees with the stored default lets a
        /// value exist that the UI cannot express, and the first drag snaps it permanently.
        /// </summary>
        public static float Scale(float raw)
            => Mathf.Clamp(raw, Metrics.OverviewTabScaleMin, Metrics.OverviewTabScaleMax);

        /// <summary>Standard width at a given scale, rounded to whole pixels. Every layout site
        /// must use THIS rather than the raw constant - they only agree at scale 1.</summary>
        public static float WidthAt(float scale) => Mathf.Round(Metrics.OverviewTabW * Scale(scale));

        /// <summary>
        /// The standard overview-tab size: <see cref="Metrics.OverviewTabW"/> x
        /// <see cref="Metrics.OverviewTabH"/>, scaled and height-clamped.
        ///
        /// <para>Scaling SHRINKS the logical box so content reflows. Never scale a tab with
        /// <c>GUI.matrix</c> - that fights RimWorld's own UIScale transform and renders the
        /// content at the wrong size relative to the window box at any UI scale but 1.0.</para>
        /// </summary>
        /// <param name="scale">User tab-size multiplier. Clamped internally.</param>
        /// <param name="extraWidth">Width of an open side flyout, already scaled by the caller
        /// if it scales. Added AFTER the standard width so the base footprint stays the
        /// standard one.</param>
        /// <param name="maxHeight">Override for the height clamp. Pass
        /// <see cref="MaxHeightHosted"/> when a host owns the rect; leave null to clamp against
        /// the live pane.</param>
        public static Vector2 Size(float scale = 1f, float extraWidth = 0f, float? maxHeight = null)
        {
            float s = Scale(scale);
            float h = Mathf.Min(Mathf.Round(Metrics.OverviewTabH * s), maxHeight ?? MaxHeight);
            return new Vector2(Mathf.Round(Metrics.OverviewTabW * s) + extraWidth, h);
        }
    }
}
