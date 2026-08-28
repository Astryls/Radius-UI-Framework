// Radius UI Framework - Tokens/Palette.Bands.cs
//
// ADDED 2026-08-19. ADDITIVE ONLY: nothing existing is touched, no value changed.
//
// THE ALERT BAND'S OWN FILL SET, and why it is not the semantic ramp.
//
// The suite has two band conventions in production, and they are NOT interchangeable:
//
//   Radius UI - Health Tab (DrawNowBand): 33px, filled straight from the semantic ramp
//     (Palette.Bad / Warn), text drawn in near-black InkOnAccent. Legible because the ramp
//     colours are BRIGHT and the ink on them is dark.
//
//   Radius UI - Colonist Bar (docs/specs/07-ALERT-BANDS.md): 38px, filled from the four values
//     below, text drawn WHITE. Legible because these fills are DARK.
//
// Putting white text on Palette.Bad, or dark text on BandCritical, fails contrast in both
// directions - which is exactly why these are separate named tokens rather than "the ramp,
// darker". A band is the one element in the suite allowed to be a solid block of colour, so
// it must stay rare and it must stay readable.
//
// Values transcribed verbatim from 07-ALERT-BANDS.md so the Colonist Bar can drop its local
// literals onto these names without a single pixel changing (§13 rule 2).

using UnityEngine;

namespace RadiusUI.Framework
{
    public static partial class Palette
    {
        /// <summary>Alert band fill, critical: bleeding out, downed, mental break in progress,
        /// a need at zero. Pairs with WHITE text, never with <see cref="InkOnAccent"/>. #b3372e</summary>
        public static readonly Color BandCritical = new Color(0.702f, 0.216f, 0.180f);

        /// <summary>Alert band fill, warning: break risk rising, starving, a need below its
        /// threshold. Pairs with WHITE text. #c8860d</summary>
        public static readonly Color BandWarning = new Color(0.784f, 0.525f, 0.051f);

        /// <summary>Alert band fill, cold: hypothermia, heatstroke. A separate hue because
        /// temperature danger reads wrong in amber. Pairs with WHITE text. #2e6e6b</summary>
        public static readonly Color BandCold = new Color(0.180f, 0.431f, 0.420f);

        /// <summary>Alert band fill, info: needs tending, a forecast worth knowing but not yet
        /// urgent. Pairs with WHITE text. #1f5fa8</summary>
        public static readonly Color BandInfo = new Color(0.122f, 0.373f, 0.659f);

        /// <summary>Text and glyph colour drawn ON a Band* fill. Deliberately NOT
        /// <see cref="InkOnAccent"/>, which is near-black and disappears on these dark fills.</summary>
        public static readonly Color BandInk = new Color(1f, 1f, 1f);

        /// <summary>Secondary text on a Band* fill - the right-hand meta slot (an ETA, a count).
        /// White at 82%, per 07-ALERT-BANDS.md.</summary>
        public static readonly Color BandInkDim = new Color(1f, 1f, 1f, 0.82f);

        /// <summary>The severity disc behind a band's glyph. Black at 34%, per the spec.</summary>
        public static readonly Color BandDisc = new Color(0f, 0f, 0f, 0.34f);

        /// <summary>Inactive cycle dot on a band. White at 34%; the active dot is
        /// <see cref="BandInk"/>.</summary>
        public static readonly Color BandDotIdle = new Color(1f, 1f, 1f, 0.34f);

        /// <summary>The 2px cycle progress line along a band's bottom edge. White at 45%.</summary>
        public static readonly Color BandProgress = new Color(1f, 1f, 1f, 0.45f);
    }
}
