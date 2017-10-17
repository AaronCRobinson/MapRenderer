using System;
using Verse;
using UnityEngine;
using SettingsHelper;

namespace MapRenderer
{
    public class MapRendererSettings : ModSettings
    {
        private const string defaulfExportFormat = "JPG";
        private const int defaultQuality = 750;

        public string exportFormat = defaulfExportFormat;
        public string path;
        public int quality = defaultQuality;
        public bool showWeather = true;

        // NOTE: is this redudant?
        public MapRendererSettings()
        {
            this.path = DesktopPath;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.exportFormat, "exportFormat", defaulfExportFormat);
            Scribe_Values.Look(ref this.quality, "quality", defaultQuality);
            Scribe_Values.Look(ref this.path, "path", DesktopPath);
            Scribe_Values.Look(ref this.showWeather, "showWeather", true);
        }

        private string DesktopPath
        {
            get
            {
                return Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            }
        }
    }

    class MapRendererMod : Mod
    {
        public static MapRendererSettings settings;

        public static string[] exportFormats = { "JPG", "PNG" };

        public MapRendererMod(ModContentPack content) : base(content)
        {
            settings = GetSettings<MapRendererSettings>();
            ListingStandardHelper.Gap = 10f;
        }

        public override string SettingsCategory() => "MR_MapRender".Translate();

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listing_Standard = new Listing_Standard();
            listing_Standard.Begin(inRect);
            listing_Standard.AddLabeledCheckbox("MR_ShowWeatherLabel".Translate(), ref settings.showWeather);
            listing_Standard.AddHorizontalLine(3f);
            listing_Standard.AddLabeledRadioList($"{"MR_AddExportFormatOptionsDescription".Translate()}:", exportFormats, ref settings.exportFormat);
            listing_Standard.AddHorizontalLine(3f);
            listing_Standard.AddLabeledNumericalTextField<int>("MR_QualityLabel".Translate(), ref settings.quality);
            listing_Standard.AddLabeledTextField("MR_PathLabel".Translate(), ref settings.path);
            listing_Standard.End();
            settings.Write();
        }

    }
}