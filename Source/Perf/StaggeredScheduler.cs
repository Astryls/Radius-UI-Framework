// Radius UI Framework - Perf/StaggeredScheduler.cs
//
// PERFORMANCE_PLAYBOOK Part C's "StaggeredScheduler", and B2's "budgeted work queues beat
// a burst recompute".
//
// THE PROBLEM IT SOLVES: a consumer notices N things need rebuilding (25 quest snapshots,
// 40 pawn cards, every condition on the map) and rebuilds them all in the frame it noticed.
// That is one visible hitch. Spreading the same work across the next N/budget frames costs
// the same total CPU and produces no hitch at all, because a frame that does 4 items instead
// of 100 stays inside its budget.
//
// WHEN NOT TO USE IT: if the work is cheap, or must be correct THIS frame, do it inline.
// Deferred work is stale work - only defer things a player cannot see going stale (icon
// prewarms, structural re-scans, tooltip text rebuilds), never the numbers on screen.
//
// STATUS: no v1 consumer. Landed to close the Part C gap the compliance review found;
// the Quest Menu's rebuild is a single O(quests) pass and does NOT need deferring. Kept
// deliberately small so it does not rot while unused.
//
// Thread affinity: main thread only (drain from OnGUI or a GameComponent update). Not
// thread safe.

using System;
using System.Collections.Generic;
using Verse;

namespace RadiusUI.Framework
{
    /// <summary>
    /// A FIFO work queue drained a few items per pump, so a burst of deferred work becomes a
    /// flat cost across several frames instead of one hitch.
    /// </summary>
    public sealed class StaggeredScheduler
    {
        private readonly Queue<Action> queue;
        private readonly string label;
        private readonly PatchGuard guard;

        /// <param name="label">Shown in the log if an item throws. Name the owning feature.</param>
        /// <param name="capacity">Expected queue depth, to size the backing store once.</param>
        public StaggeredScheduler(string label, int capacity = 32)
        {
            this.label = label;
            queue = new Queue<Action>(capacity);
            guard = new PatchGuard("StaggeredScheduler:" + label);
        }

        /// <summary>Items waiting. Cost: field read.</summary>
        public int PendingCount => queue.Count;

        /// <summary>True once repeated failures have disabled draining (see PatchGuard).</summary>
        public bool Disabled => !guard.ShouldRun;

        /// <summary>
        /// Queue one unit of work.
        ///
        /// <para>The delegate allocates at ENQUEUE time, which is the point: you pay one
        /// closure when the work is discovered, not one per frame while it waits. Do not call
        /// this from inside a per-frame draw loop - that reintroduces the per-frame allocation
        /// the queue exists to remove.</para>
        /// </summary>
        public void Enqueue(Action work)
        {
            if (work != null)
            {
                queue.Enqueue(work);
            }
        }

        /// <summary>
        /// Run up to <paramref name="budget"/> queued items and return how many ran.
        /// Call once per frame (or per tick) from the owning component.
        ///
        /// <para>An item that throws is logged once through the shared <see cref="PatchGuard"/>
        /// and DROPPED - a failing item never blocks the queue behind it, and repeated failures
        /// disable draining rather than spamming the log every frame.</para>
        /// </summary>
        public int Drain(int budget = 4)
        {
            if (budget <= 0 || queue.Count == 0 || !guard.ShouldRun)
            {
                return 0;
            }
            int ran = 0;
            while (ran < budget && queue.Count > 0)
            {
                Action work = queue.Dequeue();
                ran++;
                try
                {
                    work();
                }
                catch (Exception e)
                {
                    guard.Fail(e);
                    if (!guard.ShouldRun)
                    {
                        break;
                    }
                }
            }
            return ran;
        }

        /// <summary>Drop all pending work (map change, window closed, settings reset).</summary>
        public void Clear()
        {
            queue.Clear();
        }
    }
}
