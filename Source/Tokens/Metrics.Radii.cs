// Radius UI Framework - Tokens/Metrics.Radii.cs
//
// ADDED generation 14 (2026-08-27). PURELY ADDITIVE: a NEW partial file on the existing
// Metrics partial, per §13's concurrent-edit rule. Nothing existing is renamed and no
// existing VALUE moves (§13 rules 1 and 2).
//
// WHY THIS EXISTS. The suite's row radius is 10 and always has been - Spatial.RowPlate bakes
// exactly that into its atlas - but the number was never NAMED, so a consumer drawing a row
// through CardChrome (one GPU quad) rather than Spatial (nine draw calls) had no token to
// reach for and had to type a literal 10f. That is how a spacing scale starts to drift: not
// through disagreement, but because the right value has no name and the nearest named one is
// wrong by two pixels either way.
//
// Metrics.cs already states the intent in its header - "Radii here are BASE values; CardChrome
// applies the user's RadiusTheme.RadiusScale on top, so consumers pass these constants and
// never pre-multiply." This closes the gap between that sentence and what was actually
// available to pass.

namespace RadiusUI.Framework
{
    public static partial class Metrics
    {
        /// <summary>
        /// Base corner radius for a ROW plate: a list row, a timeline row, a tab segment, a
        /// filter pill's plate. Sits between <see cref="RadiusChip"/> (8, for chips and small
        /// tiles) and <see cref="RadiusCard"/> (12, for cards and panels).
        ///
        /// <para>This is the same 10 that <c>Spatial.RowPlate</c> bakes into its atlas, named
        /// so a consumer drawing the same shape through <c>CardChrome.Rounded</c> - one GPU
        /// quad instead of a nine-call 9-slice - lands on the identical radius instead of
        /// guessing. Per-row work should prefer the CardChrome path; Spatial's atlas is for
        /// surfaces and shapes a rounded rect cannot express.</para>
        /// </summary>
        public const float RadiusRow = 10f;
    }
}
