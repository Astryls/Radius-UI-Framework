using System.Collections.Generic;
using UnityEngine;

namespace RadiusUI.Framework
{
    /// <summary>Marker for a per-entity UI snapshot held by a <see cref="SnapshotTable{T}"/>.</summary>
    public interface ISnapshot
    {
        /// <summary>Frame index at which this snapshot next goes stale. Owned by the table.</summary>
        int NextRefresh { get; set; }
    }

    /// <summary>
    /// Per-entity UI snapshots refreshed on a slow cadence and STAGGERED so entities never
    /// refresh in lockstep. Extracted from Colonist Bar's <c>PawnUiCache</c>, which collapsed
    /// the per-frame engine walks three bar views were repeating every Repaint per pawn:
    /// job-report grammar, royalty/ideology lookups, hediff scans, apparel tatter, tend checks.
    ///
    /// THE SPLIT THAT MAKES THIS WORK: only SLOW-changing state belongs in a snapshot. Fast
    /// state (health %, mood, stances) must stay live at the call site, or the UI visibly lags
    /// the game. Putting everything in the snapshot is the classic mistake - it trades a
    /// performance problem for a correctness one.
    ///
    /// THE STAGGER: the refresh frame is offset by <c>id % Spread</c>. Without it, every
    /// entity added on the same frame refreshes on the same frame forever, which converts a
    /// smooth per-frame cost into a periodic spike - exactly what the cache was meant to avoid.
    ///
    /// Thread affinity: OnGUI main thread only. Not thread safe.
    /// </summary>
    /// <typeparam name="T">Snapshot payload; one instance is kept alive per entity id.</typeparam>
    public sealed class SnapshotTable<T> where T : class, ISnapshot, new()
    {
        /// <summary>Prime-ish spread for the per-id stagger. Coprime with typical intervals.</summary>
        private const int Spread = 7;

        private readonly Dictionary<int, T> map;
        private readonly int intervalFrames;

        /// <param name="intervalFrames">
        /// Frames between refreshes, before the per-id stagger. 15 is ~4 Hz at 60 fps, which is
        /// the cadence Colonist Bar settled on for pawn state.
        /// </param>
        /// <param name="capacity">Expected entity count, to size the dictionary once.</param>
        public SnapshotTable(int intervalFrames = 15, int capacity = 32)
        {
            this.intervalFrames = Mathf.Max(1, intervalFrames);
            map = new Dictionary<int, T>(capacity);
        }

        /// <summary>
        /// Fetch the snapshot for <paramref name="id"/> (creating it on first use).
        /// <paramref name="stale"/> is true when the caller must refill it this frame.
        ///
        /// Allocation-free on the steady path - deliberately does NOT take a refresh delegate,
        /// because a closure capturing the entity would allocate per entity per frame, which is
        /// the very cost this class exists to remove.
        /// <code>
        ///   Snap s = table.Get(pawn.thingIDNumber, out bool stale);
        ///   if (stale) { /* do the expensive engine walks, fill s */ }
        ///   DrawFast(pawn, s);   // fast-changing values read live here
        /// </code>
        /// </summary>
        public T Get(int id, out bool stale)
        {
            if (!map.TryGetValue(id, out T? s))
            {
                s = new T { NextRefresh = int.MinValue };
                map[id] = s;
            }

            int now = Time.frameCount;
            if (now >= s.NextRefresh)
            {
                // Stagger by id so entities never refresh on the same frame.
                s.NextRefresh = now + intervalFrames + (id % Spread);
                stale = true;
            }
            else
            {
                stale = false;
            }
            return s;
        }

        /// <summary>Force <paramref name="id"/> to refill on its next <see cref="Get"/>.</summary>
        public void Invalidate(int id)
        {
            if (map.TryGetValue(id, out T? s))
            {
                s.NextRefresh = int.MinValue;
            }
        }

        /// <summary>
        /// Drop snapshots for entities no longer present. Transient per-entity state leaks
        /// without this - see <see cref="IdMap.PruneById{TV}"/>.
        /// </summary>
        public void Prune(HashSet<int> live)
        {
            IdMap.PruneById(map, live);
        }

        /// <summary>Live snapshot count (diagnostics only).</summary>
        public int Count => map.Count;

        /// <summary>Drop everything (mode switch, map change).</summary>
        public void Clear()
        {
            map.Clear();
        }
    }
}
