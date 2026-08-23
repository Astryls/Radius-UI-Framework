// Radius UI Framework - Chrome/Spatial.Bands.cs
//
// ADDED 2026-08-19. ADDITIVE ONLY: nothing existing is touched, no value changed.
// New partial file rather than an edit to Spatial.cs, per ARCHITECTURE §13's concurrent-edit
// rule (Spatial gains `partial`, exactly as UIKit and Metrics already did at generation 4).
//
// WHY THIS EXISTS.
// `BottomBand` was forked THREE times before this file: Radius UI - Health Tab
// (Chrome/Spatial.cs:86), Radius UI - Colonist Bar (McbSpatial.cs), and Modern Needs Tab
// (SpatialKit.cs:89) - which ModernColonistBar's 07-ALERT-BANDS.md names as its port source.
// GLOBAL_RULES §9 makes the third occurrence a shared helper, and a visual pattern belongs in
// the framework rather than in a consumer. Radius UI - Needs Tab is the fourth caller and is
// what forced the issue.
//
// THE TRAP THIS SOLVES, and why the naive version is wrong.
// An alert band is welded to the BOTTOM edge of a container: its bottom corners must match the
// container's radius exactly, and its top edge must be a straight cut. Drawing the band rect
// with a rounded atlas does NOT do that -
//
//   * `Widgets.DrawAtlas` clamps its corner slice to HALF the rect. A 38px band drawn with a
//     22px atlas rounds at ~19px, so its corners bulge past the container's 22px radius.
//   * It also rounds the TOP corners, which puts a visible notch where the band meets the
//     content above it.
//
// The fix is to clip, not to shrink: begin a clip at the band rect, then draw a capsule that is
// a full `2 * radius` tall anchored to the band's bottom. The clip discards everything above,
// leaving the container's exact radius on the bottom corners and a clean straight cut on top.
//
// ⚠ NOTHING INTERACTIVE INSIDE THE CLIP. Tooltip and hit rects registered inside a BeginClip
// resolve against clip-local coordinates and land elsewhere on screen. Draw the fill in here;
// register hover and click OUTSIDE, in screen space.

using UnityEngine;
using Verse;

namespace RadiusUI.Framework
{
    public static partial class Spatial
    {
        /// <summary>The suite's raised-island radius - the NOW capsule, the Colonist Bar slab.
        /// Named so <see cref="BottomBand"/>'s default cannot silently drift away from the
        /// container it is supposed to be welded to.</summary>
        public const float RCapsule = 22f;

        private static readonly Texture2D TriTex = MakeTriangle(24);

        /// <summary>
        /// A band welded to the bottom edge of a rounded container: bottom corners at
        /// <paramref name="radius"/>, top edge cut straight.
        ///
        /// <para>Cost: one clip plus one rounded fill. Thread affinity: OnGUI main thread.</para>
        ///
        /// <para><b>Register hover, tooltips and clicks OUTSIDE this call</b>, in screen space -
        /// anything registered inside the clip resolves against clip-local coordinates.</para>
        /// </summary>
        /// <param name="band">The band rect, in screen space. Its height is honoured exactly.</param>
        /// <param name="c">Fill colour. The band is the one element in the suite allowed to be a
        /// solid block of colour, which is why it must stay rare.</param>
        /// <param name="radius">Corner radius of the CONTAINER this band is welded to. Defaults to
        /// <see cref="RCapsule"/>.</param>
        public static void BottomBand(Rect band, Color c, float radius = RCapsule)
        {
            if (!FrameGate.Drawing || band.width < 4f || band.height < 2f) return;
            if (!Rounded) { Widgets.DrawBoxSolid(band, c); return; }

            GUI.BeginClip(band);
            try
            {
                // Clip-local: a full 2r-tall capsule anchored to the band's bottom edge. Anything
                // above the band is discarded by the clip, so only the bottom arc survives.
                float h = radius * 2f;
                CardChrome.Rounded(new Rect(0f, band.height - h, band.width, h), c, radius);
            }
            finally { GUI.EndClip(); }
        }

        /// <summary>
        /// Up-pointing triangle - the alert mark inside a band's severity disc. One draw call,
        /// no allocation. Tinted by <paramref name="c"/>.
        /// </summary>
        public static void Glyph(Rect r, Color c)
        {
            if (!FrameGate.Drawing || r.width < 2f || r.height < 2f) return;
            Color prev = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, TriTex);
            GUI.color = prev;
        }

        /// <summary>Up-pointing triangle inscribed in the texture. Unity's texture origin is
        /// bottom-left, so the apex is authored at the top row index.</summary>
        private static Texture2D MakeTriangle(int n)
        {
            var tex = NewTex(n, "tri");
            var px = new Color32[n * n];
            float half = n * 0.5f;
            for (int y = 0; y < n; y++)
            {
                float v = (y + 0.5f) / n;               // 0 at the base, 1 at the apex
                float halfWidth = half * (1f - v) * 1.02f;
                for (int x = 0; x < n; x++)
                {
                    float dx = Mathf.Abs(x + 0.5f - half);
                    px[y * n + x] = White(halfWidth - dx + 0.5f);
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);   // never read back: drop the CPU copy
            return tex;
        }
    }
}
