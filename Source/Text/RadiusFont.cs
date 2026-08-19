using UnityEngine;
using Verse;

namespace RadiusUI.Framework
{
    /// <summary>
    /// Suite text: one label/measure path every consumer routes through, honouring the
    /// user's bold/italic theme flags with REAL font faces (RimWorld's fonts are dynamic,
    /// so Unity resolves FontStyle.Bold/Italic through the OS renderer - never synthetic
    /// double-draw bold).
    ///
    /// Hard suite rules encoded here:
    ///  - GameFont.Tiny is BANNED: it silently renders at Small size under the
    ///    disable-tiny-text accessibility pref and clips hardcoded rects. Any Tiny request
    ///    is coerced to Small.
    ///  - Vanilla's shared GUIStyles (Text.fontStyles, GUI.skin) are NEVER mutated - we
    ///    draw with private copies. Mutating them restyles every other mod.
    ///  - Helpers reset Text.Anchor/WordWrap/GUI.color to engine defaults on exit, so a
    ///    caller's early return can't leak state into the end-of-frame validator.
    ///
    /// Contract: OnGUI main thread. Style copies are cached per (font, weight) with an
    /// epoch guard (font-changing mods / language switches rebuild vanilla's styles; we
    /// detect that by reference and drop our copies). Measurement uses the SAME style that
    /// draws - bold is wider than regular, so measuring regular and drawing bold drifts
    /// right-anchored layouts.
    /// </summary>
    public static class RadiusFont
    {
        // Cached style copies: [font(0=Small,1=Medium)] x [style flags 0..3 (bit0 bold, bit1 italic)].
        private static readonly GUIStyle?[,] styles = new GUIStyle?[2, 4];

        // Epoch guard: vanilla font object we built the copies from. If Verse rebuilds its
        // styles (language switch, font mod), the reference changes and we rebuild.
        private static Font? builtFromSmall;
        private static Font? builtFromMedium;

        // Reused measurement carrier - OnGUI is single-threaded, so one instance is safe.
        private static readonly GUIContent measureContent = new GUIContent();

        /// <summary>
        /// Draw a label in suite style. <paramref name="heading"/> marks title text: it
        /// renders bold when the user enabled bold headings. Body text renders italic when
        /// the user enabled italic. Resets Anchor/WordWrap/GUI.color to defaults on exit.
        /// </summary>
        public static void Label(Rect rect, string text, GameFont font = GameFont.Small,
            bool heading = false, Color? color = null,
            TextAnchor anchor = TextAnchor.UpperLeft, bool wrap = true)
        {
            font = Resolve(font);
            bool bold = heading && RadiusTheme.Bold;
            bool italic = RadiusTheme.Italic;

            if (!bold && !italic)
            {
                // Fast vanilla path: Verse styles, no copies involved.
                Text.Font = font;
                Text.Anchor = anchor;
                Text.WordWrap = wrap;
                GUI.color = color ?? Palette.Ink;
                Widgets.Label(rect, text);
            }
            else
            {
                GUIStyle style = StyleFor(font, bold, italic);
                style.alignment = anchor;
                style.wordWrap = wrap;
                GUI.color = color ?? Palette.Ink;
                GUI.Label(rect, text, style);
            }

            // Engine defaults, unconditionally (helpers reset to DEFAULTS, not captured
            // values, so a caller's leaked state can't ride through us to end of frame).
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
            Text.WordWrap = true;
            Text.Font = GameFont.Small;
        }

        /// <summary>
        /// Measure text with the SAME face Label would draw it in. Use this for any
        /// fit/centre/right-anchor math on suite text.
        /// </summary>
        public static Vector2 Size(string text, GameFont font = GameFont.Small, bool heading = false)
        {
            font = Resolve(font);
            bool bold = heading && RadiusTheme.Bold;
            bool italic = RadiusTheme.Italic;
            // Epoch check BEFORE the memo read: the no-style fast path below never touches
            // StyleFor, so without this a font/language switch would leave stale metrics
            // cached forever with no error - just layouts that quietly drift.
            EnsureEpoch();
            if (TextMemo.TryGetSize(text, font, bold, italic, out Vector2 hit))
            {
                return hit;
            }
            Vector2 measured = SizeRaw(text, font, bold, italic);
            TextMemo.PutSize(text, font, bold, italic, measured);
            return measured;
        }

        /// <summary>Uncached single-line measure. Callers go through <see cref="Size"/>.</summary>
        private static Vector2 SizeRaw(string text, GameFont font, bool bold, bool italic)
        {
            if (!bold && !italic)
            {
                Text.Font = font;
                Vector2 v = Text.CalcSize(text);
                Text.Font = GameFont.Small;
                return v;
            }
            measureContent.text = text;
            GUIStyle style = StyleFor(font, bold, italic);
            style.alignment = TextAnchor.UpperLeft;
            style.wordWrap = false;
            return style.CalcSize(measureContent);
        }

        /// <summary>Wrapped height of text at a width, in the face Label would use.</summary>
        public static float Height(string text, float width, GameFont font = GameFont.Small, bool heading = false)
        {
            font = Resolve(font);
            bool bold = heading && RadiusTheme.Bold;
            bool italic = RadiusTheme.Italic;
            EnsureEpoch();
            if (TextMemo.TryGetHeight(text, font, bold, italic, width, out float cached))
            {
                return cached;
            }
            float measured = HeightRaw(text, width, font, bold, italic);
            TextMemo.PutHeight(text, font, bold, italic, width, measured);
            return measured;
        }

        /// <summary>Uncached wrapped measure. Callers go through <see cref="Height"/>.</summary>
        private static float HeightRaw(string text, float width, GameFont font, bool bold, bool italic)
        {
            if (!bold && !italic)
            {
                Text.Font = font;
                float h = Text.CalcHeight(text, width);
                Text.Font = GameFont.Small;
                return h;
            }
            measureContent.text = text;
            GUIStyle style = StyleFor(font, bold, italic);
            style.alignment = TextAnchor.UpperLeft;
            style.wordWrap = true;
            return style.CalcHeight(measureContent, width);
        }

        /// <summary>
        /// Natural line height for a font (Tiny coerced to Small). Size single-line rects
        /// from this, never from hardcoded pixel guesses - fonts grow under accessibility
        /// settings and UI scales.
        /// </summary>
        public static float LineH(GameFont font)
        {
            return Text.LineHeightOf(Resolve(font));
        }

        /// <summary>The suite's Tiny ban: any Tiny request renders as Small.</summary>
        public static GameFont Resolve(GameFont font)
        {
            return font == GameFont.Tiny ? GameFont.Small : font;
        }

        // ------------------------------------------------------------------ weight-only API
        // Compat surface for the 2026-06-09 iteration (changelog row: "RadiusFont gains Bio
        // Tab's weight-only API"), kept because Health Tab call sites consume it. These take
        // an EXPLICIT weight - they are not gated by the user's bold-headings setting; the
        // heading-gated path is Label(heading: true).

        /// <summary>The user's bold-headings theme flag (see RadiusTheme.Bold).</summary>
        public static bool Bold => RadiusTheme.Bold;

        /// <summary>
        /// The suite face at an explicit weight. Returns a CACHED style copy: set
        /// alignment/wordWrap per call, never mutate anything else, never cache it across
        /// frames (the epoch guard may replace it when vanilla's fonts rebuild).
        /// </summary>
        public static GUIStyle Style(GameFont font, bool bold)
        {
            return StyleFor(Resolve(font), bold, RadiusTheme.Italic);
        }

        /// <summary>Single-line width of text at an explicit weight (bold is wider than
        /// regular - never measure regular and draw bold).</summary>
        public static float Width(string text, GameFont font, bool bold)
        {
            // Shares the memo with Size(): same key shape, same raw measure, so a string
            // measured through either entry point is free through the other.
            GameFont f = Resolve(font);
            bool italic = RadiusTheme.Italic;
            EnsureEpoch();
            if (TextMemo.TryGetSize(text, f, bold, italic, out Vector2 hit))
            {
                return hit.x;
            }
            Vector2 measured = SizeRaw(text, f, bold, italic);
            TextMemo.PutSize(text, f, bold, italic, measured);
            return measured.x;
        }

        /// <summary>Label at an explicit weight (weight-only API twin of the heading-gated
        /// Label). Resets Anchor/WordWrap/GUI.color to engine defaults on exit.</summary>
        public static void LabelBold(Rect rect, string text, GameFont font = GameFont.Small,
            Color? color = null, TextAnchor anchor = TextAnchor.UpperLeft, bool wrap = true)
        {
            GUIStyle style = Style(font, bold: true);
            style.alignment = anchor;
            style.wordWrap = wrap;
            GUI.color = color ?? Palette.Ink;
            GUI.Label(rect, text, style);
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
            Text.WordWrap = true;
            Text.Font = GameFont.Small;
        }

        // ------------------------------------------------------------------ style cache

        private static GUIStyle StyleFor(GameFont font, bool bold, bool italic)
        {
            EnsureEpoch();
            int f = font == GameFont.Medium ? 1 : 0;
            int s = (bold ? 1 : 0) | (italic ? 2 : 0);
            GUIStyle? style = styles[f, s];
            if (style == null)
            {
                GUIStyle source = Text.fontStyles[(int)(f == 1 ? GameFont.Medium : GameFont.Small)];
                style = new GUIStyle(source); // copy - NEVER mutate the shared original
                Font? face = source.font;
                bool dynamic = face != null && face.dynamic;
                // Only dynamic fonts can resolve real bold/italic faces; a baked bitmap
                // font would render a smeared synthetic - fall back to regular there.
                style.fontStyle = dynamic
                    ? (bold
                        ? (italic ? FontStyle.BoldAndItalic : FontStyle.Bold)
                        : (italic ? FontStyle.Italic : FontStyle.Normal))
                    : FontStyle.Normal;
                styles[f, s] = style;
            }
            return style;
        }

        private static void EnsureEpoch()
        {
            Font? small = Text.fontStyles[(int)GameFont.Small].font;
            Font? medium = Text.fontStyles[(int)GameFont.Medium].font;
            if (!ReferenceEquals(small, builtFromSmall) || !ReferenceEquals(medium, builtFromMedium))
            {
                // Vanilla's styles were rebuilt (font mod, language switch): drop our copies
                // AND every cached metric - measurements taken with the old face are wrong,
                // and a stale metric is invisible until a layout drifts.
                TextMemo.InvalidateAll();
                for (int i = 0; i < 2; i++)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        styles[i, j] = null;
                    }
                }
                builtFromSmall = small;
                builtFromMedium = medium;
            }
        }
    }
}
