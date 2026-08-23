// Radius UI Framework - Tokens/Palette.Social.cs
//
// ADDED generation 4 (2026-08-19). ADDITIVE, and a NEW PARTIAL FILE rather than an edit to
// Palette.cs, per §13's concurrent-edit rule ("prefer NEW partial files over editing existing
// ones"). Nothing in Palette.cs is touched.
//
// Two tokens the social domain needs and the existing set genuinely does not express. Both were
// raised as questions before being added, because §13 rule 3 says a missing token is a framework
// decision, not a local constant.

using UnityEngine;

namespace RadiusUI.Framework
{
    public static partial class Palette
    {
        /// <summary>
        /// A positive social bond - family, friends, lovers. The suite has drawn this in blue since
        /// Modern Social Tab ("blue for family and friends, red for anyone who resents them").
        ///
        /// <para><b>Why this is a token and not the accent.</b> It was previously drawn with
        /// <see cref="Accents"/>[0] Sky, which looks identical while the player leaves the accent on
        /// its default. The moment they pick Crimson or Rose, every liked pawn turns red and becomes
        /// indistinguishable from a rival - no error, no warning, and nothing in the code can detect
        /// it. The accent means "selected" suite-wide and must stay free to mean only that.</para>
        ///
        /// <para><b>Why it is not <see cref="Good"/>.</b> The semantic ramp answers "how bad is
        /// this". A bond answers "what KIND of thing is this", which is an identity, the same
        /// distinction <see cref="Palette.Health"/>'s capacity hues are built on. A friendship
        /// drawn in Good reads as a healthy reading rather than a relationship.</para>
        ///
        /// <para>Deliberately outside both, exactly as <see cref="Pin"/> and <see cref="Archo"/>
        /// are. #5AA9F0</para>
        /// </summary>
        public static readonly Color Bond = new Color(0.353f, 0.663f, 0.941f);

        /// <summary>
        /// The counterpart to <see cref="Bond"/>: a relationship carrying active resentment. Kept a
        /// touch deeper than <see cref="Bad"/> so a rival row and a critical health reading are not
        /// the same red when they appear in one panel. #D9534F
        /// </summary>
        public static readonly Color Rift = new Color(0.851f, 0.325f, 0.310f);

        /// <summary>
        /// A pawn's NAME inside a sentence of prose - interaction logs, social feeds, letters that
        /// name participants. Warm so the actors pick out of body text without shouting.
        ///
        /// <para>No existing token means this: <see cref="Archo"/> means archotech,
        /// <see cref="Pin"/> means player-marked, and the accent means selected. Reusing any of them
        /// would be the "same name, two meanings" trap §13 rule 3 exists to prevent. #C89B6A</para>
        /// </summary>
        public static readonly Color NameInk = new Color(0.784f, 0.608f, 0.416f);
    }
}
