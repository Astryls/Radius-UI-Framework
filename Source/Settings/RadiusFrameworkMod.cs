using UnityEngine;
using Verse;

namespace RadiusUI.Framework
{
    /// <summary>
    /// The framework's Mod entry point: loads the persisted theme settings, wires them into
    /// <see cref="RadiusTheme"/>, and draws the suite-wide settings page (accent picker,
    /// corner roundness, font flags). The framework applies no Harmony patches and draws
    /// nothing in-game by itself - it is inert until a consumer calls it.
    ///
    /// Threading: this constructor runs on a background thread during game load
    /// (LongEventHandler), so it must not touch Unity graphics APIs - it only reads the
    /// settings file and assigns managed fields, which is safe.
    /// </summary>
    public class RadiusFrameworkMod : Mod
    {
        private readonly RadiusThemeSettings settings;

        public RadiusFrameworkMod(ModContentPack content) : base(content)
        {
            settings = GetSettings<RadiusThemeSettings>();
            RadiusTheme.Initialize(settings);
        }

        public override string SettingsCategory() => "RadiusUI.Framework.Title".Translate();

        public override void WriteSettings()
        {
            base.WriteSettings();
            RadiusTheme.Notify_SettingsChanged();
        }

        /// <summary>
        /// Suite settings page. Vanilla widgets for sliders/checkboxes (this is the vanilla
        /// mod-options dialog, so vanilla idioms are the consistent choice); the accent
        /// swatches dogfood CardChrome so the picker previews the actual suite chrome.
        /// </summary>
        public override void DoSettingsWindowContents(Rect inRect)
        {
            const float swatch = 30f;
            const float gap = Metrics.Space8;
            float y = inRect.y + Metrics.Space8;

            Text.Font = GameFont.Small;

            // ---- accent picker -------------------------------------------------------
            Widgets.Label(new Rect(inRect.x, y, inRect.width, Metrics.RowText),
                "RadiusUI.Settings.AccentHeading".Translate());
            y += Metrics.RowText + Metrics.Space4;

            for (int i = 0; i < Palette.Accents.Length; i++)
            {
                var r = new Rect(inRect.x + i * (swatch + gap), y, swatch, swatch);
                bool selected = settings.accentIndex == i;

                CardChrome.Rounded(r, Palette.Accents[i], Metrics.RadiusChip);
                if (selected)
                {
                    // Selection ring: Ink outline just outside the swatch.
                    CardChrome.Outline(r.ExpandedBy(2f), Palette.Ink, 2f, Metrics.RadiusChip + 2f);
                }
                else if (Mouse.IsOver(r))
                {
                    CardChrome.Hover(r, Metrics.RadiusChip);
                }

                TooltipHandler.TipRegion(r, AccentName(i).CapitalizeFirst());
                if (Widgets.ButtonInvisible(r) && settings.accentIndex != i)
                {
                    settings.accentIndex = i;
                    RadiusTheme.Notify_SettingsChanged();
                }
            }
            y += swatch + Metrics.Space8;

            GUI.color = Palette.TextDim;
            Widgets.Label(new Rect(inRect.x, y, inRect.width, Metrics.RowText),
                "RadiusUI.Settings.AccentCurrent".Translate(AccentName(settings.accentIndex)));
            GUI.color = Color.white;
            y += Metrics.RowText + Metrics.Space16;

            // ---- corner roundness ----------------------------------------------------
            var labelRect = new Rect(inRect.x, y, inRect.width, Metrics.RowText);
            Widgets.Label(labelRect,
                "RadiusUI.Settings.Roundness".Translate(settings.radiusScale.ToStringPercent()));
            TooltipHandler.TipRegion(labelRect, "RadiusUI.Settings.RoundnessTip".Translate());
            y += Metrics.RowText;

            float newScale = Widgets.HorizontalSlider(
                new Rect(inRect.x, y, Mathf.Min(inRect.width, 340f), 24f),
                settings.radiusScale, 0f, RadiusThemeSettings.MaxRadiusScale, middleAlignment: true,
                label: null, leftAlignedLabel: null, rightAlignedLabel: null, roundTo: 0.05f);
            if (!Mathf.Approximately(newScale, settings.radiusScale))
            {
                settings.radiusScale = newScale;
                RadiusTheme.Notify_SettingsChanged();
            }
            y += 24f + Metrics.Space8;

            // Live preview card so the slider is judged against real chrome, not memory.
            var preview = new Rect(inRect.x, y, 220f, 44f);
            CardChrome.Card(preview, Mouse.IsOver(preview));
            CardChrome.Rounded(new Rect(preview.x + Metrics.PadCard, preview.y + 7f, swatch, swatch),
                RadiusTheme.Accent, Metrics.RadiusChip);
            y += 44f + Metrics.Space16;

            // ---- background refresh --------------------------------------------------
            // The one suite-wide performance lever (Throttle.EveryScaled), and as of this
            // generation the ONLY one: Mission Control used to carry a duplicate slider with
            // the same 0.5-4 range, which would have compounded with this one (x4 and x4 = x16)
            // once it started reading RadiusTheme.RefreshMult. It now defers to this setting.
            // The label states the meaning in words - see RefreshLabel for why the bare
            // multiplier is never shown.
            var refreshRect = new Rect(inRect.x, y, inRect.width, Metrics.RowText);
            Widgets.Label(refreshRect, RefreshLabel(settings.refreshMult));
            TooltipHandler.TipRegion(refreshRect, "RadiusUI.Settings.RefreshTip".Translate());
            y += Metrics.RowText;

            float newMult = Widgets.HorizontalSlider(
                new Rect(inRect.x, y, Mathf.Min(inRect.width, 340f), 24f),
                settings.refreshMult, RadiusThemeSettings.MinRefreshMult,
                RadiusThemeSettings.MaxRefreshMult, middleAlignment: true,
                label: null, leftAlignedLabel: null, rightAlignedLabel: null, roundTo: 0.25f);
            if (!Mathf.Approximately(newMult, settings.refreshMult))
            {
                settings.refreshMult = newMult;
                RadiusTheme.Notify_SettingsChanged();
            }
            y += 24f + Metrics.Space16;

            // ---- font flags ----------------------------------------------------------
            bool boldBefore = settings.fontBold;
            bool italicBefore = settings.fontItalic;

            var boldRect = new Rect(inRect.x, y, 300f, Metrics.RowText + 4f);
            Widgets.CheckboxLabeled(boldRect, "RadiusUI.Settings.Bold".Translate(), ref settings.fontBold);
            TooltipHandler.TipRegion(boldRect, "RadiusUI.Settings.BoldTip".Translate());
            y += Metrics.RowText + Metrics.Space8;

            var italicRect = new Rect(inRect.x, y, 300f, Metrics.RowText + 4f);
            Widgets.CheckboxLabeled(italicRect, "RadiusUI.Settings.Italic".Translate(), ref settings.fontItalic);
            TooltipHandler.TipRegion(italicRect, "RadiusUI.Settings.ItalicTip".Translate());
            y += Metrics.RowText + Metrics.Space24;

            if (settings.fontBold != boldBefore || settings.fontItalic != italicBefore)
            {
                RadiusTheme.Notify_SettingsChanged();
            }

            // ---- reset ---------------------------------------------------------------
            if (Widgets.ButtonText(new Rect(inRect.x, y, 200f, Metrics.ButtonH),
                    "RadiusUI.Settings.Reset".Translate()))
            {
                settings.ResetToDefaults();
                RadiusTheme.Notify_SettingsChanged();
            }
        }

        private static string AccentName(int i)
        {
            return ("RadiusUI.Accent." + Palette.AccentNames[i]).Translate();
        }

        /// <summary>
        /// Plain-language description of the background-refresh multiplier.
        ///
        /// THE RAW MULTIPLIER MUST NEVER BE SHOWN ON ITS OWN. It scales the INTERVAL between
        /// background gathers, so "4.0x" reads as "four times as often" when it actually means
        /// a QUARTER of the rate - the number states the exact opposite of what it does. The
        /// old label was literally "Background refresh: 4.0x" with a tooltip that had to say
        /// "higher values refresh less often", i.e. the tooltip existed to contradict the
        /// number next to it.
        ///
        /// So the label now carries a word that moves in the SAME direction as the slider
        /// (Fresher -> Default -> Cheaper -> Cheapest) plus an explicit rate phrase stated as
        /// a frequency ("half as often"). Both halves decrease as the slider moves right, so
        /// neither can be read backwards.
        /// </summary>
        private static string RefreshLabel(float mult)
        {
            string word;
            if (mult <= 0.8f) word = "RadiusUI.Settings.RefreshFresher".Translate();
            else if (mult < 1.25f) word = "RadiusUI.Settings.RefreshDefault".Translate();
            else if (mult < 2.5f) word = "RadiusUI.Settings.RefreshCheaper".Translate();
            else word = "RadiusUI.Settings.RefreshCheapest".Translate();

            // Named phrases for the round values the slider lands on; a computed percentage
            // covers the 0.25 steps in between (roundTo on the slider below).
            string rate;
            if (Mathf.Approximately(mult, 1f)) rate = "RadiusUI.Settings.RefreshStandard".Translate();
            else if (Mathf.Approximately(mult, 0.5f)) rate = "RadiusUI.Settings.RefreshTwice".Translate();
            else if (Mathf.Approximately(mult, 2f)) rate = "RadiusUI.Settings.RefreshHalf".Translate();
            else if (Mathf.Approximately(mult, 4f)) rate = "RadiusUI.Settings.RefreshQuarter".Translate();
            else rate = "RadiusUI.Settings.RefreshPct".Translate(
                Mathf.RoundToInt(100f / Mathf.Max(0.01f, mult)));

            return "RadiusUI.Settings.Refresh".Translate(word, rate);
        }
    }
}
