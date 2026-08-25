// Radius UI Framework - Tokens/Palette.Genes.cs
//
// ADDED 2026-08-23 (generation 10). ADDITIVE ONLY: nothing existing is touched, no value
// changed, no member renamed. New partial file rather than an edit to Palette.cs, per §13's
// concurrent-edit rule.
//
// A NAMED DOMAIN SET, exactly like Palette.Health, Palette.Needs and Palette.Commands - not an
// extension of the semantic ramp. Good/Warn/Bad answer "how bad is this". These answer "what
// KIND of thing is this, and what does it cost".
//
// WHY THESE ARE NOT ALIASES OF EXISTING TOKENS. Three of the cost steps are numerically equal
// to Good, Over and Archo today. Binding those NAMES to cost steps is the precise failure
// §10 F8 rule 3 records: `Over` means "a value above its natural maximum" and `Archo` means
// archotech PROVENANCE. A later retune of either would silently recolour a xenotype's cost
// ramp with no error and no exception - a wrong-looking screen forever. The values coincide by
// design lineage; the sets are free to move independently from here. This is the same call
// Palette.Commands made at generation 6.
//
// COST, NOT RARITY. An earlier design coded this axis as rarity, bucketed off
// GeneDef.selectionWeight. Measured against Defs/Biotech/GeneDefs/*.xml, vanilla ships
// selectionWeight = 1 for very nearly every gene, so a rarity ramp would have been a number a
// consumer invented and then presented to the player as a fact about the game. Cost is derived
// from biostatCpx + biostatArc, which is what the player actually pays at the gene assembler,
// and it ranks a modded gene correctly with no bridge.
//
// THE FALLBACK IS THE IMPORTANT PART. A modded GeneCategoryDef can never have an entry here and
// there is no upper bound on how many a load order can add, so For(defName) hashes onto a stable
// ring of distinguishable hues. Two properties matter:
//   * STABLE - the same category gets the same colour every session, so muscle memory works.
//   * NEVER GREY - grey reads as "disabled" in this suite (StripGray), and a modded gene
//     category that looks disabled is a bug report.

using System.Collections.Generic;
using UnityEngine;

namespace RadiusUI.Framework
{
    public static partial class Palette
    {
        /// <summary>
        /// Gene identity and build-cost hues. "What kind of gene is this and what does it
        /// cost", never "how good is it". Added generation 10 for Radius UI - Xenotypes;
        /// general-purpose for any module that draws genes, xenotypes or genepacks.
        /// </summary>
        public static class Genes
        {
            // ---- the cost ramp ------------------------------------------------------
            // Four steps, ascending. Derived by the consumer from GeneDef.biostatCpx and
            // GeneDef.biostatArc, never from a hardcoded gene list.

            /// <summary>Costs nothing to carry - cosmetic and zero-complexity genes. #59626c</summary>
            public static readonly Color CostTrivial = new Color(0.349f, 0.384f, 0.424f);

            /// <summary>Low complexity, one or two points. #66d966</summary>
            public static readonly Color CostLight = new Color(0.40f, 0.85f, 0.40f);

            /// <summary>High complexity - needs gene processors past the base limit. #80ccff</summary>
            public static readonly Color CostHeavy = new Color(0.502f, 0.80f, 1.00f);

            /// <summary>Requires archite capsules, which cannot be crafted. #ffdf73</summary>
            public static readonly Color CostArchite = new Color(1.00f, 0.875f, 0.451f);

            /// <summary>
            /// The four cost steps in ascending order, for a consumer that has computed a
            /// step index rather than a named tier. Index-stable.
            /// </summary>
            public static readonly Color[] CostRamp = { CostTrivial, CostLight, CostHeavy, CostArchite };

            // ---- provenance ---------------------------------------------------------
            // The single most consequential fact about a gene, and the one vanilla never
            // states plainly: was the pawn born with it, or did somebody put it in them.

            /// <summary>Germline. Inherited from the pawn's parents, and children can inherit
            /// it in turn. A xenogerm can neither add nor remove one. #86c7a1</summary>
            public static readonly Color Endogene = new Color(0.525f, 0.780f, 0.631f);

            /// <summary>Implanted by a xenogerm. Never passed to children. #c79bf2</summary>
            public static readonly Color Xenogene = new Color(0.780f, 0.608f, 0.949f);

            /// <summary>A gene that consumes archite capsules to assemble. Same hue as
            /// <see cref="CostArchite"/> and deliberately so - "archite" is one idea, and a
            /// player should not have to learn it twice. #ffdf73</summary>
            public static readonly Color Archite = CostArchite;

            /// <summary>Present on the pawn but overridden by another gene, so it does
            /// nothing. Reads as inert rather than as a warning: an overridden gene is normal,
            /// not a problem. #4a4f59</summary>
            public static readonly Color Inactive = new Color(0.290f, 0.310f, 0.349f);

            // ---- the category fallback ring -----------------------------------------

            /// <summary>
            /// Hues an unrecognised gene category is assigned from. Every one is
            /// distinguishable from every other at a 3px strip, and none is grey (grey means
            /// "disabled" in this suite).
            /// </summary>
            public static readonly Color[] Fallback =
            {
                new Color(0.498f, 0.698f, 0.910f), // steel
                new Color(0.910f, 0.655f, 0.498f), // amber clay
                new Color(0.561f, 0.839f, 0.659f), // jade
                new Color(0.839f, 0.624f, 0.910f), // lilac
                new Color(0.910f, 0.839f, 0.498f), // brass
                new Color(0.498f, 0.839f, 0.839f), // lagoon
                new Color(0.910f, 0.529f, 0.608f), // rose clay
                new Color(0.659f, 0.722f, 0.910f), // slate blue
            };

            // defName -> resolved colour. Resolved once per def, then a dictionary hit.
            private static readonly Dictionary<string, Color> Cache = new Dictionary<string, Color>(32);

            /// <summary>
            /// The identity colour for a gene category. Vanilla's Biotech categories get a
            /// named hue; anything else gets a stable slot from <see cref="Fallback"/>, keyed
            /// on defName so it never changes between sessions.
            ///
            /// <para>Takes the <c>defName</c> rather than the <c>GeneCategoryDef</c>
            /// deliberately: ARCHITECTURE §2 requires that <c>Tokens</c> depend on nothing, and
            /// accepting a RimWorld def type here would quietly couple the palette to the def
            /// system. Callers pass <c>def.defName</c>.</para>
            ///
            /// <para>Cost: one dictionary hit after the first call per def. Never allocates on
            /// the hot path. Thread affinity: OnGUI main thread (the cache is not
            /// synchronised).</para>
            /// </summary>
            public static Color For(string defName)
            {
                if (string.IsNullOrEmpty(defName)) return Fallback[0];
                if (Cache.TryGetValue(defName, out Color c)) return c;
                c = Resolve(defName);
                Cache[defName] = c;
                return c;
            }

            private static Color Resolve(string defName)
            {
                switch (defName)
                {
                    case "Archite":               return Archite;
                    case "Hemogen":               return new Color(0.851f, 0.325f, 0.310f);
                    case "Healing":               return new Color(0.541f, 0.902f, 0.788f);
                    case "Ability":               return new Color(0.620f, 0.450f, 0.850f);
                    case "Psychic":               return new Color(0.710f, 0.549f, 1.00f);
                    case "Movement":              return new Color(0.550f, 0.850f, 0.950f);
                    case "Violence":              return new Color(0.900f, 0.300f, 0.300f);
                    case "Mood":                  return new Color(1.00f, 0.710f, 0.271f);
                    case "Temperature":           return new Color(0.451f, 0.750f, 0.920f);
                    case "ResistanceAndWeakness": return new Color(0.720f, 0.820f, 0.200f);
                    case "Aptitudes":             return new Color(1.00f, 0.843f, 0.478f);
                    case "Beauty":                return new Color(1.00f, 0.561f, 0.816f);
                    case "Reproduction":          return Endogene;
                    case "Sleep":                 return new Color(0.498f, 0.612f, 1.00f);
                    case "Pain":                  return new Color(0.750f, 0.200f, 0.450f);
                    case "Drugs":                 return new Color(0.851f, 0.541f, 0.416f);
                    case "Miscellaneous":         return OkGray;
                }
                // Vanilla generates whole families from GeneTemplateDefs, and the cosmetic ones
                // all share a defName stem. Matching by stem keeps a generated category out of
                // the ring rather than scattering one family across eight hues.
                if (defName.StartsWith("Cosmetic", System.StringComparison.Ordinal)) return OkGray;

                // Stable, non-negative, defName-keyed slot. Deliberately NOT string.GetHashCode:
                // .NET does not guarantee that is stable across runtimes, and "the gene category
                // changed colour after an update" is a bug nobody would think to report.
                int h = 17;
                for (int i = 0; i < defName.Length; i++) h = unchecked(h * 31 + defName[i]);
                return Fallback[(h & 0x7FFFFFFF) % Fallback.Length];
            }
        }
    }
}
