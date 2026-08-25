// Radius UI Framework - Chrome/FlatField.cs
//
// The suite's flat text field: search boxes, name entry, filter inputs. Promoted here
// 2026-08-19 from Radius UI - Faction Menu (Chrome/FlatField.cs), where it shipped as the
// rail search box.
//
// WHY THIS IS FRAMEWORK-OWNED AND NOT PER-MOD:
// Widgets.TextField draws with Text.CurTextFieldStyle, whose eight state backgrounds are
// vanilla's tan bevel. A consumer that draws its own flat plate behind the field just puts a
// flat surface UNDER vanilla chrome - the beige border still wins, at every UI scale. The only
// fix is to draw with a COPY of that style with all eight backgrounds nulled. That is
// shared-GUIStyle surgery of exactly the kind RadiusFont already centralises, and every
// consumer with a searchable list needs it (Faction Menu, Health Tab, Colonist Bar, Quest
// Menu). Four copies of this would drift.
//
// Note for anyone tempted by the obvious shortcut: mutating GUI.skin.textField does NOTHING
// here (Widgets.TextField never reads it), and mutating Text.CurTextFieldStyle "works" but
// restyles vanilla and every other mod for the rest of the session. Copy, never mutate.

using UnityEngine;
using Verse;

namespace RadiusUI.Framework
{
    /// <summary>
    /// Flat text field on a suite plate. THE text input of the suite - do not hand-roll one,
    /// and never call <c>Widgets.TextField</c> directly on a suite surface (it draws vanilla's
    /// tan bevel over whatever plate you put behind it).
    ///
    /// <para>Contract: OnGUI main thread. One cached style, rebuilt only when vanilla's fonts
    /// change. Emits a STABLE number of IMGUI controls regardless of the field's contents (see
    /// the clear-button note in <see cref="Draw"/>).</para>
    /// </summary>
    public static class FlatField
    {
        // ------------------------------------------------------------------ metrics
        // Field-internal insets, deliberately NOT on the Metrics spacing scale: these are the
        // proven Faction Menu values that make the caret sit right against a 1px border, not
        // layout spacing a caller composes with.

        /// <summary>Text inset from the left edge. Clears the border and the corner arc.</summary>
        private const float PadLeft = 9f;

        /// <summary>Text inset from the right edge when there is no clear button.</summary>
        private const float PadRight = 8f;

        /// <summary>
        /// Width reserved at the right edge for the clear button. Reserved whenever
        /// <c>clearButton</c> is true - INCLUDING while the field is empty and the glyph is
        /// hidden - so the text rect never changes width as the user types (a field whose
        /// content reflows on the first keystroke reads as a glitch), and so the control
        /// count stays constant across event passes.
        /// </summary>
        private const float ClearReserve = 24f;

        /// <summary>Clear glyph box, centred in the reserved column.</summary>
        private const float ClearGlyph = 16f;

        // ------------------------------------------------------------------ style cache

        private static GUIStyle? style;
        private static GUIStyle? builtFromStyle;
        private static Font? builtFromFont;

        // ------------------------------------------------------------------ API

        /// <summary>
        /// Draw a flat text field and return its (possibly edited) text.
        ///
        /// <para><paramref name="controlName"/> must be UNIQUE ON SCREEN. It drives focus
        /// detection (the border switches to the accent while focused) and lets a caller focus
        /// the field programmatically with <c>GUI.FocusControl(name)</c>. Use a constant for a
        /// singleton field, and suffix with the row index when drawing fields in a loop -
        /// unnamed IMGUI fields are tracked by positional id, so focus jumps between them
        /// whenever the set of drawn fields changes.</para>
        ///
        /// <para><paramref name="placeholder"/> is drawn faint only while the field is empty
        /// AND unfocused, so it disappears the moment the caret lands. It is player-facing:
        /// pass a translated string, never a literal.</para>
        ///
        /// <para>When <paramref name="clearButton"/> is true and the field is non-empty, a
        /// cross sits at the right edge; clicking it drops focus and returns "". Clicking the
        /// reserved column while the field is EMPTY focuses the field instead of doing nothing,
        /// so the whole plate is live rather than having a dead 24px gutter.</para>
        ///
        /// Cost: two plate draws, one cached style lookup, one label when empty. No allocation
        /// once the style is built. Main thread / OnGUI only.
        /// </summary>
        /// <returns>The edited text. Never null.</returns>
        public static string Draw(Rect r, string text, string controlName,
            string? placeholder = null, bool clearButton = true)
        {
            text ??= "";
            bool focused = GUI.GetNameOfFocusedControl() == controlName;

            // Plate + border. Both are Repaint-gated inside CardChrome and emit no control,
            // so they cost nothing on the Layout and input passes.
            CardChrome.Rounded(r, Palette.Surface0, Metrics.RadiusChip);
            CardChrome.Outline(r, focused ? RadiusTheme.Accent : Palette.Border, 1f, Metrics.RadiusChip);

            float rightPad = clearButton ? ClearReserve : PadRight;
            var fieldR = new Rect(
                r.x + PadLeft,
                r.y + 1f,
                Mathf.Max(0f, r.width - PadLeft - rightPad),
                Mathf.Max(0f, r.height - 2f));

            // SetNextControlName must be the statement immediately before the field, or focus
            // detection silently never matches. Do not hoist anything between these two lines.
            //
            // The field is also emitted BEFORE the clear button on purpose: it is the control
            // whose id must never move, and ids are allocated in draw order.
            GUI.color = Color.white;   // engine default; a caller's leaked tint would dim the text
            GUI.SetNextControlName(controlName);
            string result = GUI.TextField(fieldR, text, Style());

            if (text.Length == 0 && !focused && !placeholder.NullOrEmpty())
            {
                RadiusFont.Label(fieldR, placeholder!, GameFont.Small, heading: false,
                    color: Palette.TextFaint, anchor: TextAnchor.MiddleLeft, wrap: false);
            }

            if (clearButton)
            {
                bool showClear = text.Length > 0;
                var hitR = new Rect(r.xMax - ClearReserve, r.y, ClearReserve, r.height);
                bool hover = Mouse.IsOver(hitR);

                if (showClear)
                {
                    var glyphR = new Rect(
                        hitR.center.x - ClearGlyph * 0.5f,
                        hitR.center.y - ClearGlyph * 0.5f,
                        ClearGlyph, ClearGlyph);
                    IconSet.Action.Decline.Draw(glyphR.ContractedBy(2f),
                        hover ? Palette.Ink : Palette.TextFaint);
                    TooltipHandler.TipRegion(hitR, "RadiusUI.Field.ClearTip".Translate());
                }

                // Emitted UNCONDITIONALLY (given clearButton), never gated on text.Length.
                // The caller assigns our return value, so between the input pass and the
                // Repaint pass of the SAME frame an empty field can become non-empty - and a
                // control that appears mid-frame renumbers every control a caller draws after
                // us, sending their clicks to the wrong widget. clearButton itself is a call-
                // site constant, so branching on it is pass-stable.
                if (Widgets.ButtonInvisible(hitR, doMouseoverSound: showClear))
                {
                    if (showClear)
                    {
                        GUI.FocusControl(null);
                        return "";
                    }
                    GUI.FocusControl(controlName);
                }
            }

            return result;
        }

        // ------------------------------------------------------------------ internals

        /// <summary>
        /// The flat field style: a private copy of vanilla's with every state background
        /// nulled, so only our plate shows.
        ///
        /// <para>Copied from <c>Text.textFieldStyles[Small]</c> rather than
        /// <c>Text.CurTextFieldStyle</c>, because the latter resolves against whatever
        /// <c>Text.Font</c> the caller happened to leave set - so a caller mid-Medium would
        /// bake the Medium field style into our cache permanently.</para>
        ///
        /// <para>Epoch guard: rebuilt when vanilla's field style object OR its font is
        /// replaced (font mods and language switches do one or the other). We check the source
        /// we actually copied instead of RadiusFont.Epoch, which only advances when RadiusFont
        /// itself is exercised - and this field can draw a whole frame without touching it.</para>
        /// </summary>
        private static GUIStyle Style()
        {
            GUIStyle source = Text.textFieldStyles[(int)GameFont.Small];
            Font? face = source?.font;
            if (style != null && ReferenceEquals(source, builtFromStyle) && ReferenceEquals(face, builtFromFont))
            {
                return style;
            }

            builtFromStyle = source;
            builtFromFont = face;

            // COPY. Text.textFieldStyles is process-wide and shared with vanilla and every
            // other mod for the rest of the session - mutating it here would flatten every
            // text field in the game, including ones we do not own.
            var s = new GUIStyle(source);
            s.normal.background = null;
            s.focused.background = null;
            s.hover.background = null;
            s.active.background = null;
            s.onNormal.background = null;
            s.onFocused.background = null;
            s.onHover.background = null;
            s.onActive.background = null;
            s.border = new RectOffset(0, 0, 0, 0);
            s.margin = new RectOffset(0, 0, 0, 0);
            s.padding = new RectOffset(0, 0, 0, 0);
            s.alignment = TextAnchor.MiddleLeft;
            s.clipping = TextClipping.Clip;
            s.wordWrap = false;
            s.normal.textColor = Palette.Ink;
            s.focused.textColor = Palette.Ink;
            s.hover.textColor = Palette.Ink;
            s.active.textColor = Palette.Ink;
            s.onNormal.textColor = Palette.Ink;
            s.onFocused.textColor = Palette.Ink;
            s.onHover.textColor = Palette.Ink;
            s.onActive.textColor = Palette.Ink;

            // GUI.skin.settings (cursor colour, selection colour) is deliberately NOT touched:
            // it is global, per-skin rather than per-style, and nothing in the game unwinds it.

            style = s;
            return s;
        }
    }
}
