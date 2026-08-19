// Radius UI Framework - Tokens/Metrics.cs
//
// Canonical layout metrics. Pure data. Radii here are BASE values; CardChrome
// applies the user's RadiusTheme.RadiusScale on top, so consumers pass these
// constants and never pre-multiply.

namespace RadiusUI.Framework
{
    public static class Metrics
    {
        // ------------------------------------------------------------------ radii

        /// <summary>Base corner radius for cards and panels.</summary>
        public const float RadiusCard = 12f;

        /// <summary>Base corner radius for chips, bands, small tiles.</summary>
        public const float RadiusChip = 8f;

        // ------------------------------------------------------------------ spacing
        // One spacing scale for the whole suite. Pick from these, never invent.

        public const float Space4 = 4f;
        public const float Space8 = 8f;
        public const float Space16 = 16f;
        public const float Space24 = 24f;

        /// <summary>Inner padding of a card or panel.</summary>
        public const float PadCard = 10f;

        // ------------------------------------------------------------------ rows and controls

        /// <summary>Single-line text row height for GameFont.Small (never go below 22).</summary>
        public const float RowText = 23f;

        /// <summary>Standard button height.</summary>
        public const float ButtonH = 30f;

        /// <summary>Section header row height.</summary>
        public const float HeaderH = 24f;

        /// <summary>Standard progress/need bar height (pill).</summary>
        public const float BarH = 7f;

        /// <summary>Width reserved for a vertical scrollbar.</summary>
        public const float ScrollGutter = 16f;

        // ---- Added 2026-08-17 (Quest Menu session; values from the approved combo ----
        // ---- mockup stylesheet). General-purpose - use rather than redefining.     ----

        /// <summary>Window / top-level content padding.</summary>
        public const float PadWindow = 16f;

        /// <summary>Sticky section/group bar height in feeds and lists.</summary>
        public const float SectionBarH = 26f;

        /// <summary>Square icon plate in list rows.</summary>
        public const float IconPlate = 30f;

        /// <summary>Larger icon plate in headers and detail views.</summary>
        public const float IconPlateLarge = 34f;

        /// <summary>List row height: icon plate plus two text lines.</summary>
        public const float RowList = 46f;

        /// <summary>
        /// Slim scrollbar gutter (additive, 2026-06-10). FlatScroll renders its bar at this
        /// width; reserve THIS in new layouts. ScrollGutter (16) stays for old call sites
        /// per the API contract - reserving 16 over a 10px bar is harmless slack.
        /// </summary>
        public const float ScrollGutterSlim = 10f;
    }
}
