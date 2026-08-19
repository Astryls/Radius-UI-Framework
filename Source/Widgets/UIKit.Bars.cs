// Radius UI Framework - Widgets/UIKit.Bars.cs
//
// ADDED generation 4 (2026-08-19). ADDITIVE, new partial file, nothing existing touched.

using UnityEngine;
using Verse;

namespace RadiusUI.Framework
{
    public static partial class UIKit
    {
        /// <summary>
        /// A centre-zero bidirectional bar: the track's midpoint is 0, positive fills right,
        /// negative fills left. For any reading that is naturally signed - an opinion, a mood
        /// delta, a stat offset - where a one-sided bar forces the player to read the number
        /// before they know which way it runs.
        ///
        /// <para>Third occurrence in the suite (Health Tab, Colonist Bar, Radius UI - Social Tab),
        /// so GLOBAL_RULES §9 makes it shared.</para>
        ///
        /// <para>Cost: 3-4 draw calls (well, fill, midline), all through <see cref="Spatial.Pill"/>
        /// so nothing is 9-sliced. Repaint-gated internally; emits no GUI control, so it is safe to
        /// skip on non-Repaint passes without shifting control ids.</para>
        /// </summary>
        /// <param name="r">Track rect. Height drives the cap radius; <see cref="Metrics.BarH"/> is
        /// the suite default.</param>
        /// <param name="value">Signed reading.</param>
        /// <param name="max">Absolute value that fills half the track. Clamped, never divides by 0.</param>
        /// <param name="positive">Fill colour when value &gt;= 0.</param>
        /// <param name="negative">Fill colour when value &lt; 0.</param>
        /// <param name="showMidline">Draw the zero tick. Off for very short bars where the tick
        /// would be most of the bar.</param>
        public static void BiBar(Rect r, float value, float max, Color positive, Color negative,
                                 bool showMidline = true)
        {
            if (!FrameGate.Drawing || r.width < 2f || r.height < 1f) return;

            Spatial.Pill(r, Palette.Surface0);

            float limit = Mathf.Max(0.0001f, max);
            float frac = Mathf.Clamp(value / limit, -1f, 1f);
            float half = r.width * 0.5f;
            float mid = r.x + half;
            float len = Mathf.Abs(frac) * half;

            if (len >= 1f)
            {
                Rect fill = frac >= 0f
                    ? new Rect(mid, r.y, len, r.height)
                    : new Rect(mid - len, r.y, len, r.height);
                Spatial.Pill(fill, frac >= 0f ? positive : negative);
            }

            if (showMidline && r.height >= 4f)
                Widgets.DrawBoxSolid(new Rect(mid - 0.5f, r.y - 1f, 1f, r.height + 2f),
                                     Palette.WashStrong);
        }

        /// <summary>
        /// <see cref="BiBar"/> with the suite's standard opinion range (-100..100) and the
        /// bond/rift identity pair. Use this for anything that is literally an opinion, so the
        /// colour choice cannot drift between consumers.
        /// </summary>
        public static void OpinionBar(Rect r, float opinion, bool showMidline = true)
            => BiBar(r, opinion, 100f, Palette.Bond, Palette.Rift, showMidline);
    }
}
