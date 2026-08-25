// Radius UI Framework - Tokens/Metrics.Rows.cs
//
// ADDED generation 4 (2026-08-19). ADDITIVE, new partial file, nothing existing touched.

namespace RadiusUI.Framework
{
    public static partial class Metrics
    {
        /// <summary>
        /// Compact list row: a small portrait, one line of text and a value. 34px.
        ///
        /// <para>Exists because the scale had a real gap. <see cref="RowText"/> (23) is a single
        /// line of text with no art, and <see cref="RowList"/> (46) is an icon plate plus two text
        /// lines. A row with a 22-26px portrait and one line of text is neither, and three
        /// consumers were about to invent 34 locally - which is exactly the drift the spacing
        /// scale exists to prevent.</para>
        /// </summary>
        public const float RowCompact = 34f;

        /// <summary>
        /// A one-line identity strip: portrait, name, and a value cluster. 44px.
        ///
        /// <para>The compressed form of a detail header, for panels too short to spend 90px on a
        /// card that mostly restates the selected row.</para>
        /// </summary>
        public const float StripIdentity = 44f;
    }
}
