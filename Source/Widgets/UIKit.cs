// Radius UI Framework - Widgets/UIKit.cs
//
// The suite's interactive widget vocabulary: buttons, icon buttons, tag pills, section
// bars. PERFORMANCE_PLAYBOOK Part C names UIKit as a shared framework piece, and
// GLOBAL_RULES §9 forces the issue - the Quest Menu had grown a mod-local button kit and
// every other consumer was going to grow its own, which is the exact fork the suite exists
// to prevent. Promoted here 2026-08-18 from Quest Menu's QMKit.
//
// NIELSEN #5 IS ENCODED IN THE SIGNATURE, not left to the call site: Button takes
// `enabled` AND `tip`, draws the disabled state itself, keeps the tooltip hoverable while
// disabled, and swallows the click. A caller therefore cannot produce the "enabled button
// that rejects you afterwards" pattern the house rules ban - the cheapest way to make the
// right thing the default is to make the wrong thing unexpressible.
//
// Thread affinity: OnGUI main thread only. No per-call allocation (labels excepted).

using UnityEngine;
using Verse;

namespace RadiusUI.Framework
{
    /// <summary>Visual weight of a <see cref="UIKit.Button"/>.</summary>
    public enum ButtonStyle
    {
        /// <summary>Accent-filled. The one affirmative action on a surface - never two.</summary>
        Primary = 0,

        /// <summary>Raised neutral plate. Secondary actions.</summary>
        Solid = 1,

        /// <summary>Text only until hovered. Tertiary/dismissive actions.</summary>
        Ghost = 2,
    }

    public static class UIKit
    {
        /// <summary>
        /// Suite button. Returns true only on a real click of an ENABLED button.
        ///
        /// <para>Disabled buttons stay hover-inspectable and swallow the click, so
        /// <paramref name="tip"/> must carry the REASON when <paramref name="enabled"/> is
        /// false (Nielsen #5: prevent the error, explain it in place; never enable-then-reject).</para>
        ///
        /// Cost: up to 3 draws plus one label; one IMGUI control (stable across passes -
        /// the control is emitted whether enabled or not, so control ids never shift).
        /// </summary>
        public static bool Button(Rect r, string label, ButtonStyle style = ButtonStyle.Solid,
            bool enabled = true, string? tip = null)
        {
            bool hover = Mouse.IsOver(r);
            switch (style)
            {
                case ButtonStyle.Primary:
                {
                    Color fill = RadiusTheme.Accent;
                    if (!enabled)
                    {
                        fill.a = 0.40f;
                    }
                    CardChrome.Rounded(r, fill, Metrics.RadiusChip);
                    if (hover && enabled)
                    {
                        CardChrome.Hover(r, Metrics.RadiusChip);
                    }
                    Color ink = Palette.InkOnAccent;
                    if (!enabled)
                    {
                        ink.a = 0.75f;
                    }
                    RadiusFont.Label(r, label, GameFont.Small, heading: true, color: ink,
                        anchor: TextAnchor.MiddleCenter, wrap: false);
                    break;
                }
                case ButtonStyle.Solid:
                {
                    CardChrome.Rounded(r, Palette.Surface2, Metrics.RadiusChip);
                    if (hover && enabled)
                    {
                        CardChrome.Hover(r, Metrics.RadiusChip);
                    }
                    CardChrome.Outline(r, Palette.Border, 1f, Metrics.RadiusChip);
                    RadiusFont.Label(r, label, GameFont.Small, heading: false,
                        color: enabled ? Palette.Ink : Palette.TextDim,
                        anchor: TextAnchor.MiddleCenter, wrap: false);
                    break;
                }
                default:
                {
                    if (hover && enabled)
                    {
                        CardChrome.Hover(r, Metrics.RadiusChip);
                    }
                    RadiusFont.Label(r, label, GameFont.Small, heading: false,
                        color: enabled ? Palette.TextDim : Palette.TextFaint,
                        anchor: TextAnchor.MiddleCenter, wrap: false);
                    break;
                }
            }
            if (tip != null && tip.Length > 0)
            {
                TooltipHandler.TipRegion(r, tip);
            }
            return Widgets.ButtonInvisible(r) && enabled;
        }

        /// <summary>
        /// Square icon button on a raised plate (titlebar controls, row affordances).
        /// The glyph brightens on hover so the control reads as live without a second colour.
        /// </summary>
        public static bool IconButton(Rect r, RadiusIcon icon, string? tip = null)
        {
            bool hover = Mouse.IsOver(r);
            CardChrome.Rounded(r, Palette.Surface2, Metrics.RadiusChip);
            if (hover)
            {
                CardChrome.Hover(r, Metrics.RadiusChip);
            }
            icon.Draw(r.ContractedBy(7f), hover ? Palette.Ink : Palette.TextDim);
            if (tip != null && tip.Length > 0)
            {
                TooltipHandler.TipRegion(r, tip);
            }
            return Widgets.ButtonInvisible(r);
        }

        /// <summary>
        /// Rounded tag pill with an optional leading glyph, drawn at (<paramref name="x"/>,
        /// <paramref name="y"/>). Returns the horizontal advance INCLUDING the trailing gap,
        /// so a caller lays a row out as <c>x += UIKit.TagPill(x, y, ...)</c>.
        ///
        /// <para>Non-interactive by design: a pill that looked clickable but was not tested
        /// as one is a signifier lie. Wrap it in your own hit test if you need a click.</para>
        /// </summary>
        public static float TagPill(float x, float y, string label, Color color, RadiusIcon? icon = null)
        {
            float textW = RadiusFont.Size(label).x;
            float iconW = icon.HasValue ? 16f : 0f;
            var r = new Rect(x, y, textW + iconW + 20f, PillH);
            CardChrome.Pill(r, Palette.Surface2);
            Color line = color;
            line.a = 0.45f;
            CardChrome.Outline(r, line, 1f, r.height * 0.5f);
            float tx = r.x + 10f;
            if (icon.HasValue)
            {
                icon.Value.Draw(new Rect(tx, r.y + 5f, 14f, 14f), color);
                tx += iconW;
            }
            RadiusFont.Label(new Rect(tx, r.y, textW + 2f, r.height), label,
                color: color, anchor: TextAnchor.MiddleLeft, wrap: false);
            return r.width + Metrics.Space8;
        }

        /// <summary>Standard tag pill height. Sized for GameFont.Small without clipping.</summary>
        public const float PillH = 24f;

        /// <summary>
        /// Recessed section/group bar for feeds and lists: sentence-case label left, optional
        /// count or fold affordance right.
        ///
        /// <para>Sentence case is not a suggestion here - the suite banned ALL-CAPS section
        /// headers (they read as a dashboard affectation next to vanilla). Pass
        /// "Expiring soon", never "EXPIRING SOON".</para>
        /// Draw-only; add your own <c>ButtonInvisible</c> over the same rect to make it fold.
        /// </summary>
        public static void SectionBar(Rect r, string label, string? right = null)
        {
            CardChrome.Fill(r, Palette.Surface);
            RadiusFont.Label(new Rect(r.x + 14f, r.y, r.width - 28f, r.height), label,
                color: Palette.TextDim, anchor: TextAnchor.MiddleLeft, wrap: false);
            if (right != null && right.Length > 0)
            {
                RadiusFont.Label(new Rect(r.x + 14f, r.y, r.width - 28f, r.height), right,
                    color: Palette.TextDim, anchor: TextAnchor.MiddleRight, wrap: false);
            }
        }
    }
}
