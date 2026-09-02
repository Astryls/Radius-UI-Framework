// Radius UI Framework - Tokens/Palette.Policies.cs
//
// ADDED generation 15 (2026-08-28). PURELY ADDITIVE: a new partial file on the existing
// Palette partial. Nothing existing is renamed and no existing VALUE moves (§13 rules 1, 2).
//
// WHY THIS EXISTS, AND WHY IT IS NOT "JUST USE Palette.Health".
// Radius UI - Policies draws five domains side by side - apparel, food, drugs, reading,
// medical - and every surface in it is keyed on the domain hue: the rail strip, the selected
// policy row, the vessel rim, the chip on every assignment, the bar in every readout. Two of
// those domains have an obvious neighbour in an existing set (medical wants a health hue, food
// wants something green) and reaching for them is exactly the fork this file prevents. The day
// Palette.Health retunes, the medical policy screen would silently follow it - with no error,
// because nothing is wrong, the colour just stopped meaning what it meant.
//
// The same call Palette.Commands made at generation 6 and Palette.Genes made at generation 10.
//
// AND WHY IT IS NOT Good/Warn/Bad. A domain is not a severity. Food being green must never
// read as "food is fine"; medical being teal must never read as "medical is cold". The
// semantic ramp answers "how bad is this" and stays free to do only that.
//
// Kit is the second half: flat stamped metalware for the food tray. It is CHROME, so it lives
// in the neutral vocabulary rather than gaining hues of its own - four steps that sit between
// Surface2 and StripGray. It has one consumer today, which is normally an argument against
// adding a token (see F1, and StaggeredScheduler's recorded non-build). It is here anyway
// because the alternative is a mod-local colour constant, and §10's rule is unconditional: a
// missing token is a framework decision, not a local constant.
//
// Thread affinity: none (constants + a memoised pure function).

using System.Collections.Generic;
using UnityEngine;

namespace RadiusUI.Framework
{
    public static partial class Palette
    {
        /// <summary>
        /// Policy-domain identities for <c>Radius UI - Policies</c>. A domain hue is an
        /// IDENTITY, not a severity - it answers "which policy am I editing", never "is this
        /// good". Pair with <see cref="Palette.Bands"/> for the "something is wrong" layer,
        /// which has its own dark fills and takes white ink.
        /// </summary>
        public static class Policies
        {
            /// <summary>Apparel policies. Cloth blue.</summary>
            public static readonly Color Apparel = new Color(0.498f, 0.659f, 0.910f);

            /// <summary>Food policies. Fresh green, deliberately clear of <see cref="Good"/>.</summary>
            public static readonly Color Food = new Color(0.561f, 0.788f, 0.420f);

            /// <summary>Drug policies. Pharmaceutical violet.</summary>
            public static readonly Color Drugs = new Color(0.780f, 0.608f, 0.949f);

            /// <summary>Reading policies. Paper amber, kept clear of <see cref="Warn"/>.</summary>
            public static readonly Color Reading = new Color(0.910f, 0.722f, 0.498f);

            /// <summary>Medical care defaults. Clinical teal. NOT a <see cref="Health"/> hue -
            /// see this file's header for why that distinction is load-bearing.</summary>
            public static readonly Color Medical = new Color(0.437f, 0.827f, 0.769f);

            /// <summary>
            /// Stable hue ring for a policy domain added by a mod we have no rule for. Never
            /// grey: grey reads as disabled, and an unrecognised domain is perfectly usable.
            /// Same shape and reasoning as <see cref="Palette.Commands"/>'s ring.
            /// </summary>
            public static readonly Color[] Fallback =
            {
                new Color(0.498f, 0.698f, 0.910f),
                new Color(0.910f, 0.655f, 0.498f),
                new Color(0.561f, 0.839f, 0.659f),
                new Color(0.839f, 0.624f, 0.910f),
                new Color(0.910f, 0.839f, 0.498f),
                new Color(0.498f, 0.839f, 0.839f),
                new Color(0.910f, 0.529f, 0.608f),
                new Color(0.659f, 0.722f, 0.910f),
            };

            private static readonly Dictionary<string, Color> Cache = new Dictionary<string, Color>(12);

            /// <summary>
            /// Hue for a domain id. Unknown ids get a stable slot from <see cref="Fallback"/>, so
            /// a mod-added domain is coloured consistently without the framework knowing it exists.
            ///
            /// <para>Takes a STRING, not a def type: Tokens depends on nothing (§2), and accepting
            /// a def would couple the palette to the def system.</para>
            /// </summary>
            public static Color For(string domainId)
            {
                if (string.IsNullOrEmpty(domainId)) return Fallback[0];
                if (Cache.TryGetValue(domainId, out Color c)) return c;
                c = Resolve(domainId);
                Cache[domainId] = c;
                return c;
            }

            private static Color Resolve(string id)
            {
                switch (id)
                {
                    case "apparel": return Apparel;
                    case "food":    return Food;
                    case "drugs":   return Drugs;
                    case "reading": return Reading;
                    case "medical": return Medical;
                }
                // Stable, non-negative, id-keyed slot. Deliberately NOT string.GetHashCode:
                // .NET does not guarantee that is stable across runtimes, and "my tab changed
                // colour after an update" is a bug nobody would think to report.
                int h = 17;
                for (int i = 0; i < id.Length; i++) h = unchecked(h * 31 + id[i]);
                return Fallback[(h & 0x7FFFFFFF) % Fallback.Length];
            }

            /// <summary>
            /// Flat stamped metalware, for a surface that has to read as a physical container
            /// rather than as a panel. Four neutral steps, no hues: the kit is chrome, and a
            /// coloured tray would compete with the food sitting in it.
            ///
            /// <para>Drawn as construction, never as shading - a face, a pressed basin, a rim
            /// that catches, and a softer inner lip. There is no gradient anywhere in this
            /// suite and this does not introduce one.</para>
            /// </summary>
            public static class Kit
            {
                /// <summary>The tray body.</summary>
                public static readonly Color Face = new Color(0.137f, 0.157f, 0.188f);

                /// <summary>A compartment pressed into the tray.</summary>
                public static readonly Color Basin = new Color(0.078f, 0.094f, 0.118f);

                /// <summary>The pressed edge that catches the light.</summary>
                public static readonly Color Rim = new Color(0.224f, 0.251f, 0.294f);

                /// <summary>A softer inner lip, one step in from <see cref="Rim"/>.</summary>
                public static readonly Color Edge = new Color(0.169f, 0.192f, 0.227f);
            }
        }
    }
}
