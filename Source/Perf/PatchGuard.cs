using System;
using Verse;

namespace RadiusUI.Framework
{
    /// <summary>
    /// A self-disabling wrapper for code that must never take the game down with it -
    /// Harmony patch bodies, compat bridges, per-frame draw hooks.
    ///
    /// WHY THIS EXISTS: a throw inside OnGUI or Tick costs a full stack walk AND RimWorld logs
    /// it every frame, so one bug degrades into an unplayable game and a 200 MB log. The
    /// opposite failure is just as bad: the suite currently has ~10 bare <c>catch { }</c>
    /// blocks that swallow the error silently, so a real bug is invisible forever.
    ///
    /// This is the middle path the Performance Playbook (Part B6) asks for: log ONCE with
    /// enough context to act on, keep running, and after <see cref="MaxFailures"/> failures
    /// disable the body permanently so a broken feature degrades instead of spamming.
    ///
    /// NOTE: this needs no Harmony reference - it is a plain guarded-invoke helper, so the
    /// framework stays patch-free (ARCHITECTURE §7).
    ///
    /// Thread affinity: main thread expected (Log.* is main-thread only in practice). Not
    /// thread safe.
    /// </summary>
    public sealed class PatchGuard
    {
        private readonly string label;
        private readonly int maxFailures;
        private int failures;

        /// <param name="label">
        /// Shown in the log line. Use something a bug report can be grepped for, e.g.
        /// "RadiusUI.HUD alert overlay".
        /// </param>
        /// <param name="maxFailures">Failures tolerated before the guard disables itself.</param>
        public PatchGuard(string label, int maxFailures = 3)
        {
            this.label = label;
            this.maxFailures = Math.Max(1, maxFailures);
        }

        /// <summary>Failures tolerated before <see cref="Disabled"/> latches.</summary>
        public int MaxFailures => maxFailures;

        /// <summary>True once the guard has given up. Latches; never resets on its own.</summary>
        public bool Disabled { get; private set; }

        /// <summary>
        /// Fast pre-check for hot paths. Prefer this shape in per-frame code - it is a single
        /// field read and allocates nothing, whereas <see cref="Run"/> takes a delegate:
        /// <code>
        ///   if (!guard.ShouldRun) return;
        ///   try { /* body */ }
        ///   catch (Exception e) { guard.Fail(e); }
        /// </code>
        /// </summary>
        public bool ShouldRun => !Disabled;

        /// <summary>
        /// Record a failure: logs the first one (and the disabling one) and latches
        /// <see cref="Disabled"/> once <see cref="MaxFailures"/> is reached. Never rethrows.
        /// </summary>
        public void Fail(Exception e)
        {
            if (Disabled)
            {
                return;
            }

            failures++;
            if (failures == 1)
            {
                Log.Error($"[Radius UI] {label} threw; guarding. {e}");
            }

            if (failures >= maxFailures)
            {
                Disabled = true;
                Log.Error($"[Radius UI] {label} failed {failures} times and is now disabled "
                        + "for this session. Other Radius UI features are unaffected.");
            }
        }

        /// <summary>
        /// Convenience wrapper for cold paths (startup, settings, event handlers). Returns
        /// true when <paramref name="body"/> ran to completion.
        ///
        /// Do NOT use this in per-frame code with a capturing lambda - the closure allocates
        /// every call. Use the <see cref="ShouldRun"/> / <see cref="Fail"/> shape there.
        /// </summary>
        public bool Run(Action body)
        {
            if (Disabled)
            {
                return false;
            }
            try
            {
                body();
                return true;
            }
            catch (Exception e)
            {
                Fail(e);
                return false;
            }
        }

        /// <summary>
        /// Re-arm a guard that disabled itself (settings change, user hit "retry"). Use
        /// sparingly - a guard that keeps re-arming into the same bug is log spam again.
        /// </summary>
        public void Reset()
        {
            failures = 0;
            Disabled = false;
        }
    }
}
