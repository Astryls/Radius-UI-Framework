using System.Collections.Generic;

namespace RadiusUI.Framework
{
    /// <summary>
    /// Helpers for transient state keyed by <c>Thing.thingIDNumber</c>.
    ///
    /// Any per-pawn UI state held in a dictionary LEAKS unless it is pruned: pawns die, leave
    /// the map, or get despawned, and nothing tells the UI. Colonist Bar had this prune loop
    /// copy-pasted into every cache owner before it was hoisted; this is that one home.
    ///
    /// Key by the int id, never by the <c>Pawn</c> reference - holding a Thing keeps a dead
    /// pawn's whole object graph alive, and RimWorld reuses Thing instances across load.
    ///
    /// Thread affinity: OnGUI main thread only. Not thread safe (the scratch list is shared).
    /// </summary>
    public static class IdMap
    {
        // Shared scratch so pruning itself allocates nothing. Safe because the whole module is
        // main-thread-only and PruneById never re-enters.
        private static readonly List<int> pruneTmp = new List<int>(64);

        /// <summary>
        /// Remove every entry whose key is not in <paramref name="live"/>.
        ///
        /// Two-pass by necessity: a Dictionary cannot be modified while its key collection is
        /// being enumerated, so dead keys are collected first and removed after.
        ///
        /// Cost: O(n) over the dictionary, no allocation. Call on a slow cadence (once a
        /// second is plenty), not every frame.
        /// </summary>
        public static void PruneById<TV>(Dictionary<int, TV> d, HashSet<int> live)
        {
            if (d.Count == 0)
            {
                return;
            }

            pruneTmp.Clear();
            foreach (int k in d.Keys)
            {
                if (!live.Contains(k))
                {
                    pruneTmp.Add(k);
                }
            }

            for (int i = 0; i < pruneTmp.Count; i++)
            {
                d.Remove(pruneTmp[i]);
            }
            pruneTmp.Clear();
        }
    }
}
