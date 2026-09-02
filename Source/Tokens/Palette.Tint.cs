// Radius UI Framework - Tokens/Palette.Tint.cs
//
// ADDED generation 15 (2026-08-28). PURELY ADDITIVE: a new partial file plus one new struct.
// Nothing existing is renamed and no existing VALUE moves (§13 rules 1, 2).
//
// WHY THIS EXISTS.
// The suite's flat style has one signature move, and it is currently written out by hand
// everywhere it appears: a state chip is a 15% fill of a hue, a 34% hairline of the same hue,
// and a lightened same-hue ink. Adaptive Work Priorities ships it as SIX constants for TWO
// hues (SelFill/SelEdge/SelInk and OnFill/OnEdge/OnInk); the Needs Tab and the Colonist Bar
// each write their own; Policies needs it for five domain hues, five medical care tiers and
// however many drug chemistries a modded game has.
//
// That is well past §9's third occurrence, and the failure mode when it drifts is the one this
// framework keeps recording: not an error, just two screens that no longer look like the same
// product. So the RELATIONSHIP is named once, here, and a consumer passes a hue.
//
// The alpha constants are AWP's, unchanged, so existing surfaces do not move. Ink is a lerp
// toward white that reproduces AWP's two hand-picked inks to within a couple of points:
//   Accent (0.45, 0.75, 1.00) -> (0.72, 0.86, 1.00)   AWP SelInk
//   Good   (0.40, 0.85, 0.40) -> (0.67, 0.92, 0.67)   AWP OnInk is (0.62, 0.92, 0.62)
// Close enough that nothing needs re-authoring, and now every OTHER hue gets a correct ink
// instead of a guess.
//
// PERFORMANCE: returns a struct and allocates nothing, so it is safe to call per row per frame
// and needs no cache (PERFORMANCE_PLAYBOOK: nothing in Tokens may allocate at draw time). Three
// lerps is cheaper than the dictionary probe a cache would cost.
//
// Thread affinity: none (pure function over a struct).

using UnityEngine;

namespace RadiusUI.Framework
{
    /// <summary>
    /// The three colours a flat state chip is drawn from, all derived from one hue.
    /// Obtain one with <see cref="Palette.TintOf(Color)"/>; never assemble it by hand.
    /// </summary>
    public readonly struct TintSet
    {
        /// <summary>The chip's fill. Faint enough to sit on any surface in the ramp.</summary>
        public readonly Color Fill;

        /// <summary>A 1px hairline around the chip. This is what makes it read as a chip
        /// rather than as a wash.</summary>
        public readonly Color Edge;

        /// <summary>Label colour on top of <see cref="Fill"/>. Lightened toward white so it
        /// stays legible without falling back to plain <see cref="Palette.Ink"/>, which would
        /// throw away the hue the chip exists to carry.</summary>
        public readonly Color Ink;

        internal TintSet(Color fill, Color edge, Color ink) { Fill = fill; Edge = edge; Ink = ink; }
    }

    public static partial class Palette
    {
        /// <summary>Alpha of a tint chip's fill. AWP's SelFill/OnFill, unchanged.</summary>
        public const float TintFillAlpha = 0.15f;

        /// <summary>Alpha of a tint chip's hairline. AWP's SelEdge, unchanged.</summary>
        public const float TintEdgeAlpha = 0.34f;

        /// <summary>How far a tint chip's ink is lightened toward white.</summary>
        public const float TintInkLift = 0.45f;

        /// <summary>
        /// The flat style's state-chip triad for one hue: a 15% fill, a 34% hairline and a
        /// lightened same-hue ink.
        ///
        /// <para>Allocation-free and uncached - three lerps beat a dictionary probe, so this is
        /// safe per row per frame.</para>
        ///
        /// <para>Alpha on <paramref name="hue"/> is ignored; the triad supplies its own.</para>
        /// </summary>
        public static TintSet TintOf(Color hue)
        {
            return new TintSet(
                new Color(hue.r, hue.g, hue.b, TintFillAlpha),
                new Color(hue.r, hue.g, hue.b, TintEdgeAlpha),
                Color.Lerp(new Color(hue.r, hue.g, hue.b, 1f), Color.white, TintInkLift));
        }

        /// <summary>
        /// <see cref="TintOf(Color)"/> with the fill and hairline scaled by
        /// <paramref name="strength"/>, for a chip that is present but not selected. Ink is
        /// left alone: dimming the label as well makes a hover state look disabled.
        /// </summary>
        public static TintSet TintOf(Color hue, float strength)
        {
            return new TintSet(
                new Color(hue.r, hue.g, hue.b, TintFillAlpha * strength),
                new Color(hue.r, hue.g, hue.b, TintEdgeAlpha * strength),
                Color.Lerp(new Color(hue.r, hue.g, hue.b, 1f), Color.white, TintInkLift));
        }
    }
}
