// Radius UI Framework - Icons/IconSet.Emoji.cs
//
// The colour half of the library, by path. GENERATED from the restyle manifest -
// do not hand-edit; regenerate if an icon changes treatment.
//
// Why this exists as a lookup rather than a path prefix (which is how Anatomy/ and
// Marker/ are detected): treatment does NOT follow category. Common/ is mostly colour
// but Common/Heart, Common/Leaf and Common/Clock are glyphs; Stat/ is mostly colour but
// its trend arrows and chevron are glyphs. Two rules force a glyph regardless of how the
// icon looks:
//
//   1. ALIAS CONTAGION. IconSet.aliases maps retired names onto survivors. When the
//      retired name lives in a tinted category the survivor inherits that constraint -
//      Squad/ emblems are read as raw .Texture (opting out of the tier rule entirely) and
//      are documented by their consumer as "white, tinted per-squad", so colour art there
//      cannot be protected. That is why Common/Heart (Squad/Icon4 + Action/ViewOrgans),
//      Stat/Shield (Slot/Shield) and Status/Burning (Squad/Icon7) are glyphs.
//
//   2. CONTRAST TINTS. A tint of InkOnAccent / BandInk / Surface0 means the icon sits ON a
//      coloured surface and must take that surface's ink. Colour art is illegible there.
//      That is why Common/Clock, Occasion/Pregnancy, Event/Quest, Medical/Bleed and
//      Action/Check are glyphs.
//
// Anything NOT listed here keeps the tier its accessor declares (Glyph, or Illustration /
// Marker for Anatomy/ and Marker/).

using System.Collections.Generic;

namespace RadiusUI.Framework
{
    public static partial class IconSet
    {
        /// <summary>
        /// Paths whose art is saturated colour and must therefore draw alpha-only.
        /// Consulted by <c>Reg</c>, the single funnel every accessor and <c>Get</c> passes
        /// through, so a path listed here is tiered correctly however it is reached.
        /// </summary>
        internal static readonly HashSet<string> EmojiPaths = new HashSet<string>
        {
            "Alert/Cold",
            "Alert/Crit",
            "Alert/Notice",
            "Alert/Threat",
            "Alert/Warn",
            "Alert/Warning",
            "Common/Bell",
            "Common/Book",
            "Common/BookClosed",
            "Common/BookOpen",
            "Common/Cal",
            "Common/Chest",
            "Common/Fang",
            "Common/Letter",
            "Common/Mail",
            "Common/Note",
            "Common/Pod",
            "Common/Scroll",
            "Common/Skull",
            "Common/Sword",
            "Condition/Ash",
            "Condition/Aurora",
            "Condition/Drought",
            "Condition/EMI",
            "Condition/Eclipse",
            "Condition/Eclipse2",
            "Condition/Fauna",
            "Condition/Fire",
            "Condition/Gravity",
            "Condition/Mech",
            "Condition/Moon",
            "Condition/Plague",
            "Condition/Planetkiller",
            "Condition/Quake",
            "Condition/Radiation",
            "Condition/Rainbow",
            "Condition/Skull",
            "Condition/SolarFlare",
            "Condition/Telescope",
            "Condition/Toxic",
            "Condition/Volcano",
            "Event/Bug",
            "Event/Disease",
            "Event/Joiner",
            "Event/Pod",
            "Event/Raid",
            "Event/Trade",
            "Event/Visitor",
            "Gene/ArchiteCapsule",
            "Gene/Genepack",
            "Gene/Helix",
            "Gene/Xenogerm",
            "Medical/Bandage",
            "Medical/Bionic",
            "Medical/Illness",
            "Medical/Injury",
            "Medical/Missing",
            "Medical/Pain",
            "Medical/Surgery",
            "Need/Beauty",
            "Need/Comfort",
            "Need/Indoors",
            "Need/Recreation",
            "Occasion/Anniversary",
            "Occasion/Appointment",
            "Occasion/Birthday",
            "Occasion/Caravan",
            "Occasion/Challenge",
            "Occasion/Conclave",
            "Occasion/Coronation",
            "Occasion/Death",
            "Occasion/Election",
            "Occasion/Growth",
            "Occasion/Marriage",
            "Occasion/Milestone",
            "Occasion/Reminder",
            "Occasion/Research",
            "Occasion/Ritual",
            "Occasion/Royal",
            "Occasion/Shuttle",
            "Season/Fall",
            "Season/PermanentSummer",
            "Season/PermanentWinter",
            "Season/Spring",
            "Season/Summer",
            "Season/Winter",
            "Stat/Blunt",
            "Stat/Box",
            "Stat/Bullet",
            "Stat/Cross",
            "Stat/Droplet",
            "Stat/Fist",
            "Stat/Flame",
            "Stat/Food",
            "Stat/Fps",
            "Stat/Glow",
            "Stat/Heart",
            "Stat/Heat",
            "Stat/Melee",
            "Stat/Mood",
            "Stat/Quality",
            "Stat/Sharp",
            "Stat/Shooting",
            "Stat/Skull",
            "Stat/Temp",
            "Stat/Temperature",
            "Stat/Tetris",
            "Stat/Thermometer",
            "Stat/Wave",
            "Stat/Wealth",
            "Stat/Weight",
            "Status/Aim",
            "Status/Break",
            "Status/Caravan",
            "Status/Danger",
            "Status/Flee",
            "Status/Focus",
            "Status/Idle",
            "Status/Medical",
            "Status/Mount",
            "Status/Research",
            "Status/Zzz",
            "Weather/Anomaly",
            "Weather/Clear",
            "Weather/Fog",
            "Weather/HeavyRain",
            "Weather/Moon",
            "Weather/Overcast",
            "Weather/Rain",
            "Weather/Rain2",
            "Weather/Sandstorm",
            "Weather/Sun",
            "Weather/Thunder",
            "Weather/Wind",
            "Weather/Wind2",
        };
    }
}
