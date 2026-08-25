// Radius UI Framework - Tokens/Metrics.Tabs.cs
//
// ADDED 2026-08-20. ADDITIVE, new partial file, nothing existing touched.
//
// The standard footprint for a Radius UI module that REPLACES a vanilla overview tab
// (Health, Needs, Social, Bio/Character, Gear, Log, Training...). See ARCHITECTURE §17.
//
// Pure data only - Tokens must not read game state (§2). The live screen clamp that turns
// these numbers into a usable size lives in Panes/OverviewTab, which is allowed to.

namespace RadiusUI.Framework
{
    public static partial class Metrics
    {
        /// <summary>
        /// Standard width of a pane-replacing overview tab, at scale 1.
        ///
        /// <para>Value taken from Radius UI - Health Tab, which set the suite's precedent and
        /// which Needs Tab already matches exactly. A player flips between these tabs
        /// constantly in the same inspect pane, so ANY difference in footprint reads as the
        /// pane glitching rather than as two different panels.</para>
        /// </summary>
        public const float OverviewTabW = 890f;

        /// <summary>
        /// Standard height of a pane-replacing overview tab, at scale 1.
        ///
        /// <para>790 is not arbitrary: a humanlike pawn has ~13 capacities, and at the Health
        /// Tab's 46px row height the Overall card plus that list needs ~722px on top of the
        /// identity band. The previous 620 could not show them without a scrollbar, and
        /// scrolling that column hides capacities, which is the one thing it exists to
        /// prevent.</para>
        ///
        /// <para>ALWAYS clamp this against <see cref="OverviewTab.MaxHeight"/> before use. A
        /// bare 790 overflows the top of the screen at low resolution or high UI scale.</para>
        /// </summary>
        public const float OverviewTabH = 790f;

        /// <summary>Lower bound of the user's tab-size slider. Below this the content stops
        /// reflowing usefully and starts truncating.</summary>
        public const float OverviewTabScaleMin = 0.70f;

        /// <summary>Upper bound of the user's tab-size slider.</summary>
        public const float OverviewTabScaleMax = 1.10f;

        /// <summary>
        /// Default tab-size multiplier: 1.00, i.e. the standard footprint unscaled.
        ///
        /// <para>This is part of the standard, not a per-mod taste. Two suite tabs that agree on
        /// their base constants but ship different DEFAULT scales still render at different
        /// sizes for every player who never opens the settings - which is nearly all of
        /// them.</para>
        /// </summary>
        public const float OverviewTabScaleDefault = 1.00f;
    }
}
