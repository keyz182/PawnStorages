using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;
using Color = UnityEngine.Color;

namespace PawnStorages.Farm
{
    public class ITab_Breeding : ITab
    {
        private static readonly Vector2 WinSize = new(300f, 480f);
        public readonly ThingFilterUI.UIState ThingFilterState = new();
        public const float lineHeight = 40f;
        private bool alternate;

        public CompFarmStorage compFarmStorage => SelThing.TryGetComp<CompFarmStorage>();
        public CompFarmNutrition compFarmNutrition => SelThing.TryGetComp<CompFarmNutrition>();

        public ITab_Breeding()
        {
            size = ITab_PenAnimals.WinSize;
            labelKey = "PS_BreedingTab";
        }

        public void DrawLine(float position, float width, PawnKindDef pawn, float progress)
        {
            Rect rect = new Rect(0.0f, position, width, lineHeight);

            if (alternate)
            {
                Widgets.DrawRectFast(rect, new Color(1f, 1f, 1f, ITab_Pawn_Log_Utility.AlternateAlpha));
            }

            alternate = !alternate;

            Widgets.DefIcon(new Rect(5f, position + 2.5f, 35f, 35f), pawn, drawPlaceholder: true, color: Listing_TreeThingFilter.NoMatchColor);

            StringBuilder label = new(pawn.LabelCap);

            Widgets.Label(new Rect(45f, position, width - 90f, 20f), label.ToString());

            if (compFarmNutrition.Props.doesBreeding)
            {
                Widgets.Label(new Rect(45f, position + 20f, width - 90f, 20f),
                    $"Progress: {Mathf.CeilToInt(progress * 100)}%");
            }
        }

        public override void FillTab()
        {
            if (compFarmNutrition == null) return;
            if (!compFarmNutrition.breedingProgress.Any()) return;

            Widgets.Label(new Rect(5.0f, 0.0f, WinSize.x, 30f), "Breeding Animals");

            Rect tabRect = new Rect(0.0f, 30.0f, WinSize.x, WinSize.y - 30f).ContractedBy(10f);
            Rect scrollViewRect = new Rect(tabRect);

            float totalHeight = compFarmNutrition.breedingProgress.Count * lineHeight;

            Rect viewRect = new Rect(0.0f, 0.0f, scrollViewRect.width, totalHeight);

            Widgets.AdjustRectsForScrollView(tabRect, ref scrollViewRect, ref viewRect);
            Widgets.BeginScrollView(scrollViewRect, ref ThingFilterState.scrollPosition, viewRect);

            alternate = false;
            float num = 0.0f;

            foreach (var f in compFarmNutrition.breedingProgress)
            {
                DrawLine(num, scrollViewRect.width, f.Key, f.Value);

                num += lineHeight;
            }


            Widgets.EndScrollView();
        }
    }
}
