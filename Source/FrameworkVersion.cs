// Radius UI Framework - FrameworkVersion.cs
//
// WHY THIS EXISTS. Two consumers were being developed in parallel against this library.
// One of them added tokens it needed and rebuilt; the other had been compiled against the
// previous shape and died at runtime with:
//
//     MissingFieldException:  Field not found: ...Palette.Surface0
//     MissingMethodException: Method not found: ...IconSet.get_ActionViewOrgans()
//
// Both mods still COMPILED. Nothing failed at build time, because each was built against
// a framework DLL that was correct when it was built. The break appears only at runtime,
// in a DIFFERENT mod from the one that changed, with a message that names a member rather
// than the mod that needs rebuilding. That is expensive to diagnose from the log alone.
//
// So the framework states its shape, and a consumer says which shape it was built for.
// A mismatch then reports itself in one line naming BOTH mods and the fix.

using Verse;

namespace RadiusUI.Framework
{
    /// <summary>
    /// The framework's public-API generation. Thread affinity: none (constants).
    /// </summary>
    public static class FrameworkVersion
    {
        /// <summary>
        /// Bumped whenever the public surface CHANGES INCOMPATIBLY - a member removed, a
        /// member renamed, or the VALUE behind a name repurposed. Purely additive changes
        /// do not bump it: an older consumer keeps working against a newer framework.
        ///
        /// <para>History:
        /// 1 = initial component landing (Tokens/Icons/Text/Widgets/Settings).
        /// 2 = surface vocabulary reconciled to Surface0..3 / Border / Ink-TextMid-TextFaint;
        ///     IconSet's named alias accessors replaced by the Get() alias table;
        ///     HoverWash re-valued 0.14 -> 0.05 with ActiveWash taking 0.14.
        /// 3 = the ratified wash ladder lands (WashFaint/Wash/WashHover/WashStrong/WashSolid =
        ///     .03/.06/.10/.14/.22; user decision 2026-08-18). Border/BorderFaint/ActiveWash
        ///     become same-value aliases; HoverWash RE-VALUED .05 -> .10 to the ladder step -
        ///     the value behind a name changed, hence the bump. Also: ModCompat.IsActive
        ///     recognises Workshop "_steam"-suffixed ids; ModCompat.StaticValue coerces boxed
        ///     structs (TaggedString) for T=string; Throttle gains EveryTicks.
        ///     Additive within gen 3 (2026-08-18 compliance pass, no bump - an older consumer
        ///     keeps working): Widgets/UIKit (Button/IconButton/TagPill/SectionBar),
        ///     RadiusFont.Epoch, Perf/StaggeredScheduler, Perf/FrameProbe, Palette.Pin
        ///     (player-marked/bookmark yellow, deliberately outside the Good/Warn/Bad ramp).
        ///     A consumer that USES those must require 3.</para>
        /// </summary>
        public const int Current = 3;

        /// <summary>
        /// Assert at startup that this framework is new enough for <paramref name="consumer"/>.
        /// Call from a <c>[StaticConstructorOnStartup]</c>, passing the generation the mod was
        /// written against. Returns false (and logs a plain-language error naming both sides)
        /// when the framework is older than required.
        ///
        /// <para>This cannot catch every case - a consumer compiled against a LATER framework
        /// that has since been rebuilt older will still throw <c>MissingMethodException</c>
        /// before any of our code runs. It turns the common, recoverable case into a sentence
        /// a human can act on.</para>
        /// </summary>
        public static bool Require(string consumer, int minimum)
        {
            if (Current >= minimum) return true;

            Log.Error(
                "[Radius UI] " + consumer + " needs Radius UI Framework generation " + minimum
                + ", but the loaded framework is generation " + Current + ". "
                + consumer + " will not work correctly until the framework is updated. "
                + "If you build these from source, rebuild the framework and then every "
                + "consumer mod - a consumer compiled against a different generation fails "
                + "at runtime with MissingMethodException / MissingFieldException while still "
                + "compiling cleanly.");
            return false;
        }
    }
}
