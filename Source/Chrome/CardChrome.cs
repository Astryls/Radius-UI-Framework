using System;
using UnityEngine;
using Verse;

namespace RadiusUI.Framework
{
    /// <summary>
    /// Which corners of a plate are rounded, for the <see cref="CardChrome.RoundedCorners"/>
    /// overload that takes this instead of four positional floats.
    ///
    /// <para>Four bare floats in (tl, tr, br, bl) order are easy to transpose silently - the
    /// plate still draws, just wrong on one corner - so anything fusing flush against a
    /// neighbour should name its corners instead.</para>
    /// </summary>
    [Flags]
    public enum Corner
    {
        None        = 0,
        TopLeft     = 1,
        TopRight    = 2,
        BottomRight = 4,
        BottomLeft  = 8,
        All         = TopLeft | TopRight | BottomRight | BottomLeft,

        /// <summary>Header fused to the body below it.</summary>
        TopOnly     = TopLeft | TopRight,
        /// <summary>Footer fused to the body above it.</summary>
        BottomOnly  = BottomLeft | BottomRight,
        /// <summary>Left column fused to the pane on its right.</summary>
        LeftOnly    = TopLeft | BottomLeft,
        /// <summary>Right column fused to the pane on its left.</summary>
        RightOnly   = TopRight | BottomRight,

        /// <summary>Anchored to the bottom-right screen corner: only the inward corner rounds.</summary>
        AnchoredBR  = TopLeft,
        /// <summary>Anchored to the bottom-left screen corner.</summary>
        AnchoredBL  = TopRight,
        /// <summary>Anchored to the top-right screen corner.</summary>
        AnchoredTR  = BottomLeft,
        /// <summary>Anchored to the top-left screen corner.</summary>
        AnchoredTL  = BottomRight,
    }

    /// <summary>
    /// The suite's flat-2.0 drawing primitives: solid fills, rounded plates, outlines,
    /// pills and the standard card treatment. Consumers draw every surface through this
    /// class - never hand-rolled DrawBoxSolid chrome - so the whole suite restyles from
    /// one place.
    ///
    /// Implementation notes (why it looks the way it does):
    ///  - Rounding uses Unity's per-corner <c>GUI.DrawTexture</c> overload (borderRadiuses),
    ///    available on RimWorld 1.5/1.6's Unity. ONE quad per plate: no 9-slice art, no
    ///    seams at any UI scale, nothing for Resources.UnloadUnusedAssets to destroy.
    ///  - Fills use the colour-parameter overload instead of GUI.color round-trips: GUI.color's
    ///    accessors are native calls billed on every OnGUI pass, the parameter is free.
    ///  - Every draw is gated on EventType.Repaint. These helpers emit NO IMGUI controls,
    ///    so skipping Layout/input passes is safe and keeps control counts stable.
    ///
    /// Contract: zero allocation per call; main thread / OnGUI only; radius arguments are
    /// BASE radii which are multiplied by <see cref="RadiusTheme.RadiusScale"/> internally.
    /// If the rounded overload is unavailable at runtime, everything degrades to square
    /// fills once, with a single warning.
    ///
    /// <para>Carries <c>[StaticConstructorOnStartup]</c> because of the static
    /// <c>cornerTex</c> field below: RimWorld's startup reflection scan flags any type with a
    /// static <c>Texture2D</c> and logs "probably needs a StaticConstructorOnStartup
    /// attribute" into every player's log. The attribute alone silences it - no static
    /// constructor is required, and the lazy build in <c>CornerTex()</c> is unchanged. The
    /// warning is attributed to [RimWorld] rather than to us, which is why it survived from
    /// gen 12 to gen 13 unnoticed. Same reason <see cref="Spatial"/> and
    /// <c>FlatScroll</c> carry it.</para>
    /// </summary>
    [StaticConstructorOnStartup]
    public static class CardChrome
    {
        /// <summary>Latch: flips false forever if the rounded GUI.DrawTexture overload throws.</summary>
        private static bool roundedAvailable = true;

        /// <summary>
        /// False once the rounded overload has thrown and the suite has fallen back to square
        /// corners for the session. Read-only and diagnostic: consumers should not branch on
        /// this (every helper here already degrades on its own), but a settings screen or a bug
        /// report can say which path is live. Cost: field read.
        /// </summary>
        public static bool RoundingAvailable => roundedAvailable;

        /// <summary>
        /// Draw-pass gate. Delegates to <see cref="FrameGate.Drawing"/> rather than re-testing
        /// <c>Event.current.type</c> locally: one idiom for one concept across the library
        /// (GLOBAL_RULES §9 - RadiusIcon already routes through FrameGate, and a second
        /// open-coded copy here is exactly the drift the Perf layer was extracted to end).
        /// </summary>
        private static bool RepaintNow => FrameGate.Drawing;

        // ------------------------------------------------------------------ fills

        /// <summary>
        /// Solid square fill. The cheapest primitive: one draw call, no GUI.color traffic.
        /// Use for hairlines, dividers and faint washes where corners are invisible anyway.
        /// </summary>
        public static void Fill(Rect rect, Color color)
        {
            if (!RepaintNow)
            {
                return;
            }
            GUI.DrawTexture(rect, BaseContent.WhiteTex, ScaleMode.StretchToFill, true, 0f, color, 0f, 0f);
        }

        /// <summary>
        /// Rounded solid plate. <paramref name="radius"/> is the BASE radius (see Metrics);
        /// the user's roundness setting scales it, and it is clamped to half the short side
        /// (larger values render garbage in the Unity overload, so we never pass them).
        /// </summary>
        public static void Rounded(Rect rect, Color color, float radius)
        {
            if (!RepaintNow)
            {
                return;
            }
            float r = EffectiveRadius(rect, radius);
            if (r < 0.5f || !roundedAvailable)
            {
                GUI.DrawTexture(rect, BaseContent.WhiteTex, ScaleMode.StretchToFill, true, 0f, color, 0f, 0f);
                return;
            }
            try
            {
                GUI.DrawTexture(rect, BaseContent.WhiteTex, ScaleMode.StretchToFill, true, 0f, color,
                    Vector4.zero, new Vector4(r, r, r, r));
            }
            catch (System.Exception e)
            {
                DisableRounding(e);
                GUI.DrawTexture(rect, BaseContent.WhiteTex, ScaleMode.StretchToFill, true, 0f, color, 0f, 0f);
            }
        }

        /// <summary>
        /// Rounded plate with independent corners, order (topLeft, topRight, bottomRight,
        /// bottomLeft). For plates that fuse flush against a neighbour (e.g. a banner sitting
        /// on top of a card rounds only its top corners).
        /// </summary>
        public static void RoundedCorners(Rect rect, Color color, float tl, float tr, float br, float bl)
        {
            if (!RepaintNow)
            {
                return;
            }
            if (!roundedAvailable)
            {
                GUI.DrawTexture(rect, BaseContent.WhiteTex, ScaleMode.StretchToFill, true, 0f, color, 0f, 0f);
                return;
            }
            float cap = Mathf.Min(rect.width, rect.height) * 0.5f;
            float s = RadiusTheme.RadiusScale;
            try
            {
                GUI.DrawTexture(rect, BaseContent.WhiteTex, ScaleMode.StretchToFill, true, 0f, color,
                    Vector4.zero,
                    new Vector4(Mathf.Min(tl * s, cap), Mathf.Min(tr * s, cap),
                                Mathf.Min(br * s, cap), Mathf.Min(bl * s, cap)));
            }
            catch (System.Exception e)
            {
                DisableRounding(e);
                GUI.DrawTexture(rect, BaseContent.WhiteTex, ScaleMode.StretchToFill, true, 0f, color, 0f, 0f);
            }
        }

        /// <summary>
        /// Rounded plate with named corners - the readable form of
        /// <see cref="RoundedCorners(Rect, Color, float, float, float, float)"/>. Corners not
        /// listed in <paramref name="corners"/> are square.
        /// <paramref name="radius"/> is a BASE radius; the theme scale is applied downstream.
        /// </summary>
        public static void RoundedCorners(Rect rect, Color color, float radius, Corner corners)
        {
            RoundedCorners(rect, color,
                (corners & Corner.TopLeft)     != 0 ? radius : 0f,
                (corners & Corner.TopRight)    != 0 ? radius : 0f,
                (corners & Corner.BottomRight) != 0 ? radius : 0f,
                (corners & Corner.BottomLeft)  != 0 ? radius : 0f);
        }

        /// <summary>
        /// Rounded IMAGE plate: a real texture (a baked world map, a faction atlas, a live map
        /// camera feed) drawn so it sits INSIDE the suite's corner radius, instead of poking
        /// square corners out through a rounded outline.
        ///
        /// <para>WHY THIS IS NOT Unity's rounded overload. That overload only honours
        /// <c>borderRadiuses</c> on the direct blit path: ScaleAndCrop and ScaleToFit route
        /// through an internal BeginGroup + recursive draw that discards the radii silently,
        /// so it can only round a texture that is ALREADY at the rect's exact aspect. Half the
        /// suite's image sources are square bakes shown in wide slots (WorldSnapshot is a
        /// square Texture2D; the quest location card is about 2:1), and stretching those to fit
        /// a rounded blit would distort the map rather than crop it.</para>
        ///
        /// <para>So the image is drawn with whatever <paramref name="scale"/> is correct for it
        /// (ScaleAndCrop by default - crop, never squash), and the corners are then MASKED with
        /// <paramref name="backdrop"/>, the colour of the plate the image is sitting on. Works
        /// for any aspect, any scale mode, and does not depend on the Unity overload at all.</para>
        /// </summary>
        public static void Image(Rect rect, Texture image, float radius, Color backdrop,
            ScaleMode scale = ScaleMode.ScaleAndCrop)
        {
            if (!RepaintNow || image == null)
            {
                return;
            }
            GUI.DrawTexture(rect, image, scale);
            MaskCorners(rect, backdrop, radius);
        }

        /// <summary>
        /// Paint the four square corners of <paramref name="rect"/> out in
        /// <paramref name="backdrop"/>, leaving a rounded shape behind. The general way to fit
        /// ANY square-cornered content (a texture, a clipped group, another mod's draw) into
        /// the suite's silhouette.
        ///
        /// <para>One shared quarter-disc mask, drawn four times through texCoords flips, so
        /// this is four draw calls off one 64px texture rather than a per-radius bake.</para>
        /// </summary>
        public static void MaskCorners(Rect rect, Color backdrop, float radius)
        {
            if (!RepaintNow)
            {
                return;
            }
            float r = EffectiveRadius(rect, radius);
            if (r < 0.5f)
            {
                return;
            }
            Texture2D mask = CornerMask();
            Color prev = GUI.color;
            GUI.color = backdrop;
            // texCoords flips put the mask's opaque wedge on the correct corner. The wedge
            // lives at the texture's bottom-left, so bottom-left is the unflipped case.
            GUI.DrawTextureWithTexCoords(new Rect(rect.x, rect.yMax - r, r, r), mask,
                new Rect(0f, 0f, 1f, 1f));
            GUI.DrawTextureWithTexCoords(new Rect(rect.x, rect.y, r, r), mask,
                new Rect(0f, 1f, 1f, -1f));
            GUI.DrawTextureWithTexCoords(new Rect(rect.xMax - r, rect.yMax - r, r, r), mask,
                new Rect(1f, 0f, -1f, 1f));
            GUI.DrawTextureWithTexCoords(new Rect(rect.xMax - r, rect.y, r, r), mask,
                new Rect(1f, 1f, -1f, -1f));
            GUI.color = prev;
        }

        /// <summary>
        /// Rounded outline (frame only, interior untouched). Standard hairline is width 1
        /// in <see cref="Palette.Border"/>. Falls back to four square edge fills if the
        /// rounded overload is unavailable.
        /// </summary>
        public static void Outline(Rect rect, Color color, float width, float radius)
        {
            if (!RepaintNow)
            {
                return;
            }
            float r = EffectiveRadius(rect, radius);
            if (roundedAvailable && r >= 0.5f)
            {
                try
                {
                    GUI.DrawTexture(rect, BaseContent.WhiteTex, ScaleMode.StretchToFill, true, 0f, color,
                        new Vector4(width, width, width, width), new Vector4(r, r, r, r));
                    return;
                }
                catch (System.Exception e)
                {
                    DisableRounding(e);
                }
            }
            // Square fallback: four edge strips (still Repaint-gated, still no GUI.color).
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, width), BaseContent.WhiteTex, ScaleMode.StretchToFill, true, 0f, color, 0f, 0f);
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - width, rect.width, width), BaseContent.WhiteTex, ScaleMode.StretchToFill, true, 0f, color, 0f, 0f);
            GUI.DrawTexture(new Rect(rect.x, rect.y + width, width, rect.height - width * 2f), BaseContent.WhiteTex, ScaleMode.StretchToFill, true, 0f, color, 0f, 0f);
            GUI.DrawTexture(new Rect(rect.xMax - width, rect.y + width, width, rect.height - width * 2f), BaseContent.WhiteTex, ScaleMode.StretchToFill, true, 0f, color, 0f, 0f);
        }

        // ------------------------------------------------------------------ composites

        /// <summary>
        /// The standard suite card: Surface plate, optional hover wash, hairline border,
        /// all at <see cref="Metrics.RadiusCard"/>. This is THE card - do not hand-roll one.
        /// </summary>
        public static void Card(Rect rect, bool hovered = false)
        {
            Rounded(rect, Palette.Surface, Metrics.RadiusCard);
            if (hovered)
            {
                Rounded(rect, Palette.HoverWash, Metrics.RadiusCard);
            }
            Outline(rect, Palette.Border, 1f, Metrics.RadiusCard);
        }

        /// <summary>Card with a custom plate colour (e.g. Surface2 for raised, BGL for recessed).</summary>
        public static void Card(Rect rect, Color plate, bool hovered = false)
        {
            Rounded(rect, plate, Metrics.RadiusCard);
            if (hovered)
            {
                Rounded(rect, Palette.HoverWash, Metrics.RadiusCard);
            }
            Outline(rect, Palette.Border, 1f, Metrics.RadiusCard);
        }

        /// <summary>Fully-rounded pill (radius = half height). Tags, chips, toasts, count badges.</summary>
        public static void Pill(Rect rect, Color color)
        {
            Rounded(rect, color, rect.height * 0.5f / Mathf.Max(0.0001f, RadiusTheme.RadiusScale));
            // Note: divide out the theme scale so pills stay fully round even at low
            // roundness settings - a half-round "pill" reads as a broken button.
        }

        /// <summary>Hover wash over any plate, matching the plate's radius class.</summary>
        public static void Hover(Rect rect, float radius = Metrics.RadiusCard)
        {
            Rounded(rect, Palette.HoverWash, radius);
        }

        /// <summary>Selected/pressed wash over any plate.</summary>
        public static void Active(Rect rect, float radius = Metrics.RadiusCard)
        {
            Rounded(rect, Palette.ActiveWash, radius);
        }

        // ------------------------------------------------------------------ internals

        // The quarter-disc corner mask, built once. Opaque OUTSIDE the disc (the bit that gets
        // painted over with the backdrop) and transparent inside it, with a one-pixel feather
        // so the arc is not stair-stepped. The wedge sits at the texture's bottom-left corner;
        // MaskCorners flips texCoords to reach the other three.
        private const int CornerTexSize = 64;
        private static Texture2D? cornerTex;

        private static Texture2D CornerMask()
        {
            if (cornerTex != null)
            {
                return cornerTex;   // Unity's == also catches a destroyed texture, so this rebuilds
            }
            var t = new Texture2D(CornerTexSize, CornerTexSize, TextureFormat.RGBA32, false)
            {
                name = "RadiusUI_CornerMask",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            var px = new Color32[CornerTexSize * CornerTexSize];
            const float r = CornerTexSize;
            for (int y = 0; y < CornerTexSize; y++)
            {
                for (int x = 0; x < CornerTexSize; x++)
                {
                    float dx = x + 0.5f - r;
                    float dy = y + 0.5f - r;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(d - r + 0.5f);
                    px[y * CornerTexSize + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            }
            t.SetPixels32(px);
            t.Apply(updateMipmaps: false, makeNoLongerReadable: false);
            cornerTex = t;
            return t;
        }

        private static float EffectiveRadius(Rect rect, float baseRadius)
        {
            float r = baseRadius * RadiusTheme.RadiusScale;
            return Mathf.Min(r, Mathf.Min(rect.width, rect.height) * 0.5f);
        }

        private static void DisableRounding(System.Exception e)
        {
            if (roundedAvailable)
            {
                roundedAvailable = false;
                Log.Warning("[Radius UI] Rounded-corner GUI.DrawTexture overload unavailable (" +
                            e.GetType().Name + "). Falling back to square corners for this session.");
            }
        }
    }
}
