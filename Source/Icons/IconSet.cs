// Radius UI Framework - Icons/IconSet.cs (core)
//
// The hand-written core of the partial IconSet class. The three sibling files
// (IconSet.Glyphs.cs / .Anatomy.cs / .Marker.cs) are GENERATED accessor tables that
// call Reg() below - regenerate them with _dev/RadiusUIFramework/texture-intake/
// gen_iconset.sh, never hand-edit them. This file owns:
//   - Reg(): the single loader every generated accessor funnels through.
//   - Get(): string-path lookup incl. the 12 retired alias names (aliases.json).
//   - The alias-backed ordered sets (SquadEmblems x8, ReminderSlots x8) that must
//     present their full width even though fewer files ship.
//
// Shadowing rule (ARCHITECTURE §10): consumers must NEVER ship art at Textures/RadiusUI/
// paths - ContentFinder returns the LAST-loaded match, so a local copy silently replaces
// the library's for everyone, with no error. Unique consumer art lives in that mod's own
// namespace (e.g. Textures/RadiusUIQuestMenu/).
//
// Timing: textures resolve on FIRST TOUCH of an accessor (nested-class static init) or
// Get() call. That must happen at draw time, after game content is loaded - never touch
// IconSet from a Mod constructor (background thread, content not loaded yet).

using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace RadiusUI.Framework
{
    public static partial class IconSet
    {
        private const string Root = "RadiusUI/";

        /// <summary>String-path lookup cache (accessor fields bypass this; Get() fills it).</summary>
        private static readonly Dictionary<string, RadiusIcon> byPath = new Dictionary<string, RadiusIcon>(64);

        private static readonly HashSet<string> warnedMissing = new HashSet<string>();

        /// <summary>
        /// The 12 retired names -> survivor paths (generated aliases.json, duplicate review
        /// 2026-06-09). Retiring a name never deletes it: legacy call sites keep resolving.
        /// </summary>
        private static readonly Dictionary<string, string> aliases = new Dictionary<string, string>
        {
            { "Action/Health", "Alert/Info" },
            { "Action/Pause", "Speed/Pause" },
            { "Action/ViewOrgans", "Common/Heart" },
            { "Common/Bolt", "Medical/Pain" },
            { "Common/Spark", "Status/Inspired" },
            { "Common/Star4", "Status/Inspired" },
            { "Reminder/Star", "Status/Inspired" },
            { "Slot/Shield", "Stat/Shield" },
            { "Squad/Icon3", "Action/Xray" },
            { "Squad/Icon4", "Common/Heart" },
            { "Squad/Icon7", "Status/Burning" },
            { "Weather/Snow", "Season/Winter" },
        };

        /// <summary>
        /// Load one library texture and wrap it with its art tier. The single funnel used
        /// by every generated accessor. Missing art warns once and yields a non-Exists
        /// icon (RadiusIcon.Draw no-ops on those) instead of throwing mid-frame.
        /// </summary>
        internal static RadiusIcon Reg(string path, IconTier tier)
        {
            // Colour art draws alpha-only. Applied here rather than in the generated accessor
            // tables so the treatment survives a gen_iconset.sh regeneration, and so Get()'s
            // prefix-inferred tier is corrected on the same path. Only Glyph is upgraded:
            // Illustration / Marker / Foreign already refuse or already own their tint.
            if (tier == IconTier.Glyph && EmojiPaths.Contains(path))
            {
                tier = IconTier.Emoji;
            }

            Texture2D? tex = ContentFinder<Texture2D>.Get(Root + path, reportFailure: false);
            if (tex == null && warnedMissing.Add(path))
            {
                Log.Warning("[Radius UI] Icon missing from library: " + Root + path);
            }
            return new RadiusIcon(tex, tier);
        }

        /// <summary>
        /// Resolve a library path ("Category/Name", no "RadiusUI/" prefix) at runtime,
        /// following the alias table. For dynamic lookups (def-driven icon names, saved
        /// strings); prefer the typed accessors (IconSet.Action.Pin, ...) in static code.
        /// Tier is inferred from the category prefix, matching the generated tables.
        /// Cost: one dictionary hit after first resolve.
        /// </summary>
        public static RadiusIcon Get(string path)
        {
            if (byPath.TryGetValue(path, out RadiusIcon icon))
            {
                return icon;
            }
            string resolved = aliases.TryGetValue(path, out string survivor) ? survivor : path;
            IconTier tier = resolved.StartsWith("Anatomy/", System.StringComparison.Ordinal) ? IconTier.Illustration
                : resolved.StartsWith("Marker/", System.StringComparison.Ordinal) ? IconTier.Marker
                : IconTier.Glyph;
            icon = Reg(resolved, tier);
            byPath[path] = icon;
            return icon;
        }

        // ------------------------------------------------------------------ ordered sets
        // Built lazily so touching IconSet's outer statics never forces 8 texture loads
        // before anything is drawn.

        private static RadiusIcon[]? squadEmblems;
        private static RadiusIcon[]? reminderSlots;

        /// <summary>
        /// The 8 squad emblem slots (ARCHITECTURE §11: 5 shipped files + 3 alias-backed
        /// slots, but the picker always offers 8). Index-stable: saved squad configs store
        /// the index.
        /// </summary>
        public static RadiusIcon[] SquadEmblems
        {
            get
            {
                if (squadEmblems == null)
                {
                    squadEmblems = new RadiusIcon[8];
                    for (int i = 0; i < 8; i++)
                    {
                        squadEmblems[i] = Get("Squad/Icon" + i);
                    }
                }
                return squadEmblems;
            }
        }

        /// <summary>
        /// The 8 reminder-picker slots in picker order (7 shipped files; Star is
        /// alias-backed onto Status/Inspired). Index-stable for saved reminders.
        /// </summary>
        public static RadiusIcon[] ReminderSlots
        {
            get
            {
                if (reminderSlots == null)
                {
                    reminderSlots = new[]
                    {
                        Get("Reminder/Alert"), Get("Reminder/Bell"), Get("Reminder/Check"),
                        Get("Reminder/Clock"), Get("Reminder/Flag"), Get("Reminder/Gift"),
                        Get("Reminder/Heart"), Get("Reminder/Star"),
                    };
                }
                return reminderSlots;
            }
        }

        /// <summary>Speed icons indexed by Verse.TimeSpeed (Paused..Ultrafast).</summary>
        public static RadiusIcon SpeedIcon(int timeSpeed)
        {
            switch (timeSpeed)
            {
                case 0: return Speed.Pause;
                case 1: return Speed.Normal;
                case 2: return Speed.Fast;
                case 3: return Speed.Superfast;
                default: return Speed.Ultrafast;
            }
        }
    }
}
