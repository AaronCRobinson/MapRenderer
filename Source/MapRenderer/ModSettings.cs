using Verse;
using UnityEngine;
using System.Collections.Generic;
using ModSettingsHelper;

namespace MapRenderer
{
    public class MapRendererSettings : ModSettings
    {
        private const string defaulfExportFormat = "JPG";
        private const int defaultQuality = 512;

        public string exportFormat = defaulfExportFormat;
        public int quality = defaultQuality;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.exportFormat, "exportFormat", defaulfExportFormat);
            Scribe_Values.Look(ref this.quality, "quality", defaultQuality);
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
            AddExportFormatOptions(inRect);
            ModWindowHelper.MakeTextFieldNumericLabeled<int>(inRect, "MR_QualityLabel".Translate(), ref settings.quality, ref this.qualityBuffer, 0, 32768);
            settings.Write();
        }

        private void AddExportFormatOptions(Rect rect)
        {
            GUI.BeginGroup(rect);
            ModWindowHelper.MakeLabel(rect, "Pick the desired export format:");
            ModWindowHelper.MakeLabeledRadioList(rect, exportFormats, ref settings.exportFormat);
            GUI.EndGroup();
        }
    }
}