using System.Runtime.CompilerServices;
using UnityEngine;
using Verse;

namespace RadiusUI.Framework
{
    /// <summary>
    /// Realtime cadence gates for work that should run on a wall-clock interval rather than
    /// every frame - background data gathers, structural re-scans, hover-text rebuilds.
    ///
    /// TWO GATES ON PURPOSE. <see cref="Every"/> is for FOREGROUND cadences the player is
    /// actively looking at; <see cref="EveryScaled"/> is for BACKGROUND data that the suite's
    /// refresh-rate setting is advertised to slow down. The split is deliberate so a caller
    /// has to state which one it wants rather than inheriting a cadence by accident.
    ///
    /// Realtime, not game time: these keep running while the game is paused, which is what a
    /// UI refresh wants. For game-time work use <c>Find.TickManager.TicksGame</c> instead.
    ///
    /// Thread affinity: OnGUI main thread only (<c>Time.realtimeSinceStartup</c> is a Unity
    /// API). Not thread safe.
    /// </summary>
    public static class Throttle
    {
        /// <summary>
        /// Returns true when <paramref name="nextT"/> has come due and re-arms it
        /// <paramref name="seconds"/> into the future; false otherwise. Collapses the
        /// hand-rolled <c>if (now >= _nextT) { _nextT = now + k; ... }</c> idiom.
        /// <code>
        ///   static float _next;
        ///   if (Throttle.Every(ref _next, 0.25f)) RebuildHoverText();
        /// </code>
        /// A stamp at its default value (0) fires immediately on first call, which is the
        /// wanted behaviour - the first frame should populate, not wait a full interval.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Every(ref float nextT, float seconds)
        {
            float now = Time.realtimeSinceStartup;
            if (now < nextT)
            {
                return false;
            }
            nextT = now + seconds;
            return true;
        }

        /// <summary>
        /// As <see cref="Every"/>, but the cadence is multiplied by the user's suite-wide
        /// background refresh multiplier (<see cref="RadiusTheme.RefreshMult"/>).
        /// <paramref name="baseSeconds"/> is the cadence at the default multiplier of 1.
        ///
        /// Use for background gathers - conditions, colony vitals, temperature recalcs - i.e.
        /// anything the refresh-rate slider is meant to govern. A player on a large colony can
        /// then trade freshness for frame time in one place for the whole suite.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool EveryScaled(ref float nextT, float baseSeconds)
        {
            return Every(ref nextT, baseSeconds * RadiusTheme.RefreshMult);
        }

        /// <summary>
        /// GAME-TIME twin of <see cref="Every"/> (PERFORMANCE_PLAYBOOK B1, tick-coherent
        /// caches): fires when <paramref name="nextTick"/> has come due on the
        /// <c>TicksGame</c> clock and re-arms it <paramref name="intervalTicks"/> ahead.
        /// Use for work that should track the SIMULATION - decay scans, colony-state
        /// gathers - which must stop while the game is paused and speed up at 3x, which is
        /// exactly what realtime <see cref="Every"/> must not do.
        ///
        /// Staggering is the CALLER's job, per B2: offset per entity with
        /// <c>(id % k)</c> added to the interval, or every entity re-fires on the same tick.
        ///
        /// Null-safe at the main menu (no TickManager yet): reports due, so first-call
        /// population still happens. Main thread only.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool EveryTicks(ref int nextTick, int intervalTicks)
        {
            int now;
            try { now = Find.TickManager?.TicksGame ?? 0; }
            catch { now = 0; }
            if (now < nextTick)
            {
                return false;
            }
            nextTick = now + intervalTicks;
            return true;
        }
    }
}
