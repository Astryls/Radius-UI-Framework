// Radius UI Framework - Tokens/Palette.Ideology.cs
//
// Ideology-domain colour. Lives in the framework for the same reason Palette.Health does:
// the framework owns the art these colours pair with (Textures/RadiusUI/Marker/), and a
// colour and the PNG it is drawn beside must not be able to drift apart across two mods.
//
// ─────────────────────────────────────────────────────────────────────────────
//  THE IMPACT RAMP IS NOT A STYLE CHOICE
// ─────────────────────────────────────────────────────────────────────────────
// Six of the values below are VANILLA'S OWN NUMBERS, lifted verbatim from
// RimWorld.IdeoUIUtility.GetIconAndLabelColor and .GetBackgroundColor. Vanilla colours
// every precept tile in the ideoligion window by PreceptImpact, so a consumer claiming
// "1:1 parity" with that window has to match these exactly. They are recorded here rather
// than re-derived in each consumer so that a future vanilla change is a one-line edit in
// one file.
//
// If RimWorld ever changes those colours, update THIS file and nothing else.
//
// ─────────────────────────────────────────────────────────────────────────────
//  WHY Tokens DOES NOT MAP PreceptImpact ITSELF
// ─────────────────────────────────────────────────────────────────────────────
// ARCHITECTURE §2: "Tokens depends on nothing. Pure values, no Unity calls beyond Color."
// Taking a RimWorld.PreceptImpact parameter here would put a game type in the one layer
// that is meant to have none - the same reason Metrics.Fit takes a scale argument instead
// of reading RadiusTheme. The consumer does the three-case switch; it is two lines and it
// keeps this layer inert.
//
// Thread affinity: none (immutable statics). Cost: field read, zero allocation.

using UnityEngine;

namespace RadiusUI.Framework
{
    public static partial class Palette
    {
        /// <summary>
        /// Ideology-domain colour identities. Consumers: Radius UI - Ideology.
        /// </summary>
        public static class Ideology
        {
            // ─────────────────────────────────────────────────────────────
            //  Precept impact - label and icon tint
            //  Vanilla IdeoUIUtility.GetIconAndLabelColor. High is a warm gold,
            //  medium is plain white, low is a dulled grey. The ramp encodes
            //  HOW MUCH THIS PRECEPT MATTERS, not whether it is good.
            // ─────────────────────────────────────────────────────────────

            /// <summary>High-impact precept label. Vanilla <c>1, 1, 0.5</c>.</summary>
            public static readonly Color ImpactHigh = new Color(1.00f, 1.00f, 0.50f);

            /// <summary>Medium-impact precept label. Vanilla <c>1, 1, 1</c>.</summary>
            public static readonly Color ImpactMedium = new Color(1.00f, 1.00f, 1.00f);

            /// <summary>Low-impact precept label. Vanilla <c>0.7, 0.7, 0.7</c>.</summary>
            public static readonly Color ImpactLow = new Color(0.70f, 0.70f, 0.70f);

            // ─────────────────────────────────────────────────────────────
            //  Precept impact - tile background
            //  Vanilla IdeoUIUtility.GetBackgroundColor. Three greys close
            //  enough together that the tile reads as one family, far enough
            //  apart that a grid of them shows structure at a glance.
            //
            //  These are deliberately NOT the suite's surface tokens: they are
            //  vanilla's, and a precept grid must match the vanilla window it
            //  replaces. Using Palette.PanelBG here would break parity.
            // ─────────────────────────────────────────────────────────────

            /// <summary>High-impact tile plate. Vanilla <c>0.24</c>.</summary>
            public static readonly Color ImpactBgHigh = new Color(0.24f, 0.24f, 0.24f);

            /// <summary>Medium-impact tile plate. Vanilla <c>0.18</c>.</summary>
            public static readonly Color ImpactBgMedium = new Color(0.18f, 0.18f, 0.18f);

            /// <summary>Low-impact tile plate. Vanilla <c>0.13</c>.</summary>
            public static readonly Color ImpactBgLow = new Color(0.13f, 0.13f, 0.13f);

            // ─────────────────────────────────────────────────────────────
            //  Approval ramp - five steps
            //  How strongly members feel about an issue. FOUR of the five are
            //  aliases onto the semantic ramp rather than new colours, because
            //  "this ideoligion approves" and "this reading is healthy" should
            //  not be two different greens anywhere in the suite.
            //
            //  Only Abhorrent is genuinely new: the semantic ramp bottoms out
            //  at Bad, and abhorrent has to read as a step BELOW it or the two
            //  worst stances are indistinguishable in a grid.
            // ─────────────────────────────────────────────────────────────

            /// <summary>Highest approval. Alias of <see cref="Palette.GoodBright"/>.</summary>
            public static readonly Color Exalted = GoodBright;

            /// <summary>Approved. Alias of <see cref="Palette.Good"/>.</summary>
            public static readonly Color Approved = Good;

            /// <summary>Disapproved. Alias of <see cref="Palette.Warn"/>.</summary>
            public static readonly Color Disapproved = Warn;

            /// <summary>Horrible. Alias of <see cref="Palette.Bad"/>.</summary>
            public static readonly Color Horrible = Bad;

            /// <summary>
            /// Abhorrent - the fifth step, darker and more saturated than <see cref="Palette.Bad"/>
            /// so the worst stance is separable from merely horrible at badge size.
            /// </summary>
            public static readonly Color Abhorrent = new Color(0.70f, 0.23f, 0.23f);

            // ─────────────────────────────────────────────────────────────
            //  Certainty
            //  A follower's grip on the ideoligion. This IS a severity, so it
            //  routes through the shared ramp rather than inventing a third
            //  green: see Palette.Severity for the same idea on stat values.
            // ─────────────────────────────────────────────────────────────

            /// <summary>
            /// Certainty colour for <paramref name="pct"/> in 0..1. Below 0.35 a pawn is at
            /// real risk of converting away, which is the number the player actually acts on,
            /// so that is where the ramp turns.
            /// Pure. Cost: two compares.
            /// </summary>
            public static Color Certainty(float pct)
            {
                if (pct >= 0.70f) return Good;
                if (pct >= 0.35f) return Warn;
                return Bad;
            }
        }
    }
}
