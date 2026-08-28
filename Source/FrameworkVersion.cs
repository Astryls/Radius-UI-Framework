// Radius UI Framework - FrameworkVersion.cs
//
// WHY THIS EXISTS. Two consumers were being developed in parallel against this library.
// One of them added tokens it needed and rebuilt; the other had been compiled against the
// previous shape and died at runtime with:
//
//     MissingFieldException:  Field not found: ...Palette.Surface0
//     MissingMethodException: Method not found: ...IconSet.get_ActionViewOrgans()
//
// Both mods still COMPILED. Nothing failed at build time, because each was built against
// a framework DLL that was correct when it was built. The break appears only at runtime,
// in a DIFFERENT mod from the one that changed, with a message that names a member rather
// than the mod that needs rebuilding. That is expensive to diagnose from the log alone.
//
// So the framework states its shape, and a consumer says which shape it was built for.
// A mismatch then reports itself in one line naming BOTH mods and the fix.

using Verse;

namespace RadiusUI.Framework
{
    /// <summary>
    /// The framework's public-API generation. Thread affinity: none (constants).
    /// </summary>
    public static class FrameworkVersion
    {
        /// <summary>
        /// Bumped whenever the public surface CHANGES INCOMPATIBLY - a member removed, a
        /// member renamed, or the VALUE behind a name repurposed. Purely additive changes
        /// do not bump it: an older consumer keeps working against a newer framework.
        ///
        /// <para>History:
        /// 1 = initial component landing (Tokens/Icons/Text/Widgets/Settings).
        /// 2 = surface vocabulary reconciled to Surface0..3 / Border / Ink-TextMid-TextFaint;
        ///     IconSet's named alias accessors replaced by the Get() alias table;
        ///     HoverWash re-valued 0.14 -> 0.05 with ActiveWash taking 0.14.
        /// 3 = the ratified wash ladder lands (WashFaint/Wash/WashHover/WashStrong/WashSolid =
        ///     .03/.06/.10/.14/.22; user decision 2026-08-18). Border/BorderFaint/ActiveWash
        ///     become same-value aliases; HoverWash RE-VALUED .05 -> .10 to the ladder step -
        ///     the value behind a name changed, hence the bump. Also: ModCompat.IsActive
        ///     recognises Workshop "_steam"-suffixed ids; ModCompat.StaticValue coerces boxed
        ///     structs (TaggedString) for T=string; Throttle gains EveryTicks.
        ///     Additive within gen 3 (2026-08-18 compliance pass, no bump - an older consumer
        ///     keeps working): Widgets/UIKit (Button/IconButton/TagPill/SectionBar),
        ///     RadiusFont.Epoch, Perf/StaggeredScheduler, Perf/FrameProbe, Palette.Pin
        ///     (player-marked/bookmark yellow, deliberately outside the Good/Warn/Bad ramp).
        ///     A consumer that USES those must require 3.
        /// 4 = Chrome/Spatial lands (2026-08-19, Radius UI - Social Tab session): procedural
        ///     9-slice rounded surfaces, Well, Pill, Dot, Ring and neutral Elevate - the shape
        ///     vocabulary CardChrome deliberately does not cover (§10 F1). Retires FOUR forks
        ///     (Modern Social Tab, Modern Needs Tab, Colonist Bar, Health Tab). Also additive:
        ///     Palette.Bond / Palette.Rift (social identity pair, outside the accent AND outside
        ///     the semantic ramp), Palette.NameInk (a pawn name inside prose), UIKit.BiBar +
        ///     UIKit.OpinionBar, Metrics.RowCompact (34) + Metrics.StripIdentity (44).
        ///     UIKit and Metrics became `partial` so future additions land in new files.
        ///
        ///     NOTE ON THE BUMP. Every change at gen 4 is PURELY ADDITIVE, and the rule above
        ///     says additive changes need no bump. It is bumped anyway, deliberately: the
        ///     additions are large enough that a consumer needs a way to ASSERT their presence.
        ///     Without a bump, a consumer using Spatial against an older gen-3 DLL gets a raw
        ///     MissingMethodException - precisely the failure this class exists to turn into a
        ///     sentence. No existing value or meaning changed, and every existing consumer
        ///     (requiring 1, 2 or 3) still passes Require() unchanged.
        /// 5 = the alert-band vocabulary lands (2026-08-19, Radius UI - Needs Tab session).
        ///     PURELY ADDITIVE, same deliberate bump rationale as gen 4 - a consumer needs a way
        ///     to ASSERT these exist rather than discover their absence as a raw
        ///     MissingMethodException. Nothing existing was touched and no value moved.
        ///       Chrome/Spatial.Bands.cs : Spatial.BottomBand + Spatial.Glyph + Spatial.RCapsule.
        ///         Retires a FOURTH fork - BottomBand existed in Health Tab (Chrome/Spatial.cs:86),
        ///         Colonist Bar (McbSpatial.cs) and Modern Needs Tab (SpatialKit.cs:89). Spatial
        ///         gains `partial`, exactly as UIKit and Metrics did at gen 4.
        ///       Tokens/Palette.Bands.cs : BandCritical/Warning/Cold/Info + BandInk/BandInkDim/
        ///         BandDisc/BandDotIdle/BandProgress. The Colonist Bar band's OWN dark fill set,
        ///         which is NOT the semantic ramp - these take white text, the ramp takes
        ///         InkOnAccent, and swapping either way fails contrast.
        ///       Tokens/Palette.Needs.cs : Palette.Needs domain set (11 identities + an 8-hue
        ///         stable fallback ring keyed on defName, so a modded need gets a consistent,
        ///         never-grey colour).
        /// 6 = the radial vocabulary lands (2026-08-20, Radius UI - Gizmos session). PURELY
        ///     ADDITIVE, same deliberate bump rationale as gen 4 and 5 - a consumer needs a way
        ///     to ASSERT these exist rather than discover their absence as a raw
        ///     MissingMethodException. Nothing existing was touched and no value moved.
        ///       Chrome/Spatial.Arc.cs : Spatial.AnnulusSector + ArcStroke + AngleFromUp +
        ///         InSector. The wedge - the one shape Spatial had no answer for and the only
        ///         genuinely new drawing primitive the Gizmos mockup set asked for. ONE cached
        ///         texture per SHAPE (inner ratio + sweep), rotated to its bearing, so a
        ///         twelve-sector wheel is twelve draw calls off two or three textures rather
        ///         than ~180 thin quads with double-composited seams. AngleFromUp/InSector ship
        ///         WITH it so a consumer's hit test cannot drift from the drawn shape.
        ///       Tokens/Palette.Commands.cs : the command-domain identity set (Combat/Abilities/
        ///         Orders/Work/Social + Pinned/Dev/Status aliases + an 8-hue stable fallback ring
        ///         keyed on category id). Exists because all five Gizmos studies were borrowing
        ///         Palette.Health and Palette.Needs identities to colour a command by domain -
        ///         tokens that answer a DIFFERENT question, so the day the health domain retunes
        ///         its purple every command wheel in the suite would silently follow. Values
        ///         currently coincide with those hues by design lineage; the two sets are now
        ///         free to move independently, which is the point.
        ///       Chrome/SizeAnim.cs : frame-rate independent panel size easing, advanced AT MOST
        ///         ONCE PER FRAME through FrameGate. Third occurrence (Inspector pane height, HUD
        ///         card fold, Gizmos drawer collapsing in both axes). The once-per-frame rule is
        ///         the whole point: a layout getter is evaluated several times per frame, and an
        ///         animation that advances on every call lets two calls in one frame disagree -
        ///         which strands "settled" forever and presents as a blank panel with no
        ///         exception in the log.</para>
        ///
        /// <para>7 = the World layer becomes ASSERTABLE (2026-08-21, Radius UI - Quest Menu
        ///     session). PURELY ADDITIVE - nothing existing was touched and no value moved.
        ///     `World/WorldSnapshot.cs` has physically existed since 2026-08-19 and was already
        ///     consumed by Faction Menu (StagePane) and Ideology (spread plot), but it was never
        ///     named in this history, so NO consumer could assert it. That is the same hole that
        ///     shipped a crash to users four days ago: RadiusFont.Epoch landed additively inside
        ///     generation 3, Quest Menu asserted 3, an early gen-3 framework passed the check and
        ///     then threw
        ///         MissingMethodException: Method not found: int RadiusFont.get_Epoch()
        ///     about a hundred times a frame. Naming the layer and bumping is what makes the
        ///     guard mean something.
        ///       World/WorldSnapshot.cs : real baked terrain for a world-map view - Get() (the
        ///         bake, overscan + quantized zoom ladder), Basis/Transport (view frame),
        ///         ProjectWorld/Unproject (tile &lt;-&gt; screen), PlanetRadius, BiomeColor and the
        ///         water/river colours.
        ///
        ///     RULE, restated because it has now cost real user-facing crashes twice: a consumer
        ///     must require the FIRST generation THAT CANNOT PREDATE the members it uses. For
        ///     anything in World/, that is 7 - NOT 6, even though the files existed at 6, because
        ///     an early gen-6 build is not required to carry today's surface.</para>
        ///
        /// <para>8 = WorldSnapshot.Get's seed parameter is FIXED (2026-08-21, same session).
        ///     NOT additive - this changes a signature AND the behaviour behind an existing
        ///     name, so it bumps under the original rule.
        ///       `Get(..., PlanetTile seed = default)` becomes `PlanetTile? seed = null`, and Get
        ///       substitutes `center` when no seed is given. The old default was silently broken:
        ///       PlanetTile.Valid is `tileId >= 0` and PlanetTile.Invalid is tileId -1, so
        ///       `default(PlanetTile)` - tileId 0 - tested as VALID. Every caller that omitted the
        ///       seed flood-filled from TILE 0, failed the window cull on its first iteration,
        ///       enqueued nothing, and got back a texture still holding its WaterColor pre-fill.
        ///       It presented as "the world map is blank" - an empty ocean that was really just
        ///       the untouched buffer. Faction Menu's StagePane was never affected (it always
        ///       passes an explicit seed); Ideology's spread plot and Quest Menu's location
        ///       mini-map both were.
        ///     Consumers of World/ must now require 8. Callers passing a PlanetTile still compile
        ///     unchanged via the implicit nullable conversion, but the assembly signature moved,
        ///     so every consumer needs a REBUILD.</para>
        ///
        /// <para>9 = RadiusFont.LabelItalic exists (2026-08-22, Radius UI - Health Tab session).
        ///     ADDITIVE - nothing was touched and no value moved - and it bumps ANYWAY, because
        ///     additive-inside-a-generation is exactly the hole that has now cost user-facing
        ///     crashes THREE times (RadiusFont.Epoch inside 3, the World layer inside 6, this).
        ///     A gen-8 framework built on 08-21 does not carry it, so a consumer asserting 8
        ///     passes the guard and then throws
        ///         MissingMethodException: Method not found: void RadiusFont.LabelItalic(...)
        ///     from inside a draw loop. The rule has no exception for "small" or "purely
        ///     additive": a consumer must require the FIRST generation THAT CANNOT PREDATE the
        ///     members it uses, and for LabelItalic that is 9.
        ///       Text/RadiusFont.cs : LabelItalic(Rect, string, GameFont, Color?, TextAnchor,
        ///         bool) - the explicit-weight twin of LabelBold, in a real italic face. Only a
        ///         DYNAMIC font resolves one; on a baked bitmap font StyleFor falls back to
        ///         regular rather than a synthetic slant.
        ///     Every consumer asserting 2-8 keeps passing unchanged (Require tests
        ///     Current >= minimum), so this bump is free for the rest of the suite.</para>
        ///
        /// <para>10 = the gene vocabulary (2026-08-23, Radius UI - Xenotypes session).
        ///     PURELY ADDITIVE - nothing existing touched, no value moved, no member renamed -
        ///     and it bumps anyway, for the reason generation 9 spells out.
        ///       Tokens/Palette.Genes.cs : the gene-domain identity set. A four-step build-COST
        ///         ramp (CostTrivial/CostLight/CostHeavy/CostArchite, plus the index-stable
        ///         CostRamp array), the provenance pair Endogene/Xenogene, Archite, Inactive,
        ///         and an 8-hue fallback ring with For(string categoryDefName) - a string, not a
        ///         GeneCategoryDef, because §2 requires Tokens to depend on nothing.
        ///         Three cost steps are numerically equal to Good, Over and Archo. They are NOT
        ///         aliases of them: those names mean other things, and F8 rule 3 is exactly
        ///         about what happens when a name is made to mean two things at once.
        ///       Icons/IconSet.Glyphs.cs : a 19th category, Gene/ (Helix, Genepack, Xenogerm,
        ///         ArchiteCapsule), plus Action/Search - the magnifier the library never had,
        ///         which every search box in the suite had been hand-rolling. Tier 1, 128px,
        ///         flat white, generated through gen_iconset.sh like every other accessor.
        ///     Library goes 272 -> 277 files, 19 -> 20 categories. Every consumer asserting
        ///     2-9 keeps passing unchanged.</para>
        ///
        /// <para>11 = the Map layer lands (2026-08-26, Radius UI - Quest Menu session).
        ///     PURELY ADDITIVE - nothing existing touched, no value moved - and it bumps anyway,
        ///     for the reason generation 9 spells out.
        ///       Map/MapCapture.cs : MapCapture (Request / Release / IsLive) plus the
        ///         MapCaptureDriver MapComponent that pumps it. A live camera feed of a piece of
        ///         the playing map, rendered into a RenderTexture any panel can draw. Promoted
        ///         out of Radius UI - Colonist Bar's LiveViewManager, where the same machinery
        ///         was welded to per-pawn portraits; this is the general form, and it also fixes
        ///         the limitation that version lives with - it force-submits the terrain sections
        ///         and things the engine culled away, so a feed works wherever the player's
        ///         camera happens to be pointing rather than only inside the current view.
        ///     NOTE: this generation adds a MapComponent, so it appears in save files as
        ///     `&lt;li Class="RadiusUI.Framework.MapCaptureDriver" /&gt;`. It holds no state and
        ///     saves nothing, but removing the framework from an existing save will log the
        ///     usual "could not find class" line for it. Consumers of Map/ must require 11.</para>
        ///
        /// <para>12 = rounded IMAGES, and a smoothed/rate-limited MapCapture (2026-08-26, same
        ///     session, straight off the first in-game look at the live view).
        ///     ADDITIVE in the members it adds, but MapCapture.Request GAINED A PARAMETER, so
        ///     the assembly signature moved and every consumer needs a rebuild - which is a
        ///     bump under the original rule, not merely the gen-9 "assert it exists" rule.
        ///       Chrome/CardChrome.cs : CardChrome.Image(Rect, Texture, radius, tint) - a real
        ///         texture clipped to the suite's corner radius. Every texture the suite draws
        ///         (baked world maps, the faction atlas, the live map feed) was poking square
        ///         corners out through a rounded outline, because plain GUI.DrawTexture has no
        ///         radius. Note it must use StretchToFill: Unity's rounded overload silently
        ///         drops the radii on the ScaleAndCrop/ScaleToFit paths.
        ///       Map/MapCapture.cs : Request gains an optional refreshHz; new Cut(key). The
        ///         capture now runs at ~24 Hz (scaled by RadiusTheme.RefreshMult) instead of
        ///         every frame, and the camera EASES toward its aim with a dead zone and a
        ///         snap threshold, so a subject group whose bounding box pops as pawns shuffle
        ///         no longer jerks the shot.
        ///     Consumers of Map/ must now require 12.</para>
        /// </summary>
        public const int Current = 12;

        /// <summary>
        /// Assert at startup that this framework is new enough for <paramref name="consumer"/>.
        /// Call from a <c>[StaticConstructorOnStartup]</c>, passing the generation the mod was
        /// written against. Returns false (and logs a plain-language error naming both sides)
        /// when the framework is older than required.
        ///
        /// <para>This cannot catch every case - a consumer compiled against a LATER framework
        /// that has since been rebuilt older will still throw <c>MissingMethodException</c>
        /// before any of our code runs. It turns the common, recoverable case into a sentence
        /// a human can act on.</para>
        /// </summary>
        public static bool Require(string consumer, int minimum)
        {
            if (Current >= minimum) return true;

            Log.Error(
                "[Radius UI] " + consumer + " needs Radius UI Framework generation " + minimum
                + ", but the loaded framework is generation " + Current + ". "
                + consumer + " will not work correctly until the framework is updated. "
                + "If you build these from source, rebuild the framework and then every "
                + "consumer mod - a consumer compiled against a different generation fails "
                + "at runtime with MissingMethodException / MissingFieldException while still "
                + "compiling cleanly.");
            return false;
        }
    }
}
