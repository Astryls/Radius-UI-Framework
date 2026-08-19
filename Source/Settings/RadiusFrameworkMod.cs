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
            // The one suite-wide performance lever (Throttle.EveryScaled). Shipped scribed but
            // UI-less at generation 2, which made it a config-file-only setting - §8 #1/#6
            // violations. The label carries the live value; the tooltip owns the explanation.
            var refreshRect = new Rect(inRect.x, y, inRect.width, Metrics.RowText);
            Widgets.Label(refreshRect,
                "RadiusUI.Settings.Refresh".Translate(settings.refreshMult.ToString("0.0") + "x"));
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
    }
}
