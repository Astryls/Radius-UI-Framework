// Radius UI Framework - Tokens/Palette.Commands.cs
//
// ADDED generation 6 (2026-08-20). PURELY ADDITIVE: a new partial on the existing Palette
// partial. Nothing existing is renamed and no existing VALUE moves (§13 rules 1 and 2).
//
// WHY THIS EXISTS, AND WHY IT IS NOT "JUST USE Palette.Health".
// Radius UI - Gizmos colours a pawn command by its DOMAIN - combat, abilities, orders, work,
// social. Every one of its five design studies leaned on that hue, and every one of them was
// borrowing Palette.Health and Palette.Needs identities to get it, because no command domain
// set existed. Those tokens answer a different question ("what KIND of body part is this",
// "which need is this") and a consumer reading Palette.Health.Consciousness in command code is
// a fork waiting to happen: the day the health domain retunes its purple, every command wheel
// in the suite silently follows it.
//
// So the NAMES are new and the values are pinned here. They currently COINCIDE with the
// health/needs hues, which is deliberate - the approved mockup is the spec and this is what it
// shows - but the two sets are now free to move independently, which is the entire point.
//
// This is the same call Palette.Health made in its own header: a domain set answers "what kind
// of thing is this", the semantic ramp answers "how bad is this". A command's category never
// means "good" or "bad", so it must never be drawn from Good/Warn/Bad.
//
// Thread affinity: none (constants + a memoised pure function).

using System.Collections.Generic;
using UnityEngine;

namespace RadiusUI.Framework
{
    public static partial class Palette
    {
        /// <summary>
        /// Command-domain identities for pawn command surfaces (Radius UI - Gizmos). A category
        /// hue is an IDENTITY, not a severity: combat is red because it is combat, never because
        /// something is wrong. Pair with <see cref="Palette.Bands"/> for the "something IS wrong"
        /// layer, which has its own dark fills and takes white ink.
        /// </summary>
        public static class Commands
        {
            /// <summary>Draft, attack, stances, weapon swaps, ammo.</summary>
            public static readonly Color Combat = new Color(0.90f, 0.30f, 0.30f);

            /// <summary>Psycasts, spells, and anything else cast from a pool.</summary>
            public static readonly Color Abilities = new Color(0.62f, 0.45f, 0.85f);

            /// <summary>Positional and one-shot instructions: wait, carry, drop, rest, tend.</summary>
            public static readonly Color Orders = new Color(0.55f, 0.85f, 0.95f);

            /// <summary>Policies, priorities, apparel, medicine - the standing settings.</summary>
            public static readonly Color Work = new Color(1.00f, 0.710f, 0.271f);

            /// <summary>Interactions aimed at another pawn.</summary>
            public static readonly Color Social = new Color(1.00f, 0.561f, 0.816f);

            /// <summary>
            /// The player's own pinned set. Aliases <see cref="Palette.Pin"/> rather than
            /// restating a near-identical gold: "pinned" already has a colour in this suite and
            /// two names for one meaning is how a palette starts to drift.
            /// </summary>
            public static readonly Color Pinned = Pin;

            /// <summary>Developer-only commands. Aliases <see cref="Palette.DevAccent"/>, which
            /// exists precisely so a dev control never reads as a statement about the subject.</summary>
            public static readonly Color Dev = DevAccent;

            /// <summary>
            /// A non-Command gizmo: a status readout, not an action (a shield bar, a psyfocus
            /// meter, a growth tracker). Deliberately a neutral grey - it is not a category, it
            /// cannot be fired, and colouring it would imply it could be.
            /// </summary>
            public static readonly Color Status = OkGray;

            /// <summary>
            /// Stable hue ring for commands from a mod we have no rule for. Never grey: grey
            /// reads as disabled, and an unrecognised command is perfectly usable. Same reasoning
            /// and same shape as <see cref="Palette.Needs"/>'s fallback ring.
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
            /// Hue for a category id. Unknown ids get a stable slot from <see cref="Fallback"/>, so
            /// a mod-added category is coloured consistently without the framework knowing it exists.
            ///
            /// <para>Takes a STRING, not a def type: Tokens depends on nothing (§2), and accepting a
            /// def would couple the palette to the def system.</para>
            /// </summary>
            public static Color For(string categoryId)
            {
                if (string.IsNullOrEmpty(categoryId)) return Fallback[0];
                if (Cache.TryGetValue(categoryId, out Color c)) return c;
                c = Resolve(categoryId);
                Cache[categoryId] = c;
                return c;
            }

            private static Color Resolve(string id)
            {
                switch (id)
                {
                    case "combat":    return Combat;
                    case "abilities": return Abilities;
                    case "ability":   return Abilities;
                    case "orders":    return Orders;
                    case "order":     return Orders;
                    case "work":      return Work;
                    case "social":    return Social;
                    case "pinned":    return Pinned;
                    case "dev":       return Dev;
                    case "status":    return Status;
                }
                // Stable, non-negative, id-keyed slot. Deliberately NOT string.GetHashCode: .NET
                // does not guarantee that is stable across runtimes, and "my commands changed
                // colour after an update" is a bug nobody would think to report.
                int h = 17;
                for (int i = 0; i < id.Length; i++) h = unchecked(h * 31 + id[i]);
                return Fallback[(h & 0x7FFFFFFF) % Fallback.Length];
            }
        }
    }
}
