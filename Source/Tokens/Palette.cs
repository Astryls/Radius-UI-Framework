// Radius UI Framework - Tokens/Palette.cs
//
// Canonical colour tokens (ARCHITECTURE.md appendix A + surface decision #3,
// ratified 2026-06-10 for the Inspector pilot). Pure data: no drawing, no state,
// no Unity calls beyond Color. Do not fork these in a consumer mod - a missing
// token is a framework decision, not a local constant.

using UnityEngine;

namespace RadiusUI.Framework
{
    public static partial class Palette
    {
        // ------------------------------------------------------------------ surfaces
        // Numbered dark-to-light. Chrome reads Surface/Surface2 for cards and raised
        // elements; pages sit on Surface0 and bar tracks on Surface3.

        /// <summary>Page/panel base, the deepest surface. #0c0e11</summary>
        public static readonly Color Surface0 = new Color(0.047f, 0.055f, 0.067f);

        /// <summary>Card plate. #12151a</summary>
        public static readonly Color Surface = new Color(0.071f, 0.082f, 0.102f);

        /// <summary>Raised elements, chips, tiles. #191d24</summary>
        public static readonly Color Surface2 = new Color(0.098f, 0.114f, 0.141f);

        /// <summary>
        /// Alias of Surface, the card plate. Additive compatibility member: FrameworkVersion's
        /// changelog describes this ramp as "Surface0..3", so a consumer following the doc
        /// writes Surface1 and would otherwise hit a compile error on a name the doc promised.
        /// A property, not a field, so it cannot depend on static initialiser ordering.
        /// </summary>
        public static Color Surface1 => Surface;

        /// <summary>Bar tracks and insets. #20252e</summary>
        public static readonly Color Surface3 = new Color(0.125f, 0.145f, 0.180f);

        // ------------------------------------------------------------------ the wash ladder
        // THE canonical white-wash scale, ratified 2026-08-18 (user decision, recorded in this
        // mod's ARCHITECTURE Appendix A and Colonist Bar's ARCHITECTURE §6.5). Five named steps;
        // every wash in the suite snaps to its NEAREST step - measured across Colonist Bar alone,
        // ad-hoc alphas had grown to 22 distinct values, which is a continuum, not a design.
        //
        // Declared BEFORE the compatibility names below: those alias these fields, and static
        // field initialisers run in declaration order.

        /// <summary>Ladder step 1 (3%): hairlines, faint gridlines, disabled fills.</summary>
        public static readonly Color WashFaint = new Color(1f, 1f, 1f, 0.03f);

        /// <summary>Ladder step 2 (6%): default separator, major gridline, 1px card border.</summary>
        public static readonly Color Wash = new Color(1f, 1f, 1f, 0.06f);

        /// <summary>Ladder step 3 (10%): row/plate hover wash.</summary>
        public static readonly Color WashHover = new Color(1f, 1f, 1f, 0.10f);

        /// <summary>Ladder step 4 (14%): pressed/selected wash.</summary>
        public static readonly Color WashStrong = new Color(1f, 1f, 1f, 0.14f);

        /// <summary>Ladder step 5 (22%): chips, count badges, solid-reading washes.</summary>
        public static readonly Color WashSolid = new Color(1f, 1f, 1f, 0.22f);

        // Compatibility names (generation 1/2 vocabulary). Same VALUES as their ladder step,
        // except HoverWash: generation 2 shipped it at 5%, the ratified ladder puts hover at
        // 10%, and the ladder wins (FrameworkVersion generation 3 records the re-value - the
        // one visible change: hover washes brighten from barely-there to clearly-there).

        /// <summary>1px card/menu border. Alias of <see cref="Wash"/> (6%).</summary>
        public static readonly Color Border = Wash;

        /// <summary>Faint gridline/divider. Alias of <see cref="WashFaint"/> (3%).</summary>
        public static readonly Color BorderFaint = WashFaint;

        /// <summary>Row hover wash. Alias of <see cref="WashHover"/> - RE-VALUED at generation 3
        /// from 5% to the ladder's 10%.</summary>
        public static readonly Color HoverWash = WashHover;

        /// <summary>Pressed/selected wash. Alias of <see cref="WashStrong"/> (14%).</summary>
        public static readonly Color ActiveWash = WashStrong;

        /// <summary>
        /// Bookmark / pinned-by-the-player marker. Yellow, and deliberately NOT a member of
        /// the Good/Warn/Bad ramp: pinning is a user CHOICE, not a severity, and a pinned row
        /// tinted with Warn reads as "this quest is in trouble". Also not the accent, which is
        /// already spoken for by selection.
        /// <para>Added 2026-08-18 for Radius UI - Quest Menu pins; general-purpose for any
        /// suite mod that needs a "marked by the player" state.</para>
        /// </summary>
        public static readonly Color Pin = new Color(1.00f, 0.84f, 0.32f);

        // ------------------------------------------------------------------ text

        /// <summary>Primary text - the ink Radius UI writes with. #e8eaed</summary>
        public static readonly Color Ink = new Color(0.910f, 0.918f, 0.929f);

        /// <summary>Secondary/dim text. Pure grey per decision #1.</summary>
        public static readonly Color TextDim = new Color(0.62f, 0.62f, 0.62f);

        /// <summary>Body prose - a tier softer than <see cref="Ink"/> but stronger than
        /// <see cref="TextDim"/>. Added 2026-06-17 for Radius UI - Quest Menu (long
        /// description passages); general-purpose, use for any multi-sentence body copy.</summary>
        public static readonly Color TextMid = new Color(0.839f, 0.855f, 0.886f); // #d6dae2

        /// <summary>Faintest text: hints, keyboard help, footnotes. Added 2026-06-17 for
        /// Radius UI - Quest Menu; general-purpose.</summary>
        public static readonly Color TextFaint = new Color(0.361f, 0.388f, 0.439f); // #5c6370

        /// <summary>Text drawn ON an accent or semantic fill (bands, active pills).</summary>
        public static readonly Color InkOnAccent = Surface0;

        /// <summary>Healthy/neutral bar fill. White at 38%.</summary>
        public static readonly Color FillNeutral = new Color(1f, 1f, 1f, 0.38f);

        /// <summary>Faintest ink of all: day separators, era ticks, rulers. A tier below
        /// <see cref="TextFaint"/>. Added 2026-06-17 reconciling Radius UI - Health Tab
        /// (was its local <c>Dim3</c>); general-purpose. #454c55</summary>
        public static readonly Color TextGhost = new Color(0.271f, 0.298f, 0.333f);

        /// <summary>A reading that is normal and should NOT draw the eye - healthy stats,
        /// unremarkable values. Brighter than <see cref="TextDim"/>, softer than
        /// <see cref="TextMid"/>. Added 2026-06-17 for Health Tab; also wanted by
        /// Colonist Bar.</summary>
        public static readonly Color OkGray = new Color(0.70f, 0.73f, 0.77f);

        /// <summary>Left-edge strip on a row with nothing to say - inert, missing, muted.
        /// Added 2026-06-17 for Health Tab; also wanted by Colonist Bar.</summary>
        public static readonly Color StripGray = new Color(0.29f, 0.31f, 0.35f);

        // ------------------------------------------------------------------ special meanings
        // Each of these MEANS one specific thing. None is decoration and none may be
        // reused for an unrelated purpose - that is the whole reason they are tokens.

        /// <summary>A value ABOVE its natural maximum: enhanced, boosted, over 100%.
        /// Added 2026-06-17 for Health Tab; also wanted by Colonist Bar.</summary>
        public static readonly Color Over = new Color(0.50f, 0.80f, 1.00f);

        /// <summary>Archotech. Deliberately kept clear of <see cref="Warn"/> amber so
        /// "this is archotech" can never read as "this is a warning". Note archotech is a
        /// PROVENANCE, never a power level - a consumer must not infer it from a number.
        /// #ffdf73. Added 2026-06-17 for Health Tab; also wanted by Colonist Bar.</summary>
        public static readonly Color Archo = new Color(1.00f, 0.875f, 0.451f);

        /// <summary>A DEVELOPER control. Deliberately outside the semantic ramp: accent,
        /// good, warn, bad and archo all say something about the subject, and a dev tool
        /// must never be mistaken for a reading of it. #c08cff.
        /// Added 2026-06-17 for Health Tab's dev rail.</summary>
        public static readonly Color DevAccent = new Color(0.753f, 0.549f, 1.00f);

        // ------------------------------------------------------------------ semantic ramp
        // Standard ramp for dark panel surfaces (tab mods, the inspector).

        public static readonly Color Good = new Color(0.40f, 0.85f, 0.40f);
        public static readonly Color Warn = new Color(0.95f, 0.65f, 0.20f);
        public static readonly Color Bad = new Color(0.90f, 0.35f, 0.35f);

        // Bright ramp for surfaces drawn directly over the game map (HUD).

        public static readonly Color GoodBright = new Color(0.55f, 0.90f, 0.45f);
        public static readonly Color WarnBright = new Color(1.00f, 0.85f, 0.30f);
        public static readonly Color BadBright = new Color(1.00f, 0.45f, 0.35f);

        // ------------------------------------------------------------------ accents
        // The user picks ONE via RadiusTheme; consumers never hardcode an accent.

        public static readonly Color[] Accents =
        {
            new Color(0.45f, 0.75f, 1.00f), // Sky (suite default)
            new Color(1.00f, 0.82f, 0.35f), // Gold
            new Color(0.40f, 0.85f, 0.55f), // Emerald
            new Color(0.70f, 0.55f, 1.00f), // Violet
            new Color(1.00f, 0.45f, 0.45f), // Crimson
            new Color(0.35f, 0.85f, 0.85f), // Teal
            new Color(1.00f, 0.62f, 0.28f), // Amber
            new Color(1.00f, 0.55f, 0.78f), // Rose
        };

        /// <summary>
        /// Translation key suffixes, index-aligned with <see cref="Accents"/>:
        /// "RadiusUI.Accent." + AccentNames[i] resolves in the framework's Keyed file.
        /// </summary>
        public static readonly string[] AccentNames =
        {
            "Sky", "Gold", "Emerald", "Violet", "Crimson", "Teal", "Amber", "Rose",
        };
    }
}
