using System.Collections.Generic;
using UnityEngine;

namespace RadiusUI.Framework
{
    /// <summary>
    /// The live theme facade every consumer reads at draw time. Owns the resolved accent
    /// colour, the corner-radius multiplier and the font flags, plus optional per-mod
    /// accent overrides.
    ///
    /// Hot-path contract (framework ARCHITECTURE §5): <see cref="Accent"/> and
    /// <see cref="RadiusScale"/> are static PROPERTIES over cached fields - within this
    /// assembly they inline to a field read; across assemblies Mono may leave a static call
    /// (~ns, fine at panel scale). No dictionary is touched unless a consumer actually
    /// registered an override, and values recompute only when settings change.
    ///
    /// TRUE PER-FRAME DRAWERS (a colonist bar reading a token 200+ times a frame) should
    /// not re-read even that: copy the IMMUTABLE tokens once at static init
    /// (<c>static readonly Color BG = Palette.Surface;</c> - copy, never a forwarding
    /// property) and memo the LIVE values (<see cref="Accent"/>) once per frame. That is
    /// the blessed consumer pattern, not an optimization trick.
    ///
    /// Thread affinity: reads are safe anywhere (plain structs); Register/Clear and
    /// Notify_SettingsChanged should happen on the main thread (settings UI / mod ctor).
    /// </summary>
    public static class RadiusTheme
    {
        private static RadiusThemeSettings? settings;

        // Cached resolved values - the per-frame fast path. Defaults cover the window
        // between assembly load and RadiusFrameworkMod's constructor running.
        private static Color accent = Palette.Accents[0];
        private static float radiusScale = 1f;
        private static float refreshMult = 1f;
        private static bool bold;
        private static bool italic;

        // Per-mod accent overrides (consumer packageId -> colour). Allocated lazily;
        // overrideCount lets the no-override path skip the dictionary entirely.
        private static Dictionary<string, Color>? overrides;
        private static int overrideCount;

        /// <summary>The suite accent the user picked. Plain field read, ~0 cost.</summary>
        public static Color Accent => accent;

        /// <summary>Corner radius multiplier (0 = square .. 1.5). Plain field read.</summary>
        public static float RadiusScale => radiusScale;

        /// <summary>
        /// Suite-wide multiplier on background refresh cadences. 1 = designed rate; HIGHER IS
        /// SLOWER AND CHEAPER. Plain field read, ~0 cost - <see cref="Throttle.EveryScaled"/>
        /// reads this per gate check. Always finite and clamped.
        /// </summary>
        public static float RefreshMult => refreshMult;

        /// <summary>Draw suite headings bold (real bold face via RadiusFont).</summary>
        public static bool Bold => bold;

        /// <summary>Draw suite text italic (via RadiusFont).</summary>
        public static bool Italic => italic;

        /// <summary>Current index into <see cref="Palette.Accents"/> (settings UI helper).</summary>
        public static int AccentIndex => settings?.accentIndex ?? 0;

        /// <summary>
        /// The accent for a specific consumer mod: its registered override if it has one,
        /// otherwise the global accent. Cost: one int compare on the common no-override
        /// path; one dictionary lookup when any override exists.
        /// </summary>
        public static Color AccentFor(string modId)
        {
            if (overrideCount == 0 || overrides == null)
            {
                return accent;
            }
            return overrides.TryGetValue(modId, out Color c) ? c : accent;
        }

        /// <summary>
        /// Register a local accent for one consumer (used sparingly - a consumer overrides
        /// the suite accent only when it has a stated reason). Lives for the process; not
        /// saved. Re-registering replaces the previous value.
        /// </summary>
        public static void RegisterAccentOverride(string modId, Color colour)
        {
            overrides ??= new Dictionary<string, Color>();
            if (!overrides.ContainsKey(modId))
            {
                overrideCount++;
            }
            overrides[modId] = colour;
        }

        /// <summary>Remove a consumer's accent override; it follows the suite accent again.</summary>
        public static void ClearAccentOverride(string modId)
        {
            if (overrides != null && overrides.Remove(modId))
            {
                overrideCount--;
            }
        }

        /// <summary>
        /// Wire the persisted settings in. Called once from RadiusFrameworkMod's ctor.
        /// NOTE: the Mod ctor runs on a background thread during load - this only assigns
        /// managed fields and Color structs, which is safe there (no Unity object APIs).
        /// </summary>
        internal static void Initialize(RadiusThemeSettings s)
        {
            settings = s;
            Notify_SettingsChanged();
        }

        /// <summary>Recompute the cached fast-path values after any settings change.</summary>
        public static void Notify_SettingsChanged()
        {
            RadiusThemeSettings? s = settings;
            if (s == null)
            {
                return;
            }
            int i = Mathf.Clamp(s.accentIndex, 0, Palette.Accents.Length - 1);
            accent = Palette.Accents[i];
            radiusScale = Mathf.Clamp(
                float.IsNaN(s.radiusScale) ? 1f : s.radiusScale, 0f, RadiusThemeSettings.MaxRadiusScale);
            refreshMult = Mathf.Clamp(
                float.IsNaN(s.refreshMult) ? 1f : s.refreshMult,
                RadiusThemeSettings.MinRefreshMult, RadiusThemeSettings.MaxRefreshMult);
            bold = s.fontBold;
            italic = s.fontItalic;
        }
    }
}
