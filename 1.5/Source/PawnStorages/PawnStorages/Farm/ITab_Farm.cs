using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using PawnStorages.Farm.Comps;
using PawnStorages.Farm.Interfaces;
using RimWorld;
using TMPro;
using UnityEngine;
using Verse;
using Verse.Noise;

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

        public override void FillTab()
        {

            Rect rect1 = new Rect(0.0f, 0.0f, ITab_Storage.WinSize.x, ITab_Storage.WinSize.y).ContractedBy(10f);
            Widgets.BeginGroup(rect1);
            // if (this.IsPrioritySettingVisible)
            // {
            //     Text.Font = GameFont.Small;
            //     Rect rect2 = new Rect(0.0f, 0.0f, 160f, this.TopAreaHeight - 6f);
            //     if (Widgets.ButtonText(rect2, (string)("Priority".Translate() + ": " + settings.Priority.Label().CapitalizeFirst())))
            //     {
            //         List<FloatMenuOption> options = new List<FloatMenuOption>();
            //         foreach (StoragePriority storagePriority in Enum.GetValues(typeof(StoragePriority)))
            //         {
            //             if (storagePriority != StoragePriority.Unstored)
            //             {
            //                 StoragePriority localPr = storagePriority;
            //                 options.Add(new FloatMenuOption(localPr.Label().CapitalizeFirst(), (Action)(() => settings.Priority = localPr)));
            //             }
            //         }
            //         Find.WindowStack.Add((Window)new FloatMenu(options));
            //     }
            //     UIHighlighter.HighlightOpportunity(rect2, "StoragePriority");
            // }

            // Rect rect3 = new Rect(0.0f, this.TopAreaHeight, rect1.width, rect1.height - this.TopAreaHeight);
            // Bill[] array1 = BillUtility.GlobalBills().Where<Bill>((Func<Bill, bool>)(b => b is Bill_Production && b.GetSlotGroup() == storeSettingsParent && b.recipe.WorkerCounter.CanPossiblyStore((Bill_Production)b, b.GetSlotGroup()))).ToArray<Bill>();

            // ThingFilter filter = settings.filter;

            // IEnumerable<SpecialThingFilterDef> forceHiddenFilters = this.HiddenSpecialThingFilters();
            // ThingFilterUI.DoThingFilterConfigWindow(rect3, Parent.ThingFilterState, Parent.ThingFilter, Parent.ThingFilter, 8);
            // Bill[] array2 = BillUtility.GlobalBills().Where<Bill>((Func<Bill, bool>)(b => b is Bill_Production && b.GetSlotGroup() == storeSettingsParent && b.recipe.WorkerCounter.CanPossiblyStore((Bill_Production)b, b.GetSlotGroup()))).ToArray<Bill>();
            // foreach (Bill bill in ((IEnumerable<Bill>)array1).Except<Bill>((IEnumerable<Bill>)array2))
            // Messages.Message((string)"MessageBillValidationStoreZoneInsufficient".Translate((NamedArgument)bill.LabelCap, (NamedArgument)bill.billStack.billGiver.LabelShort.CapitalizeFirst(), (NamedArgument)SlotGroup.GetGroupLabel(bill.GetSlotGroup())), (LookTargets)(bill.billStack.billGiver as Thing), MessageTypeDefOf.RejectInput, false);
            // PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.StorageTab, KnowledgeAmount.FrameDisplayed);

            Widgets.Label(new Rect(5.0f, 0.0f, rect1.width, 30f), "Farm Management");

            Rect tabRect = new Rect(0.0f, 30.0f, rect1.width, rect1.height - 30f).ContractedBy(10f);
            Rect scrollViewRect = new Rect(tabRect);

            float totalHeight = Parent.AllowableThing.Count * (LineHeight + 2f);

            Rect viewRect = new Rect(0.0f, 0.0f, scrollViewRect.width, totalHeight);

            Widgets.AdjustRectsForScrollView(tabRect, ref scrollViewRect, ref viewRect);
            Widgets.BeginScrollView(scrollViewRect, ref ScrollPosition, viewRect);

            // alternate = false;
            float num = 0.0f;

            foreach (var tDef in Parent.AllowableThing)
            {
                Color? nullable = new Color?();
                // if (this.searchFilter.Matches(tDef))
                    // ++this.matchCount;
                // else
                nullable = new Color?(Listing_TreeThingFilter.NoMatchColor);

                var iconWidth = 20f;
                Widgets.DefIcon(new Rect(5f, num, iconWidth, LineHeight), (Def)tDef, drawPlaceholder: true, color: nullable);
                
                var labelX = iconWidth + 2f + 5f;
                Rect labelLeft = new Rect(labelX, num, scrollViewRect.width - 26f - labelX - 5f, LineHeight);


                Widgets.DrawHighlightIfMouseover(labelLeft);
                if (!tDef.DescriptionDetailed.NullOrEmpty())
                {
                    if (Mouse.IsOver(labelLeft))
                        GUI.DrawTexture(labelLeft, (Texture)TexUI.HighlightTex);
                    TooltipHandler.TipRegion(labelLeft, (TipSignal)tDef.DescriptionDetailed);
                }

                var label = (string)tDef.LabelCap;
                Text.Anchor = TextAnchor.MiddleLeft;
                GUI.color = nullable ?? Color.white;
                // labelLeft.width = scrollViewRect.width - 26f - labelLeft.xMin + widthOffset;
                // labelLeft.yMax += 5f;
                // labelLeft.yMin -= 5f;
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
