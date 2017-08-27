using System;
using Verse;
using UnityEngine;
using ModSettingsHelper;

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

        private string qualityBuffer;

        public MapRendererMod(ModContentPack content) : base(content)
        {
            settings = GetSettings<MapRendererSettings>();
        }

        public override string SettingsCategory() => "MR_MapRender".Translate();

        public override void DoSettingsWindowContents(Rect inRect)
        {
            ModWindowHelper.Reset();
            ModWindowHelper.MakeLabeledCheckbox(inRect, "MR_ShowWeatherLabel".Translate(), ref settings.showWeather);
            AddExportFormatOptions(inRect);
            ModWindowHelper.MakeTextFieldNumericLabeled<int>(inRect, "MR_QualityLabel".Translate(), ref settings.quality, ref this.qualityBuffer, 0, 1500);
            ModWindowHelper.MakeLabeledTextField(inRect, "MR_PathLabel".Translate(), ref settings.path);
            settings.Write();
        }

        private void AddExportFormatOptions(Rect rect)
        {
            GUI.BeginGroup(rect);
            ModWindowHelper.MakeLabel(rect, $"{"MR_AddExportFormatOptionsDescription".Translate()}:");
            ModWindowHelper.MakeLabeledRadioList(rect, exportFormats, ref settings.exportFormat);
            GUI.EndGroup();
        }
    }
}