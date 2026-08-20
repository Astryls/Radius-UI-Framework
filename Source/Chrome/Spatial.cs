// Radius UI Framework - Chrome/Spatial.cs
//
// ADDED generation 4 (2026-08-19). ADDITIVE: nothing existing is touched.
//
// WHY THIS EXISTS, AND WHY NOW.
// CardChrome rides Unity's public rounded-rect DrawTexture overload (§10 F1). That covers fills
// and outlines and nothing else - it cannot draw a drop shadow, a disc, a ring or a capsule.
// Everything in this file is the shape vocabulary CardChrome deliberately does not cover.
//
// §10 deferred `Spatial` from v1 "until the three pilot consumers prove what the API needs",
// because designing against zero real consumers is guesswork. That condition is now met: FOUR
// forked copies exist in the suite (Modern Social Tab, Modern Needs Tab, Colonist Bar, Health Tab)
// and Radius UI - Social Tab is a real consumer whose whole design language is recessed trays and
// raised cards. GLOBAL_RULES §9 makes the third occurrence a shared helper; this is the fourth.
//
// This is a LIFT of Modern Social Tab's Spatial.cs, not a rewrite, so the suite gets one
// implementation rather than a fifth. Its hard-won comments are preserved verbatim where they
// explain a trap, because every one of them cost someone a debugging session.
//
// COST DISCIPLINE. A 9-slice is NINE GUI.DrawTexture calls, so it is for surfaces and for row
// cards (viewport culling caps those at ~12 on screen). Anything drawn per list row uses Pill
// (three calls) or a plain filled rect. A tint at 2-3% alpha has no visible corners, so
// alternating row stripes should stay square.
//
// Thread affinity: OnGUI main thread only.

using UnityEngine;
using Verse;

namespace RadiusUI.Framework
{
    /// <summary>
    /// Rounded surfaces, capsules, discs and neutral elevation - the primitives the suite's
    /// "recessed tray, raised card" depth language needs and RimWorld's widget set does not have.
    ///
    /// <para>Everything here is ONE procedurally generated texture per shape, drawn through
    /// <see cref="Widgets.DrawAtlas"/> (vanilla's 9-slice: corner size = atlas.width/4, clamped to
    /// half the rect, UI-scaling snapped). That gives a true rounded rectangle at any size from a
    /// handful of small textures, tinted by GUI.color, with <b>no shipped art</b> and therefore no
    /// cs-assets manifest entry that could drift.</para>
    ///
    /// <para><see cref="StaticConstructorOnStartupAttribute"/> is mandatory: the class holds static
    /// <see cref="Texture2D"/> fields, and the attribute is what tells RimWorld these are built on
    /// the main thread at load.</para>
    /// </summary>
    [StaticConstructorOnStartup]
    public static partial class Spatial
    {
        // Corner radii matching the suite's spatial scale (xl 22 / lg 18 / md 14 / sm 10 / chip 6).
        private static readonly Texture2D CapsuleTex = MakeRounded(22);
        private static readonly Texture2D SurfaceTex = MakeRounded(18);
        private static readonly Texture2D CardTex = MakeRounded(14);
        private static readonly Texture2D RowTex = MakeRounded(10);
        private static readonly Texture2D ChipTex = MakeRounded(6);
        private static readonly Texture2D ShadowTex = MakeShadow(18, 14);
        private static readonly Texture2D DiscTex = MakeDisc(32);
        private static readonly Texture2D RingTex = MakeRing(48, 0.14f);

        /// <summary>
        /// Escape hatch. If a future Unity or driver combination ever renders the atlas path wrong,
        /// one flag returns every surface to square corners without touching a call site.
        /// </summary>
        public static bool Rounded = true;

        // ---------------------------------------------------------------- draw

        private static void Atlas(Rect r, Color c, Texture2D tex)
        {
            if (!FrameGate.Drawing || r.width < 2f || r.height < 2f) return;
            if (!Rounded) { Widgets.DrawBoxSolid(r, c); return; }
            Color prev = GUI.color;
            GUI.color = c;
            Widgets.DrawAtlas(r, tex);
            GUI.color = prev;
        }

        /// <summary>Raised capsule or tab shell (22px corners). 9 draw calls.</summary>
        public static void Capsule(Rect r, Color c) => Atlas(r, c, CapsuleTex);

        /// <summary>Panel surface or recessed well (18px corners). 9 draw calls.</summary>
        public static void Surface(Rect r, Color c) => Atlas(r, c, SurfaceTex);

        /// <summary>Card (14px corners) - dossiers, stat tiles, overview cards. 9 draw calls.</summary>
        public static void Card(Rect r, Color c) => Atlas(r, c, CardTex);

        /// <summary>Row plate or small control (10px corners). 9 draw calls.</summary>
        public static void RowPlate(Rect r, Color c) => Atlas(r, c, RowTex);

        /// <summary>Icon chip (6px corners) - the squircle behind a small tag. 9 draw calls.</summary>
        public static void Chip(Rect r, Color c) => Atlas(r, c, ChipTex);

        /// <summary>
        /// A recessed tray: the surface fill plus a soft inner shade at the top edge, which is what
        /// makes content inside it read as sitting IN something rather than on it. Pairs with
        /// <see cref="Elevate"/> - a suite screen is trays and lifted cards, never outlines.
        /// </summary>
        public static void Well(Rect r, Color fill)
        {
            Surface(r, fill);
            if (!FrameGate.Drawing || !Rounded || r.height < 6f) return;
            // Two hairlines rather than a gradient texture: at these alphas the corners are
            // invisible, so square strips cost 2 draw calls and read identically.
            Widgets.DrawBoxSolid(new Rect(r.x + 6f, r.y, r.width - 12f, 1f),
                new Color(0f, 0f, 0f, 0.35f));
            Widgets.DrawBoxSolid(new Rect(r.x + 6f, r.y + 1f, r.width - 12f, 1f),
                new Color(0f, 0f, 0f, 0.16f));
        }

        // Half-disc sample windows. Unity's texture origin is bottom-left and IMGUI does NOT flip on
        // sample, so texture-Y 0.5..1 is the TOP of the drawn rect.
        private static readonly Rect UvLeft = new Rect(0f, 0f, 0.5f, 1f);
        private static readonly Rect UvRight = new Rect(0.5f, 0f, 0.5f, 1f);
        private static readonly Rect UvTop = new Rect(0f, 0.5f, 1f, 0.5f);
        private static readonly Rect UvBottom = new Rect(0f, 0f, 1f, 0.5f);

        /// <summary>
        /// Fully rounded capsule: two caps and a bar, THREE draw calls instead of the nine a 9-slice
        /// would cost. Progress bars and state strips are the most-drawn rounded things in the suite.
        ///
        /// <para>The caps are HALF discs sampled with texcoords, not whole discs sitting under the
        /// middle rect. That matters because the naive version double-draws the inner half of each
        /// cap: with an opaque colour you never notice, but with ANY alpha below 1 the body
        /// composites twice and renders visibly darker than its own end caps. On a short bar the two
        /// caps and the doubled body then read as three separate lumps.</para>
        ///
        /// <para>Orientation-aware: a 3x30 state strip caps top and bottom, not left and right.</para>
        /// </summary>
        public static void Pill(Rect r, Color c)
        {
            if (!FrameGate.Drawing || r.width < 1f || r.height < 1f) return;
            if (!Rounded) { Widgets.DrawBoxSolid(r, c); return; }

            if (r.width >= r.height)
            {
                float d = r.height, h = d * 0.5f;
                if (r.width <= d + 0.5f) { Dot(r, c); return; }
                Color prev = GUI.color;
                GUI.color = c;
                GUI.DrawTextureWithTexCoords(new Rect(r.x, r.y, h, d), DiscTex, UvLeft);
                GUI.DrawTextureWithTexCoords(new Rect(r.xMax - h, r.y, h, d), DiscTex, UvRight);
                GUI.color = prev;
                Widgets.DrawBoxSolid(new Rect(r.x + h, r.y, r.width - d, d), c);
            }
            else
            {
                float d = r.width, h = d * 0.5f;
                if (r.height <= d + 0.5f) { Dot(r, c); return; }
                Color prev = GUI.color;
                GUI.color = c;
                GUI.DrawTextureWithTexCoords(new Rect(r.x, r.y, d, h), DiscTex, UvTop);
                GUI.DrawTextureWithTexCoords(new Rect(r.x, r.yMax - h, d, h), DiscTex, UvBottom);
                GUI.color = prev;
                Widgets.DrawBoxSolid(new Rect(r.x, r.y + h, d, r.height - d), c);
            }
        }

        /// <summary>Filled circle - ONE draw call, so this is what rows and markers use.</summary>
        public static void Dot(Rect r, Color c)
        {
            if (!FrameGate.Drawing || r.width < 0.5f || r.height < 0.5f) return;
            Color prev = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, Rounded ? DiscTex : BaseContent.WhiteTex);
            GUI.color = prev;
        }

        /// <summary>
        /// Hollow circle - one draw call. Stroke thickness is baked at 14% of the radius, which
        /// reads as a hairline at 26px and as a rule at 200px. Wanted by radial layouts.
        /// </summary>
        public static void Ring(Rect r, Color c)
        {
            if (!FrameGate.Drawing || r.width < 2f || r.height < 2f) return;
            Color prev = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, RingTex);
            GUI.color = prev;
        }

        /// <summary>
        /// Neutral drop shadow under a raised surface. The blur is baked into the atlas, so an
        /// elevation is 9 draw calls rather than a stack of offset rectangles. No coloured halos:
        /// the suite's elevation is always plain black alpha.
        ///
        /// <para>A shadow is the one thing here that draws OUTSIDE its own surface, so it is the one
        /// thing that can escape the panel it belongs to. IMGUI has no cheap clip that is safe for
        /// tooltips, so the spill is CLAMPED instead - the 9-slice just scales its edge slices and
        /// the falloff still reads correctly. Pass the panel rect as <paramref name="clamp"/>.</para>
        /// </summary>
        public static void Elevate(Rect r, Rect clamp, float spread = 14f, float alpha = 0.42f)
        {
            if (!Rounded || !FrameGate.Drawing) return;
            Rect s = new Rect(r.x - spread, r.y - spread + spread * 0.42f,
                              r.width + spread * 2f, r.height + spread * 2f);
            float x0 = Mathf.Max(s.x, clamp.x), x1 = Mathf.Min(s.xMax, clamp.xMax);
            float y0 = Mathf.Max(s.y, clamp.y), y1 = Mathf.Min(s.yMax, clamp.yMax);
            if (x1 - x0 < 2f || y1 - y0 < 2f) return;
            Atlas(new Rect(x0, y0, x1 - x0, y1 - y0), new Color(0f, 0f, 0f, alpha), ShadowTex);
        }

        // ---------------------------------------------------------------- generation

        private static Texture2D NewTex(int n, string tag)
        {
            var t = new Texture2D(n, n, TextureFormat.RGBA32, false);
            t.filterMode = FilterMode.Bilinear;
            t.wrapMode = TextureWrapMode.Clamp;
            // Without HideAndDontSave, Resources.UnloadUnusedAssets (fired on every new game, save
            // load and return to menu) destroys these - a texture reachable only from a static field
            // is not traceable by the sweep, and the surfaces silently stop drawing.
            t.hideFlags = HideFlags.HideAndDontSave;
            t.name = "RadiusUI_Spatial_" + tag + "_" + n;
            return t;
        }

        private static Color32 White(float a)
            => new Color32(255, 255, 255, (byte)Mathf.RoundToInt(Mathf.Clamp01(a) * 255f));

        /// <summary>
        /// White rounded rect, sized so DrawAtlas's quarter-width corner slice is exactly the
        /// requested radius (n = 4r). Coverage is antialiased from the signed distance, so the arc
        /// stays clean at every scale the 9-slice stretches to.
        ///
        /// <para>Do NOT be tempted to raise the radius toward n/2: at rx = width/2 the source becomes
        /// a circle, and the edge strips (UV 0.25-0.75) then sample curved, half-transparent pixels
        /// and stretch them across the whole span, smearing the edges.</para>
        /// </summary>
        private static Texture2D MakeRounded(int r)
        {
            int n = r * 4;
            var tex = NewTex(n, "round");
            var px = new Color32[n * n];
            float half = n * 0.5f;
            float flat = half - r;
            for (int y = 0; y < n; y++)
            {
                float dy = Mathf.Abs(y + 0.5f - half);
                float qy = Mathf.Max(dy - flat, 0f);
                for (int x = 0; x < n; x++)
                {
                    float dx = Mathf.Abs(x + 0.5f - half);
                    float qx = Mathf.Max(dx - flat, 0f);
                    float d = Mathf.Sqrt(qx * qx + qy * qy) - r;
                    px[y * n + x] = White(0.5f - d);
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);   // never read back: drop the CPU copy
            return tex;
        }

        /// <summary>Soft shadow for a rounded rect of radius r, with the blur baked in.</summary>
        private static Texture2D MakeShadow(int r, int blur)
        {
            int c = r + blur;
            int n = c * 4;
            var tex = NewTex(n, "shadow");
            var px = new Color32[n * n];
            float half = n * 0.5f;
            float flat = half - blur - r;
            for (int y = 0; y < n; y++)
            {
                float dy = Mathf.Abs(y + 0.5f - half);
                float qy = Mathf.Max(dy - flat, 0f);
                for (int x = 0; x < n; x++)
                {
                    float dx = Mathf.Abs(x + 0.5f - half);
                    float qx = Mathf.Max(dx - flat, 0f);
                    float d = Mathf.Sqrt(qx * qx + qy * qy) - r;
                    float a = d <= 0f ? 1f : 1f - Mathf.Clamp01(d / blur);
                    px[y * n + x] = White(a * a);   // squared falloff reads softer
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return tex;
        }

        private static Texture2D MakeDisc(int n)
        {
            var tex = NewTex(n, "disc");
            var px = new Color32[n * n];
            float half = n * 0.5f;
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = x + 0.5f - half, dy = y + 0.5f - half;
                    float d = Mathf.Sqrt(dx * dx + dy * dy) - (half - 0.5f);
                    px[y * n + x] = White(0.5f - d);
                }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return tex;
        }

        /// <summary>Hollow circle. <paramref name="thickness"/> is a fraction of the radius.</summary>
        private static Texture2D MakeRing(int n, float thickness)
        {
            var tex = NewTex(n, "ring");
            var px = new Color32[n * n];
            float half = n * 0.5f;
            float outer = half - 0.5f;
            float inner = outer * (1f - thickness);
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = x + 0.5f - half, dy = y + 0.5f - half;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    // coverage of the annulus: inside the outer edge AND outside the inner edge
                    float a = Mathf.Min(0.5f - (d - outer), 0.5f + (d - inner));
                    px[y * n + x] = White(a);
                }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return tex;
        }
    }
}
