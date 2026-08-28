// Radius UI Framework - Tokens/Palette.Needs.cs
//
// ADDED 2026-08-19. ADDITIVE ONLY: nothing existing is touched, no value changed.
//
// A NAMED DOMAIN SET, exactly like Palette.Health and Palette.Ideology - not an extension of the
// semantic ramp. Good/Warn/Bad answer "how bad is this". These answer "what KIND of thing is
// this". A need's hue is an IDENTITY: recreation is the same hue at 7% and at 98%, and the ramp
// is what says which of those is a problem.
//
// It lives in the framework rather than in the Needs Tab because Colonist Bar's need readouts
// and the HUD both want the same hues, and a colour must not be able to drift between the two
// screens a player compares side by side.
//
// DELIBERATELY NOT THE ACCENT. The accent is the user's choice and is already spoken for by
// "selected / active". A need tinted with it would read as selected.
//
// THE FALLBACK IS THE IMPORTANT PART. A modded need can never have an entry here, and there is
// no upper bound on how many a load order can add, so `For(def)` hashes the defName onto a
// stable ring of distinguishable hues. Two properties matter:
//   * STABLE - the same need gets the same colour every session, so muscle memory works.
//   * NEVER GREY - grey reads as "disabled" in this suite (StripGray), and a modded need that
//     looks disabled is a bug report.

using System.Collections.Generic;
using UnityEngine;

namespace RadiusUI.Framework
{
    public static partial class Palette
    {
        /// <summary>
        /// Need identity hues. "What kind of need is this", never "how bad is it".
        /// Thread affinity: read-only after type init; safe from any thread.
        /// </summary>
        public static class Needs
        {
            // ---- vanilla identities -------------------------------------------------
            // Chosen to stay clear of each other AND of the semantic ramp, so a need's own
            // colour can never be mistaken for a severity reading.

            /// <summary>Mood. The need a player checks first, so it takes the warmest,
            /// highest-attention hue in the set. #ffb545</summary>
            public static readonly Color Mood = new Color(1.00f, 0.710f, 0.271f);

            /// <summary>Food / hunger. #8ed46a</summary>
            public static readonly Color Food = new Color(0.557f, 0.831f, 0.416f);

            /// <summary>Rest / sleep. #7f9cff</summary>
            public static readonly Color Rest = new Color(0.498f, 0.612f, 1.00f);

            /// <summary>Recreation / joy. #ff8fd0</summary>
            public static readonly Color Recreation = new Color(1.00f, 0.561f, 0.816f);

            /// <summary>Beauty. #c79cff</summary>
            public static readonly Color Beauty = new Color(0.780f, 0.612f, 1.00f);

            /// <summary>Comfort. #6fd5c4</summary>
            public static readonly Color Comfort = new Color(0.435f, 0.835f, 0.769f);

            /// <summary>Outdoors / indoors. #7fc7d4</summary>
            public static readonly Color Outdoors = new Color(0.498f, 0.780f, 0.831f);

            /// <summary>Chemical / drug need. Deliberately close to
            /// <see cref="Palette.DevAccent"/>'s family without being it. #b58cff</summary>
            public static readonly Color Chemical = new Color(0.710f, 0.549f, 1.00f);

            /// <summary>Learning (Biotech children). #ffd77a</summary>
            public static readonly Color Learning = new Color(1.00f, 0.843f, 0.478f);

            /// <summary>Suppression (Ideology slaves). #d98a6a</summary>
            public static readonly Color Suppression = new Color(0.851f, 0.541f, 0.416f);

            /// <summary>Deathrest (Biotech sanguophages). #a86fd4</summary>
            public static readonly Color Deathrest = new Color(0.659f, 0.435f, 0.831f);

            // ---- the fallback ring --------------------------------------------------

            /// <summary>
            /// Hues an unrecognised need is assigned from. Every one is distinguishable from every
            /// other at a 7px bar width, and none is grey (grey means "disabled" in this suite).
            /// </summary>
            public static readonly Color[] Fallback =
            {
                new Color(0.451f, 0.780f, 0.937f), // cyan
                new Color(0.937f, 0.702f, 0.451f), // sand
                new Color(0.686f, 0.878f, 0.510f), // moss
                new Color(0.937f, 0.549f, 0.549f), // clay
                new Color(0.643f, 0.647f, 0.937f), // periwinkle
                new Color(0.937f, 0.851f, 0.478f), // straw
                new Color(0.541f, 0.902f, 0.788f), // mint
                new Color(0.902f, 0.612f, 0.831f), // orchid
            };

            // defName -> resolved colour. Resolved once per def, then a dictionary hit.
            private static readonly Dictionary<string, Color> Cache = new Dictionary<string, Color>(32);

            /// <summary>
            /// The identity colour for a need. Known vanilla and DLC needs get their named hue;
            /// anything else gets a stable slot from <see cref="Fallback"/>, keyed on defName so
            /// it never changes between sessions.
            ///
            /// <para>Takes the <c>defName</c> rather than the <c>NeedDef</c> deliberately:
            /// ARCHITECTURE §2 requires that <c>Tokens</c> depend on nothing, and accepting a
            /// RimWorld def type here would quietly couple the palette to the def system. Callers
            /// pass <c>def.defName</c>.</para>
            ///
            /// <para>Cost: one dictionary hit after the first call per def. Never allocates on the
            /// hot path. Thread affinity: OnGUI main thread (the cache is not synchronised).</para>
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
                    case "Mood":        return Mood;
                    case "Food":        return Food;
                    case "Rest":        return Rest;
                    case "Joy":         return Recreation;
                    case "Beauty":      return Beauty;
                    case "Comfort":     return Comfort;
                    case "Outdoors":    return Outdoors;
                    case "Indoors":     return Outdoors;
                    case "RoomSize":    return Comfort;
                    case "Learning":    return Learning;
                    case "Suppression": return Suppression;
                    case "Deathrest":   return Deathrest;
                    case "KillThirst":  return Suppression;
                    case "MechEnergy":  return Chemical;
                }
                // Chemical needs are generated per drug (Chemical_Alcohol, Chemical_GoJuice, ...),
                // so they are matched by stem rather than listed - a modded drug then reads as a
                // chemical need automatically instead of falling through to the ring.
                if (defName.StartsWith("Chemical", System.StringComparison.Ordinal)) return Chemical;

                // Stable, non-negative, defName-keyed slot. Deliberately NOT string.GetHashCode:
                // .NET does not guarantee that is stable across runtimes, and "the same need
                // changed colour after an update" is a bug nobody would think to report.
                int h = 17;
                for (int i = 0; i < defName.Length; i++) h = unchecked(h * 31 + defName[i]);
                return Fallback[(h & 0x7FFFFFFF) % Fallback.Length];
            }
        }
    }
}
