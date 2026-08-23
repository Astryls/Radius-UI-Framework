// Radius UI Framework - Perf/FrameProbe.cs
//
// PERFORMANCE_PLAYBOOK B7 says: measure with a profiler, watch the ALLOCATION column, and
// "record the measured budget for each hot path in ARCHITECTURE.md so regressions are
// visible". The compliance review found every Measured cell in the framework's §5 still
// reading "not yet" - budgets that were never measured are aspirations, not budgets.
//
// This is the instrument that lets those cells be filled with REAL numbers. It does not
// replace Dubs Performance Analyzer (which sees the whole game and the GC); it answers the
// one question DPA answers awkwardly for UI code: "which of MY named draw phases owns the
// milliseconds, and how many times per frame does each actually run?"
//
// HONESTY NOTE: numbers from this are wall-clock over a Stopwatch, on a machine also
// running the game. Use it for RELATIVE attribution and for catching a regression, not to
// publish absolute figures. Anything under ~0.05 ms per call is at the noise floor.
//
// Thread affinity: main thread only. Zero cost when disabled: Sample() returns a default
// struct and the using-block compiles to a constrained call on a struct, so there is no
// boxing and no allocation on the disabled path.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace RadiusUI.Framework
{
    public static class FrameProbe
    {
        /// <summary>
        /// Master switch. Leave FALSE in shipped play - flip it from a dev-mode control for a
        /// measurement pass. Every <see cref="Sample"/> is a single bool test while off.
        /// </summary>
        public static bool Enabled;

        private sealed class Entry
        {
            public long Ticks;
            public int Calls;
            public long Worst;
        }

        private static readonly Dictionary<string, Entry> entries =
            new Dictionary<string, Entry>(16, StringComparer.Ordinal);

        private static readonly Stopwatch clock = Stopwatch.StartNew();
        private static int frames;
        private static int lastFrameStamp = -1;

        /// <summary>
        /// Open a measurement scope. Use with a using block so it always closes:
        /// <code>
        ///   using (FrameProbe.Sample("QuestFeed.rows")) { DrawRows(); }
        /// </code>
        /// Nesting is fine (each label accumulates independently), but note that an outer
        /// scope's time INCLUDES its inner scopes - subtract if you want exclusive time.
        /// </summary>
        public static Scope Sample(string label)
        {
            if (!Enabled)
            {
                return default;
            }
            // Count a frame the first time any sample opens in a new Unity frame, so
            // per-frame averages divide by frames actually measured.
            int f = UnityEngine.Time.frameCount;
            if (f != lastFrameStamp)
            {
                lastFrameStamp = f;
                frames++;
            }
            return new Scope(label, clock.ElapsedTicks);
        }

        /// <summary>Measurement scope. Struct: no allocation, no boxing in a using block.</summary>
        public readonly struct Scope : IDisposable
        {
            private readonly string? label;
            private readonly long startTicks;

            internal Scope(string label, long startTicks)
            {
                this.label = label;
                this.startTicks = startTicks;
            }

            public void Dispose()
            {
                if (label == null)
                {
                    return;   // disabled path
                }
                long elapsed = clock.ElapsedTicks - startTicks;
                if (!entries.TryGetValue(label, out Entry e))
                {
                    e = new Entry();
                    entries[label] = e;
                }
                e.Ticks += elapsed;
                e.Calls++;
                if (elapsed > e.Worst)
                {
                    e.Worst = elapsed;
                }
            }
        }

        /// <summary>Discard everything measured so far and restart the frame counter.</summary>
        public static void Reset()
        {
            entries.Clear();
            frames = 0;
            lastFrameStamp = -1;
        }

        /// <summary>
        /// Human-readable report: per label, calls per frame, mean ms per call, worst call,
        /// and total ms per frame. Paste straight into ARCHITECTURE.md's Measured column.
        /// Allocates - call it once when you want the numbers, never per frame.
        /// </summary>
        public static string Report()
        {
            if (frames == 0 || entries.Count == 0)
            {
                return "FrameProbe: nothing measured (Enabled=" + Enabled + ").";
            }
            double msPerTick = 1000.0 / Stopwatch.Frequency;
            StringBuilder sb = ScratchText.Sb();
            sb.Append("FrameProbe over ").Append(frames).Append(" frames\n");
            sb.Append("label | calls/frame | mean ms | worst ms | ms/frame\n");
            foreach (KeyValuePair<string, Entry> kv in entries)
            {
                Entry e = kv.Value;
                double totalMs = e.Ticks * msPerTick;
                sb.Append(kv.Key).Append(" | ")
                  .Append((e.Calls / (float)frames).ToString("0.00")).Append(" | ")
                  .Append((totalMs / Math.Max(1, e.Calls)).ToString("0.0000")).Append(" | ")
                  .Append((e.Worst * msPerTick).ToString("0.0000")).Append(" | ")
                  .Append((totalMs / frames).ToString("0.0000")).Append('\n');
            }
            return sb.ToString();
        }
    }
}
