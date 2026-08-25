// Radius UI Framework - Settings/SettingsPage.cs
//
// A small settings-drawing kit, so four consumers stop hand-rolling Listing_Standard.
//
// This is NOT a base class. A consumer's Mod type must derive from Verse.Mod, so inheritance
// is already spent; and the settings MODEL differs per mod while only the DRAWING repeats.
// So this is a helper that takes the listing and draws one control properly.
//
// What "properly" means here is GLOBAL_RULES §8, made cheap enough that nobody skips it:
//   #6 recognition over recall - a slider ALWAYS shows its live value on its own label,
//      never only in a tooltip;
//   #5 error prevention - a control that cannot do anything is disabled WITH a reason on
//      hover, rather than left enabled to fail;
//   #10 help - every non-obvious control takes a tooltip;
//   #3 user control - every page can offer a reset.
//
// §7 applies to every string passed in: sentence case, no em or en dash, and the caller
// passes an already-Translate()d string - this kit never sees a raw key, so it cannot be
// the reason something ships untranslated.

using System;
using UnityEngine;
using Verse;

namespace RadiusUI.Framework
{
    /// <summary>
    /// Widgets for a mod settings page. Stateless; every method takes the listing it draws
    /// into. Thread affinity: OnGUI main thread.
    /// </summary>
    public static class SettingsPage
    {
        /// <summary>
        /// Checkbox with a tooltip. When <paramref name="enabled"/> is false the row is
        /// dimmed, the value cannot change, and <paramref name="disabledReason"/> is shown on
        /// hover instead of the normal tooltip (Nielsen #5: say WHY, do not just grey it out).
        /// </summary>
        public static void Checkbox(Listing_Standard listing, string label, ref bool value,
            string? tooltip = null, bool enabled = true, string? disabledReason = null)
        {
            Rect row = listing.GetRect(Metrics.RowText);
            bool hover = Mouse.IsOver(row);
            if (hover) Widgets.DrawBoxSolid(row, Palette.HoverWash);

            string? tip = enabled ? tooltip : (disabledReason ?? tooltip);
            if (!tip.NullOrEmpty()) TooltipHandler.TipRegion(row, tip);

            if (!enabled)
            {
                Color prev = GUI.color;
                GUI.color = Palette.TextDim;
                Widgets.CheckboxLabeled(row, label, ref DummyFalse);
                GUI.color = prev;
                DummyFalse = false;
                return;
            }

            Widgets.CheckboxLabeled(row, label, ref value);
        }

        // Sink for the disabled path: Widgets.CheckboxLabeled needs a ref, and it must not be
        // the caller's field or a click on a disabled row would still toggle it.
        private static bool DummyFalse;

        /// <summary>
        /// Slider whose CURRENT VALUE is part of its label, formatted by the caller.
        /// <paramref name="labelFormat"/> receives the formatted value, e.g.
        /// <c>"Tab size: {0}"</c>. Snaps to <paramref name="step"/> so the value is always a
        /// round number a player can describe.
        /// </summary>
        public static float Slider(Listing_Standard listing, string labelFormat, float value,
            float min, float max, float step = 0.05f, string? tooltip = null,
            Func<float, string>? format = null)
        {
            string shown = format != null ? format(value) : value.ToStringPercent();
            Rect labelRect = listing.GetRect(Metrics.RowText);
            Widgets.Label(labelRect, labelFormat.Replace("{0}", shown));
            if (!tooltip.NullOrEmpty()) TooltipHandler.TipRegion(labelRect, tooltip);

            float raw = listing.Slider(value, min, max);
            if (step <= 0f) return raw;
            return Mathf.Round(raw / step) * step;
        }

        /// <summary>
        /// Section heading in the suite's accent, with the gap above that separates it from
        /// the previous group. Sentence case, per §7.
        /// </summary>
        public static void Heading(Listing_Standard listing, string text)
        {
            listing.Gap(Metrics.Space8);
            Rect r = listing.GetRect(Metrics.HeaderH);
            Color prev = GUI.color;
            GUI.color = RadiusTheme.Accent;
            RadiusFont.LabelBold(r, text, GameFont.Small, RadiusTheme.Accent);
            GUI.color = prev;
        }

        /// <summary>
        /// Right-aligned reset button. Returns true on the frame it is pressed; the caller
        /// restores its own defaults, because only it knows them (Nielsen #3).
        /// </summary>
        public static bool ResetButton(Listing_Standard listing, string label)
        {
            listing.Gap(Metrics.Space8);
            Rect row = listing.GetRect(Metrics.ButtonH);
            float w = Mathf.Min(220f, row.width * 0.4f);
            return Widgets.ButtonText(new Rect(row.xMax - w, row.y, w, row.height), label);
        }

        /// <summary>Explanatory paragraph in dim text - for the sentence under a heading.</summary>
        public static void Note(Listing_Standard listing, string text)
        {
            Color prev = GUI.color;
            GUI.color = Palette.TextDim;
            float h = RadiusFont.Height(text, listing.ColumnWidth, GameFont.Small);
            Widgets.Label(listing.GetRect(h), text);
            GUI.color = prev;
        }
    }
}
