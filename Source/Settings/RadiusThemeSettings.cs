using UnityEngine;
using Verse;

namespace RadiusUI.Framework
{
    /// <summary>
    /// Persisted, suite-wide theme choices (vanilla ModSettings; config-file backed, never
    /// world state, so the framework is safe to add or remove mid-save).
    ///
    /// Consumers never read this class directly - they go through <see cref="RadiusTheme"/>,
    /// which caches these values for zero-cost per-frame reads. After any field changes,
    /// call <see cref="RadiusTheme.Notify_SettingsChanged"/> (the settings UI does).
    /// </summary>
    public class RadiusThemeSettings : ModSettings
    {
        /// <summary>Index into <see cref="Palette.Accents"/>. Default 0 = Sky.</summary>
        public int accentIndex;

        /// <summary>Draw suite headings in real bold (dynamic-font bold face).</summary>
        public bool fontBold;

        /// <summary>Draw suite text italic.</summary>
        public bool fontItalic;

        /// <summary>Corner radius multiplier. 1 = designed radii, 0 = fully square.</summary>
        public float radiusScale = 1f;

        /// <summary>
        /// Suite-wide multiplier on BACKGROUND refresh cadences (see
        /// <see cref="Throttle.EveryScaled"/>). 1 = designed rate; HIGHER IS SLOWER AND
        /// CHEAPER (2 = half as often). Lets a player on a large colony trade freshness for
        /// frame time once, for every Radius UI mod at once.
        ///
        /// Only affects background gathers. Foreground cadences the player is actively
        /// watching use <see cref="Throttle.Every"/> and deliberately ignore this.
        /// </summary>
        public float refreshMult = 1f;

        /// <summary>Reserved for a future OS-font override (framework ARCHITECTURE §9).
        /// Scribed now so enabling it later does not churn player configs. No UI yet.</summary>
        public string fontName = "";

        /// <summary>Upper bound for <see cref="radiusScale"/> (slightly rounder than designed).</summary>
        public const float MaxRadiusScale = 1.5f;

        /// <summary>Lower bound for <see cref="refreshMult"/> (twice as fresh, twice the cost).</summary>
        public const float MinRefreshMult = 0.5f;

        /// <summary>Upper bound for <see cref="refreshMult"/> (quarter rate, cheapest).</summary>
        public const float MaxRefreshMult = 4f;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref accentIndex, "accentIndex", 0);
            Scribe_Values.Look(ref fontBold, "fontBold", defaultValue: false);
            Scribe_Values.Look(ref fontItalic, "fontItalic", defaultValue: false);
            Scribe_Values.Look(ref radiusScale, "radiusScale", 1f);
            Scribe_Values.Look(ref refreshMult, "refreshMult", 1f);
            Scribe_Values.Look(ref fontName, "fontName", "");

            // Heal bad persisted values on load. NaN passes ordinary comparison guards and
            // Mathf.Clamp(NaN) stays NaN, so scrub explicitly before clamping.
            if (float.IsNaN(radiusScale) || float.IsInfinity(radiusScale))
            {
                radiusScale = 1f;
            }
            if (float.IsNaN(refreshMult) || float.IsInfinity(refreshMult))
            {
                refreshMult = 1f;
            }
            radiusScale = Mathf.Clamp(radiusScale, 0f, MaxRadiusScale);
            refreshMult = Mathf.Clamp(refreshMult, MinRefreshMult, MaxRefreshMult);
            accentIndex = Mathf.Clamp(accentIndex, 0, Palette.Accents.Length - 1);
        }

        /// <summary>Restore every setting to its shipped default (settings "reset" button).</summary>
        public void ResetToDefaults()
        {
            accentIndex = 0;
            fontBold = false;
            fontItalic = false;
            radiusScale = 1f;
            refreshMult = 1f;
            fontName = "";
        }
    }
}
