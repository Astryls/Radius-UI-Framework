using System.Runtime.CompilerServices;
using UnityEngine;

namespace RadiusUI.Framework
{
    /// <summary>
    /// Viewport culling for rows inside a scroll view.
    ///
    /// THE FACT THAT SURPRISES PEOPLE: <c>Widgets.BeginScrollView</c> clips PIXELS - it does
    /// NOT skip your draw code. Every row in a 400-entry list still runs its label
    /// measurement, its icon resolution and its formatting even when it is a thousand pixels
    /// off-screen. Culling is therefore a real saving, not a micro-optimisation, and both
    /// suite drawers ended up hand-rolling it in 8+ places.
    ///
    /// TWO RULES BOTH MODS LEARNED THE HARD WAY:
    ///  1. Include SLACK. A hand-rolled test with no bottom slack drops a row straddling the
    ///     lower edge, which reads as a flickering gap while scrolling.
    ///  2. A CULL THAT CHANGES POSITIONS IS A BUG, not an optimisation. Skip the DRAW; still
    ///     advance your y cursor for culled rows, and never skip a row that emits an IMGUI
    ///     control (Button, TextField, scroll view) - dropping a control mid-list shifts every
    ///     later control's id and sends input to the wrong widget.
    ///
    /// Thread affinity: OnGUI main thread only.
    /// </summary>
    public static class ViewCull
    {
        /// <summary>One row of slack above and below, so edge rows never flicker.</summary>
        public const float DefaultSlack = 4f;

        /// <summary>
        /// True when a row at <paramref name="rowY"/> of height <paramref name="rowH"/> is
        /// visible in a scroll view scrolled to <paramref name="scroll"/> with viewport
        /// <paramref name="viewH"/>. Coordinates are content-space (the same space you lay
        /// rows out in).
        /// <code>
        ///   for (int i = 0; i &lt; rows.Count; i++) {
        ///       if (ViewCull.Visible(y, rowH, scroll.y, outRect.height))
        ///           DrawRow(new Rect(0, y, w, rowH), rows[i]);
        ///       y += rowH;                       // ALWAYS advance, culled or not
        ///   }
        /// </code>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Visible(float rowY, float rowH, float scroll, float viewH,
                                   float slack = DefaultSlack)
        {
            return rowY + rowH >= scroll - slack
                && rowY <= scroll + viewH + slack;
        }

        /// <summary>
        /// Rect overload of <see cref="Visible(float,float,float,float,float)"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Visible(Rect row, Vector2 scroll, Rect viewport,
                                   float slack = DefaultSlack)
        {
            return Visible(row.y, row.height, scroll.y, viewport.height, slack);
        }

        /// <summary>
        /// Index range of visible rows for a UNIFORM row height, so a long list can skip
        /// straight to the first visible index instead of testing every row. Returns the
        /// half-open range [first, end).
        ///
        /// Only valid when every row is the same height - for variable heights use
        /// <see cref="Visible(float,float,float,float,float)"/> per row.
        /// </summary>
        public static void VisibleRange(int count, float rowH, float scroll, float viewH,
                                        out int first, out int end, float slack = DefaultSlack)
        {
            if (count <= 0 || rowH <= 0f)
            {
                first = 0;
                end = 0;
                return;
            }
            first = Mathf.Max(0, Mathf.FloorToInt((scroll - slack) / rowH));
            end = Mathf.Min(count, Mathf.CeilToInt((scroll + viewH + slack) / rowH));
            if (end < first)
            {
                end = first;
            }
        }
    }
}
