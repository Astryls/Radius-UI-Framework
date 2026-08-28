// Radius UI Framework - Tokens/Palette.Health.cs
//
// Health-domain colour. Lives in the framework rather than in the Health Tab for
// one concrete reason: the framework already OWNS the art these colours pair with
// (Textures/RadiusUI/Marker/ and Anatomy/ and Medical/). A colour and the PNG it is
// drawn beside must not be able to drift apart across two mods.
//
// This is a NAMED DOMAIN SET, not an extension of the semantic ramp. Good/Warn/Bad
// answer "how bad is this"; these answer "what KIND of thing is this". A capacity
// hue is an identity, not a severity - Moving is blue whether it reads 8% or 180%.

using UnityEngine;

namespace RadiusUI.Framework
{
    public static partial class Palette
    {
        /// <summary>
        /// Health-domain colour identities. Consumers: Radius UI - Health Tab, and the
        /// health module of Radius UI - Colonist Bar.
        /// </summary>
        public static class Health
        {
            // ─────────────────────────────────────────────────────────────
            //  Capacity identities
            //  One hue per pawn capacity, so a capacity is recognisable by
            //  colour alone across the tab, the card column and the bar.
            //  These are IDENTITIES: they do not change with the value.
            //  Deliberately kept as a curated set rather than collapsed onto
            //  the accent family (decision 2026-06-09) - the accent is the
            //  user's choice and must stay free to mean "selected/active".
            // ─────────────────────────────────────────────────────────────

            /// <summary>Consciousness capacity identity.</summary>
            public static readonly Color Consciousness   = new Color(0.62f, 0.45f, 0.85f);
            /// <summary>Manipulation capacity identity.</summary>
            public static readonly Color Manipulation    = new Color(0.95f, 0.62f, 0.25f);
            /// <summary>Talking capacity identity.</summary>
            public static readonly Color Talking         = new Color(0.40f, 0.85f, 0.85f);
            /// <summary>Eating capacity identity.</summary>
            public static readonly Color Eating          = new Color(0.95f, 0.78f, 0.30f);
            /// <summary>Breathing capacity identity.</summary>
            public static readonly Color Breathing       = new Color(0.55f, 0.85f, 0.95f);
            /// <summary>Blood pumping capacity identity.</summary>
            public static readonly Color BloodPumping    = new Color(0.90f, 0.30f, 0.30f);
            /// <summary>Blood filtration capacity identity.</summary>
            public static readonly Color BloodFiltration = new Color(0.75f, 0.20f, 0.45f);
            /// <summary>Sight capacity identity.</summary>
            public static readonly Color Sight           = new Color(0.95f, 0.85f, 0.25f);
            /// <summary>Hearing capacity identity.</summary>
            public static readonly Color Hearing         = new Color(0.40f, 0.85f, 0.50f);
            // Moving deliberately has no constant: it resolves to the live accent, because
            // movement is the capacity a player checks first and it earns the theme colour.

            // ─────────────────────────────────────────────────────────────
            //  Marker vignettes
            //  These pair 1:1 with Textures/RadiusUI/Marker/*.png, whose own
            //  pixels are BAKED in these hues (tier 3: the colour IS the
            //  meaning). If a value here changes, the matching PNG must be
            //  re-authored by hand - see ARCHITECTURE §11.
            //
            //  Alpha is carried in the token because a vignette is always a
            //  wash; a caller that wants it solid should say so explicitly.
            // ─────────────────────────────────────────────────────────────

            /// <summary>Bleeding. Paired with Marker/Blood.png, whose pixels are baked in this hue.</summary>
            public static readonly Color MarkerBlood   = new Color(0.75f, 0.22f, 0.18f, 0.55f);
            /// <summary>Burns. Paired with Marker/Burn.png.</summary>
            public static readonly Color MarkerBurn    = new Color(0.92f, 0.46f, 0.12f, 0.55f);
            /// <summary>Frostbite / hypothermia. Paired with Marker/Frost.png.</summary>
            public static readonly Color MarkerFrost   = new Color(0.45f, 0.75f, 0.92f, 0.55f);
            /// <summary>Infection or disease. Paired with Marker/Virus.png.</summary>
            public static readonly Color MarkerVirus   = new Color(0.46f, 0.70f, 0.30f, 0.55f);
            /// <summary>Toxic buildup and environmental poisoning. Paired with Marker/Toxic.png.</summary>
            public static readonly Color MarkerToxic   = new Color(0.72f, 0.82f, 0.20f, 0.55f);
            /// <summary>A lasting, non-urgent condition. Paired with Marker/Chronic.png.</summary>
            public static readonly Color MarkerChronic = new Color(0.85f, 0.62f, 0.20f, 0.50f);

            // ─────────────────────────────────────────────────────────────
            //  Pulse partners
            //  The dim end of a breathing animation. Paired so the lerp never
            //  passes through a hue that means something else.
            // ─────────────────────────────────────────────────────────────

            /// <summary>Dim end of the "boosted capacity" breathe, partnered with <see cref="Over"/>.</summary>
            public static readonly Color PulseDim = new Color(0.28f, 0.48f, 0.70f);

            /// <summary>Dim end of the archotech breathe, partnered with <see cref="Archo"/>.</summary>
            public static readonly Color PulseDimGold = new Color(0.55f, 0.45f, 0.20f);

            /// <summary>
            /// Colour a capacity or stat by its "goodness", where 1 is normal and above 1 is
            /// enhanced: <see cref="Over"/> / <see cref="OkGray"/> / <see cref="Warn"/> /
            /// <see cref="Bad"/>.
            /// <para>Raised out of Health Tab 2026-06-17 so Colonist Bar's health module reads
            /// the same number the same colour instead of forking the thresholds - which is the
            /// entire reason this framework exists.</para>
            /// <para>Pure function; safe in a draw loop.</para>
            /// </summary>
            public static Color Severity(float goodness)
            {
                if (goodness > 1.001f) return Over;
                if (goodness >= 0.80f) return OkGray;
                if (goodness >= 0.40f) return Warn;
                return Bad;
            }
        }
    }
}
