// Radius UI Framework - Tokens/Palette.Life.cs
//
// ADDED generation 13 (2026-08-27). PURELY ADDITIVE: a NEW partial file on the existing
// Palette partial, per §13's concurrent-edit rule. Nothing existing is renamed and no
// existing VALUE moves (§13 rules 1 and 2).
//
// WHY THIS EXISTS.
// Radius UI - Bio Tab draws a life as a sequence of MOMENTS, and a moment has a kind: a
// marriage, a death, a piece of hard-won mastery, a written diary entry. The suite had four
// domain sets by generation 12 - Health ("what KIND of body part"), Needs ("which need"),
// Commands ("which command domain"), Genes ("what does this gene cost") - and none of them
// answers "what kind of MOMENT IN A LIFE is this". The predecessor mod solved it with ten
// colour constants of its own, which is exactly the fork this file exists to prevent.
//
// THE ONE RULE THAT DECIDES EVERY VALUE HERE: no hue may be mistakable for the semantic ramp.
// Good is grass green, Warn is amber, Bad is red, and they mean "how bad is this". A moment
// family means "what kind of thing was this". So Violence is GUNMETAL and not red, on purpose
// and permanently: a raid a colonist survived twenty years ago is not a warning, and drawing
// it in Bad would say it was. Loss is a cold blue rather than a red or a black. Vice is olive
// rather than a sickly green that could be read as Good.
//
// NOT ALIASES OF ANYTHING. Several of these sit near existing tokens in hue space. None of
// them aliases one, for the reason F8 rule 3 records: Archo means archotech PROVENANCE and Over
// means "above natural maximum", and binding a moment family to either name would mean a later
// retune of one silently recolours the other, with no error and nothing to grep for. Same call
// Palette.Commands made at generation 6 and Palette.Genes made at generation 10.
//
// ERA IS DELIBERATELY ABSENT. A chapter of a life (childhood, youth, the colony years) is a
// POSITION IN TIME, not a category, so it renders as a neutral value ramp using the text tokens
// that already exist - TextGhost through OkGray - with the live accent reserved for "now". No
// era hues are defined here and none should be added: the moment the suite gets six more named
// colours for six ordered steps, the ordering stops being visible in the colour.
//
// Thread affinity: none (constants + a memoised pure function). Allocates nothing after the
// first call per id.

using System.Collections.Generic;
using UnityEngine;

namespace RadiusUI.Framework
{
    public static partial class Palette
    {
        /// <summary>
        /// Life-moment identities for biography surfaces (Radius UI - Bio Tab). A family hue is
        /// an IDENTITY, not a severity: a violent moment is gunmetal because it was violent,
        /// never because something is wrong. Pair with the semantic ramp for the "something IS
        /// wrong" layer, which is a different question and has its own colours.
        /// </summary>
        public static class Life
        {
            /// <summary>Courtship, marriage, romance, attraction.</summary>
            public static readonly Color Love = new Color(1.00f, 0.529f, 0.765f);

            /// <summary>Birth, kin, children, the household.</summary>
            public static readonly Color Family = new Color(0.718f, 0.608f, 0.910f);

            /// <summary>Battles, raids, wounds taken and given. Gunmetal and NOT red: this is a
            /// KIND of moment, and drawing it in Bad would say a survived siege is a warning.</summary>
            public static readonly Color Violence = new Color(0.533f, 0.573f, 0.651f);

            /// <summary>Death, bereavement, things and people gone. A cold sunken blue rather
            /// than red or black, so grief never reads as danger.</summary>
            public static readonly Color Loss = new Color(0.369f, 0.478f, 0.600f);

            /// <summary>Making: masterworks, buildings, the work of the hands.</summary>
            public static readonly Color Craft = new Color(0.773f, 0.541f, 0.369f);

            /// <summary>Study, qualification, skill, anything learned.</summary>
            public static readonly Color Learning = new Color(0.373f, 0.784f, 0.878f);

            /// <summary>Ideoligion, rituals, oaths, conviction.</summary>
            public static readonly Color Faith = new Color(0.271f, 0.749f, 0.647f);

            /// <summary>Travel, arrival, departure, caravans, the long crossings.</summary>
            public static readonly Color Journey = new Color(0.424f, 0.608f, 0.941f);

            /// <summary>Inspiration, revelation, the archotech, the inexplicable.</summary>
            public static readonly Color Wonder = new Color(0.878f, 0.439f, 0.816f);

            /// <summary>Addiction, indulgence, the habits a life picks up. Olive rather than a
            /// sickly green, which would collide with Good.</summary>
            public static readonly Color Vice = new Color(0.639f, 0.663f, 0.310f);

            /// <summary>
            /// Index-stable family list, in the order a settings screen should present them.
            /// Index is part of the contract: a consumer may persist a family as an int, so
            /// entries are only ever APPENDED, never reordered.
            /// </summary>
            public static readonly Color[] Families =
            {
                Love, Family, Violence, Loss, Craft, Learning, Faith, Journey, Wonder, Vice
            };

            /// <summary>
            /// Stable hue ring for a moment family we have no rule for, keyed on a string id.
            /// Never grey: grey reads as disabled, and an unrecognised moment is a perfectly
            /// real thing that happened. Same reasoning and shape as the Needs, Commands and
            /// Genes fallback rings.
            /// </summary>
            public static readonly Color[] Fallback =
            {
                new Color(0.45f, 0.75f, 1.00f),
                new Color(0.40f, 0.85f, 0.55f),
                new Color(1.00f, 0.62f, 0.28f),
                new Color(0.70f, 0.55f, 1.00f),
                new Color(0.35f, 0.85f, 0.85f),
                new Color(1.00f, 0.55f, 0.78f),
                new Color(1.00f, 0.82f, 0.35f),
                new Color(0.62f, 0.78f, 0.45f),
            };

            private static readonly Dictionary<string, Color> Cache = new Dictionary<string, Color>(16);

            /// <summary>
            /// Hue for a moment-family id. Unknown ids get a stable slot from
            /// <see cref="Fallback"/>, so a family added by another mod (or derived from a
            /// modded TaleDef) is coloured consistently across sessions without the framework
            /// knowing it exists.
            ///
            /// <para>Takes a STRING, not a def type: Tokens depends on nothing (§2), and
            /// accepting a def would couple the palette to the def system.</para>
            ///
            /// <para>Cost: one dictionary hit after the first call for an id. Safe in a draw
            /// loop; main thread only, like everything else in Tokens.</para>
            /// </summary>
            public static Color For(string familyId)
            {
                if (string.IsNullOrEmpty(familyId)) return Fallback[0];
                if (Cache.TryGetValue(familyId, out Color c)) return c;
                c = Resolve(familyId);
                Cache[familyId] = c;
                return c;
            }

            private static Color Resolve(string id)
            {
                switch (id)
                {
                    case "love":     return Love;
                    case "family":   return Family;
                    case "violence": return Violence;
                    case "loss":     return Loss;
                    case "craft":    return Craft;
                    case "learning": return Learning;
                    case "faith":    return Faith;
                    case "journey":  return Journey;
                    case "wonder":   return Wonder;
                    case "vice":     return Vice;
                }
                // Stable, non-negative, id-keyed slot. Deliberately NOT string.GetHashCode:
                // .NET does not guarantee that is stable across runtimes, and "my timeline
                // changed colour after an update" is a bug nobody would think to report.
                int h = 17;
                for (int i = 0; i < id.Length; i++) h = unchecked(h * 31 + id[i]);
                return Fallback[(h & 0x7FFFFFFF) % Fallback.Length];
            }
        }
    }
}
