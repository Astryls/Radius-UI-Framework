// Radius UI Framework - Icons/RadiusIcon.cs
//
// The suite's art is THREE TIERS, not one family, and only tier 1 may be re-themed.
// That fact is enforced here, in the type, rather than by a comment nobody reads:
// a caller passes whatever colour it likes and the tier decides how much of that
// colour survives.
//
// Why it is enforced in the framework rather than at the call site: the Health Tab
// resolved an icon through ONE strategy function that could return a flat-white
// glyph, a shaded beige illustration, or a modded item's own art - and then tinted
// the result. The call site cannot know which it got, so the call site is the wrong
// place to make the decision. The texture has to carry its own tier.

using UnityEngine;
using Verse;

namespace RadiusUI.Framework
{
    /// <summary>How much of a caller's tint an icon is allowed to take.</summary>
    public enum IconTier : byte
    {
        /// <summary>
        /// Flat-white glyph. All information is in the alpha channel, so it takes any tint.
        /// 258 of the library's 268 icons.
        /// </summary>
        Glyph = 0,

        /// <summary>
        /// Rendered illustration with baked shading (<c>Anatomy/</c>, 25 files). Hue-tinting
        /// shaded beige art muddies it into a stain. Alpha only.
        /// </summary>
        Illustration = 1,

        /// <summary>
        /// Semantic marker (<c>Marker/</c>, 8 files) whose baked colour IS its meaning -
        /// blood is red, frost is blue. Re-tinting one to the user's accent destroys the
        /// only thing it communicates. Alpha only.
        /// </summary>
        Marker = 2,

        /// <summary>
        /// Art the framework does not own - a modded implant's <c>uiIcon</c>, a ThingDef
        /// texture. Tintable, because the consumer's existing design language may already
        /// depend on tinting it, and the framework has no standing to forbid that.
        /// </summary>
        Foreign = 3,

        /// <summary>
        /// Saturated flat-colour art in the emoji style (135 files). Its colour IS the icon,
        /// so it takes alpha only - exactly like <see cref="Illustration"/>, but named apart
        /// because the reason differs and the sets are maintained separately.
        /// <para>This tier is what makes the colour half of the library safe. Tinting is
        /// applied by MULTIPLY, so a caller passing <c>Palette.TextDim</c> to a saturated icon
        /// would otherwise get mud - and a caller passing <c>Palette.InkOnAccent</c>
        /// (which is <c>Surface0</c>, near-black) would get a black smudge. Routing these
        /// through alpha-only means an unaudited tint call site degrades to "draws at full
        /// colour, still dims correctly" instead of breaking.</para>
        /// </summary>
        Emoji = 4,
    }

    /// <summary>
    /// A texture plus its art tier. Struct, so passing one allocates nothing.
    /// <para>Draw through <see cref="Draw(Rect, Color)"/> and the tier rule applies itself.
    /// Reach for <see cref="Texture"/> only to hand the raw texture to an engine API that
    /// demands one - doing so opts out of the tier rule.</para>
    /// Thread affinity: main thread (touches GUI).
    /// </summary>
    public readonly struct RadiusIcon
    {
        /// <summary>The underlying texture. Null when the art failed to load.</summary>
        public readonly Texture2D? Texture;

        /// <summary>How much tint this art may take. See <see cref="IconTier"/>.</summary>
        public readonly IconTier Tier;

        public RadiusIcon(Texture2D? texture, IconTier tier)
        {
            Texture = texture;
            Tier = tier;
        }

        /// <summary>True when there is something to draw.</summary>
        public bool Exists => Texture != null;

        /// <summary>Wrap foreign art (a ThingDef uiIcon, a modded implant's texture).</summary>
        public static RadiusIcon Foreign(Texture2D? tex) => new RadiusIcon(tex, IconTier.Foreign);

        /// <summary>Wrap a flat-white glyph the framework did not load - a consumer's own art.</summary>
        public static RadiusIcon Glyph(Texture2D? tex) => new RadiusIcon(tex, IconTier.Glyph);

        /// <summary>Nothing. Draws nothing, reports <c>Exists == false</c>.</summary>
        public static readonly RadiusIcon None = new RadiusIcon(null, IconTier.Glyph);

        /// <summary>
        /// This icon if it exists, otherwise <paramref name="fallback"/>. The replacement for
        /// <c>a ?? b</c> once an icon is a struct rather than a nullable reference - and it
        /// keeps the fallback's OWN tier, which <c>??</c> on raw textures could not do.
        /// </summary>
        public RadiusIcon OrElse(RadiusIcon fallback) => Exists ? this : fallback;

        /// <summary>
        /// The colour this icon will ACTUALLY be drawn in, given a requested tint.
        /// Tier 1 and foreign art take the tint whole; tier 2 and 3 keep their own pixels
        /// and take only the alpha.
        /// <para>Exposed because a caller sometimes needs to know whether its colour landed -
        /// for example to decide that the state colour must go somewhere else on the row.</para>
        /// </summary>
        public Color Resolve(Color requested) =>
            Tier == IconTier.Illustration || Tier == IconTier.Marker || Tier == IconTier.Emoji
                ? new Color(1f, 1f, 1f, requested.a)
                : requested;

        /// <summary>
        /// Draw aspect-fit and centred inside <paramref name="box"/>, never stretched,
        /// in the tint the tier permits. No-op when the texture is missing.
        /// Cost: one <c>GUI.DrawTexture</c>, zero allocation, and NO <c>GUI.color</c> traffic -
        /// the colour rides the DrawTexture overload's parameter (GUI.color's accessors are
        /// native calls billed on every OnGUI pass; the parameter is free - same rule as
        /// CardChrome). Repaint-gated: emits no IMGUI control, so skipping the other passes is
        /// safe and saves the fit math on every non-draw pass.
        /// </summary>
        public void Draw(Rect box, Color tint)
        {
            var tex = Texture;
            if (tex == null || !FrameGate.Drawing) return;

            float tw = tex.width, th = tex.height;
            Rect fit = box;
            if (tw > 0f && th > 0f)
            {
                float sc = Mathf.Min(box.width / tw, box.height / th);
                float w = tw * sc, h = th * sc;
                fit = new Rect(box.x + (box.width - w) * 0.5f, box.y + (box.height - h) * 0.5f, w, h);
            }

            GUI.DrawTexture(fit, tex, ScaleMode.StretchToFill, true, 0f, Resolve(tint), 0f, 0f);
        }

        /// <summary>Draw at full white. Equivalent to <c>Draw(box, Color.white)</c>.</summary>
        public void Draw(Rect box) => Draw(box, Color.white);

        /// <summary>
        /// Draw at an explicit opacity, keeping the art's own colour. This is the honest
        /// call for tier 2 and 3 - it says what it does instead of passing a colour that
        /// will be thrown away.
        /// </summary>
        public void DrawFaded(Rect box, float alpha) => Draw(box, new Color(1f, 1f, 1f, alpha));

        /// <summary>Draw stretched to fill <paramref name="box"/> exactly (backgrounds, wraps).
        /// Same cost contract as <see cref="Draw(Rect, Color)"/>: colour-parameter overload,
        /// Repaint-gated, no GUI.color traffic.</summary>
        public void DrawStretched(Rect box, Color tint)
        {
            var tex = Texture;
            if (tex == null || !FrameGate.Drawing) return;
            GUI.DrawTexture(box, tex, ScaleMode.StretchToFill, true, 0f, Resolve(tint), 0f, 0f);
        }

        /// <summary>Implicit unwrap for engine APIs that demand a raw texture (button helpers, gizmos).</summary>
        public static implicit operator Texture2D?(RadiusIcon i) => i.Texture;
    }
}
