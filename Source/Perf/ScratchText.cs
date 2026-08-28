using System.Runtime.CompilerServices;
using System.Text;

namespace RadiusUI.Framework
{
    /// <summary>
    /// Pooled scratch <see cref="StringBuilder"/>s for tooltip and inspect-string assembly.
    ///
    /// The mouse is a single point over at most a handful of rects per frame, and every hover
    /// block appends then ToString()s synchronously before control leaves the block - so
    /// pooled builders remove the per-hover-frame allocation entirely.
    ///
    /// THE POOL IS ROTATED per call rather than being a single shared instance: that makes
    /// NESTING SAFE (an inner <see cref="Sb"/> consumer can no longer silently clobber an
    /// outer one's half-built string) instead of merely being forbidden by a comment.
    /// Rotation is O(1) and allocation-free.
    ///
    /// Thread affinity: OnGUI main thread only. Not thread safe.
    /// </summary>
    public static class ScratchText
    {
        private const int Slots = 4;          // must stay a power of two for the mask below
        private const int Mask = Slots - 1;

        private static readonly StringBuilder[] pool =
        {
            new StringBuilder(256), new StringBuilder(256),
            new StringBuilder(256), new StringBuilder(256)
        };

        private static int idx;

        /// <summary>
        /// Returns a pooled StringBuilder, emptied and ready.
        ///
        /// CONTRACT: consume it synchronously within the same block (append, then ToString).
        /// Up to four consumers may be live at once, so an inner block cannot corrupt an outer
        /// one - but do NOT hold a reference across frames or stash it in a field, and do not
        /// nest more than four deep.
        ///
        /// Cost: one array read, one increment, one length reset. No allocation.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static StringBuilder Sb()
        {
            StringBuilder b = pool[idx];
            idx = (idx + 1) & Mask;
            b.Length = 0;
            return b;
        }
    }
}
