using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Security.Cryptography;
using PawnStorages.Farm.Comps;
using PawnStorages.Farm.Interfaces;
using RimWorld;
using TMPro;
using UnityEngine;
using Verse;
using Verse.Noise;
using Verse.Sound;
using static HarmonyLib.Code;
using static UnityEngine.Random;

namespace PawnStorages.Farm
{
    public class ITab_Farm : ITab
    {
        public ITab_Farm()
        {
            size = ITab_PenAnimals.WinSize;
            labelKey = "PS_FarmTab";
        }

        public IFarmTabParent Parent => SelThing as IFarmTabParent;

        protected float TopAreaHeight => 20f;

        protected const float LineHeight = 20f;
        protected Vector2 ScrollPosition = Vector2.zero;
        protected QuickSearchWidget QuickSearchWidget = new QuickSearchWidget();

        public override void FillTab()
        {

            Rect tabRect = new Rect(0.0f, 0.0f, ITab_Storage.WinSize.x, ITab_Storage.WinSize.y).ContractedBy(10f);
            Widgets.BeginGroup(tabRect);

            Rect menuRect = new Rect(0.0f, 20f, tabRect.width, tabRect.height - 20f);
            Widgets.DrawMenuSection(menuRect);
            float num1 = menuRect.width - 2f;
            Rect buttonRect = new Rect(menuRect.x + 3f, menuRect.y + 3f, (float)((double)num1 / 2.0 - 3.0 - 1.5), 24f);
            if (Widgets.ButtonText(buttonRect, (string)"ClearAll".Translate()))
            {
                // filter.SetDisallowAll(forceHiddenDefs, forceHiddenFilters);
                SoundDefOf.Checkbox_TurnedOff.PlayOneShotOnCamera();
            }
            if (Widgets.ButtonText(new Rect(buttonRect.xMax + 3f, buttonRect.y, buttonRect.width, 24f), (string)"AllowAll".Translate()))
            {
                // filter.SetAllowAll(parentFilter);
                SoundDefOf.Checkbox_TurnedOn.PlayOneShotOnCamera();
            }

            // tabRect.yMin = menuRect.yMax;
            Rect searchWidgetRect = new Rect(menuRect.x + 3f, menuRect.yMin + 26f, (float)((double)tabRect.width - 16.0 - 6.0), 24f);
            QuickSearchWidget.OnGUI(searchWidgetRect);
            // tabRect.yMin = searchWidgetRect.yMax + 3f;
            // tabRect.xMax -= 4f;
            // tabRect.yMax -= 6f;

            float totalHeight = Parent.AllowableThing.Count * (LineHeight + 2f);

            Rect viewRect = new Rect(0.0f, 0.0f, menuRect.width - 16f, totalHeight);

            menuRect.yMin += 50f;
            menuRect.yMax -= 4f;

            Widgets.BeginScrollView(menuRect, ref ScrollPosition, viewRect);

            // alternate = false;
            float num = 0.0f;

            List<ThingDef> allowable;
            if (QuickSearchWidget.filter.Active)
            {
                allowable = Parent.AllowableThing.Where(tDef =>
                    tDef.LabelCap.ToString().ToLower().Contains(QuickSearchWidget.filter.Text.ToLower())).ToList();
            }
            else
            {
                allowable = Parent.AllowableThing;
            }

            foreach (var tDef in allowable)
            {
                var iconWidth = 20f;
                Widgets.DefIcon(new Rect(5f, num, iconWidth, LineHeight), (Def)tDef, drawPlaceholder: true);
                
                var labelX = iconWidth + 2f + 5f;
                Rect labelLeft = new Rect(labelX, num, viewRect.width - 26f - labelX - 5f, LineHeight);


                Widgets.DrawHighlightIfMouseover(labelLeft);
                if (!tDef.DescriptionDetailed.NullOrEmpty())
                {
                    if (Mouse.IsOver(labelLeft))
                        GUI.DrawTexture(labelLeft, (Texture)TexUI.HighlightTex);
                    TooltipHandler.TipRegion(labelLeft, (TipSignal)tDef.DescriptionDetailed);
                }

                var label = (string)tDef.LabelCap;
                Text.Anchor = TextAnchor.MiddleLeft;
                GUI.color = Color.white;

                Widgets.Label(labelLeft, label.Truncate(labelLeft.width));
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;


                bool checkOn = Parent.AllowedThings[tDef];
                bool flag = checkOn;
                
                Widgets.Checkbox(new Vector2(labelLeft.xMax, num), ref flag, LineHeight, paintable: true);
                if (checkOn != flag)
                    Parent.AllowedThings[tDef] = flag;

                num += LineHeight + 2f;
            }


            Widgets.EndScrollView();

            Widgets.EndGroup();
        }

    }
}
