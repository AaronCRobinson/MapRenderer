using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using UnityEngine;

namespace MapRenderer
{
    public class Dialog_RenderMap : Window
    {
        private const float TitleHeight = 42f;

        private const float ButtonHeight = 35f;

        public string text;

        public string title;

        public string buttonAText;

        public Action buttonAAction;

        public string buttonBText;

        public Action buttonBAction;

        public float interactionDelay;  

        private Vector2 scrollPosition = Vector2.zero;

        private float creationRealTime = -1f;

        public Dialog_RenderMap()
        {
            this.title = "MR_MapRender".Translate();
            this.text = "MR_MapRenderDescription".Translate();
            this.buttonAText = "MR_RenderButtonLabel".Translate();
            this.buttonAAction = () => {
                RenderMap renderMap = new RenderMap();
                renderMap.Render();
            };
            this.buttonBText = "MR_CloseButtoLabel".Translate();
            this.buttonBAction = () => { };
            this.creationRealTime = RealTime.LastRealTime;
            this.forcePause = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            float num = inRect.y;
            if (!this.title.NullOrEmpty())
            {
                Text.Font = GameFont.Medium;
                Widgets.Label(new Rect(0f, num, inRect.width, TitleHeight), this.title);
                num += TitleHeight;
            }
            Text.Font = GameFont.Small;
            Rect outRect = new Rect(inRect.x, num, inRect.width, inRect.height - ButtonHeight - 5f - num);
            float width = outRect.width - 16f;
            Rect viewRect = new Rect(0f, 0f, width, Text.CalcHeight(this.text, width));
            Widgets.BeginScrollView(outRect, ref this.scrollPosition, viewRect, true);
            Widgets.Label(new Rect(0f, 0f, viewRect.width, viewRect.height), this.text);
            Widgets.EndScrollView();
            /*if (this.buttonADestructive)
            {
                GUI.color = new Color(1f, 0.3f, 0.35f);
            }*/
            string label = (!this.InteractionDelayExpired) ? (this.buttonAText + "(" + Mathf.Ceil(this.TimeUntilInteractive).ToString("F0") + ")") : this.buttonAText;
            float width2 = inRect.width / 2f - 20f;
            if (Widgets.ButtonText(new Rect(inRect.width / 2f + 20f, inRect.height - ButtonHeight, width2, ButtonHeight), label, true, false, true) && this.InteractionDelayExpired)
            {
                buttonAAction?.Invoke();
                this.Close(true);
            }
            GUI.color = Color.white;
            if (this.buttonBText != null && Widgets.ButtonText(new Rect(0f, inRect.height - ButtonHeight, width2, ButtonHeight), this.buttonBText, true, false, true))
            {
                buttonBAction?.Invoke();
                this.Close(true);
            }
        }

        private float TimeUntilInteractive
        {
            get
            {
                return this.interactionDelay - (Time.realtimeSinceStartup - this.creationRealTime);
            }
        }

        private bool InteractionDelayExpired
        {
            get
            {
                return this.TimeUntilInteractive <= 0f;
            }
        }

        public override void PostClose()
        {
            if (Current.ProgramState == ProgramState.Playing)
            {
                Find.MainTabsRoot.SetCurrentTab(MainButtonDefOf.Menu, true);
            }
        }

    }
}
