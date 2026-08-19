// Radius UI Framework - Icons/IconSet.Aliases.cs
//
// COMPATIBILITY SHIM. Hand-written; not generated (unlike IconSet.Glyphs/Anatomy/Marker).
//
// WHY THIS FILE EXISTS
// The duplicate review (ARCHITECTURE §11) retired 12 icon names onto surviving files and
// DELETED the retired PNGs. The alias table in IconSet.Get() keeps the retired *paths*
// resolving, but generation 2 removed the typed accessors that consumers actually called,
// which is a source break dressed up as a runtime one:
//
//     MissingMethodException: Method not found: ...IconSet.get_ActionViewOrgans()
//
// A consumer built against generation 1 still COMPILES and then dies at runtime, in a
// different mod from the one that changed. FrameworkVersion.cs was written because of this
// exact symptom - see its header, which cites this very accessor.
//
// The framework is shared state with four consumers, so the rule is additive-only: a name
// that existed must keep existing. These forwarders restore every retired accessor and route
// it through the alias table, so both spellings agree by construction and cannot drift.
//
// [Obsolete] carries a WARNING, never an error: the point is that old call sites keep
// building while telling their author where to go next. Do not add error: true.
//
// These are PROPERTIES, matching the shape generation 1 exposed (the exception above names
// get_ActionViewOrgans, a property getter). Each is one dictionary hit into IconSet.Get's
// byPath cache after first resolve - no eager texture load at type init.
//
// Retiring a name never deletes it. If a future review retires more names, add them here too.

using System;

namespace RadiusUI.Framework
{
    public static partial class IconSet
    {
        // ---- Action ---------------------------------------------------------------

        /// <summary>Retired name <c>Action/Health</c> (the "health" action glyph). Resolves
        /// through the alias table to its survivor, <c>Alert/Info</c>.</summary>
        [Obsolete("Retired by the duplicate review. Use IconSet.Get(\"Action/Health\") or IconSet.Alert.Info.")]
        public static RadiusIcon ActionHealth => Get("Action/Health");

        /// <summary>Retired name <c>Action/Pause</c> (HUD's pause glyph). Resolves through the
        /// alias table to its survivor, <c>Speed/Pause</c>.</summary>
        [Obsolete("Retired by the duplicate review. Use IconSet.Get(\"Action/Pause\") or IconSet.Speed.Pause.")]
        public static RadiusIcon ActionPause => Get("Action/Pause");

        /// <summary>Retired name <c>Action/ViewOrgans</c> (Health Tab's view-organs toggle).
        /// Resolves through the alias table to its survivor, <c>Common/Heart</c>.</summary>
        [Obsolete("Retired by the duplicate review. Use IconSet.Get(\"Action/ViewOrgans\") or IconSet.Common.Heart.")]
        public static RadiusIcon ActionViewOrgans => Get("Action/ViewOrgans");

        // ---- Common ---------------------------------------------------------------

        /// <summary>Retired name <c>Common/Bolt</c> (Quest Menu's bolt). Resolves through the
        /// alias table to its survivor, <c>Medical/Pain</c>.</summary>
        [Obsolete("Retired by the duplicate review. Use IconSet.Get(\"Common/Bolt\") or IconSet.Medical.Pain.")]
        public static RadiusIcon CommonBolt => Get("Common/Bolt");

        /// <summary>Retired name <c>Common/Spark</c> (HUD's spark). Resolves through the alias
        /// table to its survivor, <c>Status/Inspired</c>.</summary>
        [Obsolete("Retired by the duplicate review. Use IconSet.Get(\"Common/Spark\") or IconSet.Status.Inspired.")]
        public static RadiusIcon CommonSpark => Get("Common/Spark");

        /// <summary>
        /// Retired: Quest Menu's 4-point star. Survivor: Status/Inspired.
        /// The original was 20x20, the lowest-resolution asset in the library, so it could not
        /// serve as a survivor for 128px call sites (ARCHITECTURE §11, resolution floor).
        /// </summary>
        [Obsolete("Retired by the duplicate review. Use IconSet.Get(\"Common/Star4\") or IconSet.Status.Inspired.")]
        public static RadiusIcon CommonStar4 => Get("Common/Star4");

        // ---- Reminder -------------------------------------------------------------

        /// <summary>Retired name <c>Reminder/Star</c> (HUD's reminder star). Resolves through
        /// the alias table to its survivor, <c>Status/Inspired</c>.</summary>
        [Obsolete("Retired by the duplicate review. Use IconSet.Get(\"Reminder/Star\") or IconSet.Status.Inspired.")]
        public static RadiusIcon ReminderStar => Get("Reminder/Star");

        // ---- Slot -----------------------------------------------------------------

        /// <summary>Retired name <c>Slot/Shield</c> (True RPG Inventory's shield slot).
        /// Resolves through the alias table to its survivor, <c>Stat/Shield</c>.</summary>
        [Obsolete("Retired by the duplicate review. Use IconSet.Get(\"Slot/Shield\") or IconSet.Stat.Shield.")]
        public static RadiusIcon SlotShield => Get("Slot/Shield");

        // ---- Squad ----------------------------------------------------------------
        // Squad ships 5 files but must present 8 emblem slots; 3, 4 and 7 are alias-backed.
        // Prefer IconSet.SquadEmblems, which is index-stable for saved squad assignments.

        /// <summary>Retired name <c>Squad/Icon3</c> (squad emblem 3). Resolves through the
        /// alias table to its survivor, <c>Action/Xray</c>.</summary>
        [Obsolete("Retired by the duplicate review. Use IconSet.SquadEmblems[3] or IconSet.Get(\"Squad/Icon3\").")]
        public static RadiusIcon SquadIcon3 => Get("Squad/Icon3");

        /// <summary>Retired name <c>Squad/Icon4</c> (squad emblem 4). Resolves through the
        /// alias table to its survivor, <c>Common/Heart</c>.</summary>
        [Obsolete("Retired by the duplicate review. Use IconSet.SquadEmblems[4] or IconSet.Get(\"Squad/Icon4\").")]
        public static RadiusIcon SquadIcon4 => Get("Squad/Icon4");

        /// <summary>Retired name <c>Squad/Icon7</c> (squad emblem 7). Resolves through the
        /// alias table to its survivor, <c>Status/Burning</c>.</summary>
        [Obsolete("Retired by the duplicate review. Use IconSet.SquadEmblems[7] or IconSet.Get(\"Squad/Icon7\").")]
        public static RadiusIcon SquadIcon7 => Get("Squad/Icon7");

        // ---- Weather --------------------------------------------------------------

        /// <summary>Retired name <c>Weather/Snow</c> (HUD's snow glyph). Resolves through the
        /// alias table to its survivor, <c>Season/Winter</c>.</summary>
        [Obsolete("Retired by the duplicate review. Use IconSet.Get(\"Weather/Snow\") or IconSet.Season.Winter.")]
        public static RadiusIcon WeatherSnow => Get("Weather/Snow");
    }
}
