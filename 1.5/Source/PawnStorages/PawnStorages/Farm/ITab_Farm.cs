using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using UnityEngine;
using UnityEngine.UIElements;
using Verse;
using Verse.Noise;
using static HarmonyLib.Code;
using static RimWorld.ITab_Pawn_Log_Utility;
using static UnityEngine.Random;
using Color = UnityEngine.Color;

namespace PawnStorages.Farm
{
    public class ITab_Farm : ITab
    {
        private static readonly Vector2 WinSize = new(300f, 480f);
        public readonly ThingFilterUI.UIState ThingFilterState = new();
        public const float lineHeight = 60f;
        private bool alternate = false;

        public CompFarmStorage compFarmStorage => this.SelThing.TryGetComp<CompFarmStorage>();
        public bool NeedsDrop => PawnStoragesMod.settings.AllowNeedsDrop || compFarmStorage.Props.needsDrop;

        public ITab_Farm()
        {
            this.size = ITab_PenAnimals.WinSize;
            this.labelKey = "TabPenAnimals";
        }

        public float PawnFullness(Pawn pawn)
        {
            if (ThingCompUtility.TryGetComp<CompEggLayer>(pawn, out var compLayer))
            {
                return compLayer.eggProgress;
            }

            if (ThingCompUtility.TryGetComp<CompHasGatherableBodyResource>(pawn, out var compGatherable))
            {
                return compGatherable.Fullness;
            }

            return 0;
        }

        public bool DrawLine(float position, float width, Pawn pawn)
        {
            var rect = new Rect(0.0f, position, width, lineHeight);

            if (alternate)
            {
                Widgets.DrawRectFast(rect, new Color(1f, 1f, 1f, ITab_Pawn_Log_Utility.AlternateAlpha));
            }
            alternate = !alternate;

            var nullable = new Color?(Listing_TreeThingFilter.NoMatchColor);

            Widgets.DefIcon(new Rect(5f, position+7.5f, 45f, 45f), (Def)pawn.def, drawPlaceholder: true, color: nullable);

            var label = new StringBuilder(pawn.LabelShort);

            if (NeedsDrop)
            {
                if (pawn.needs.food.Starving)
                {
                    label.Append(" (Starving!)");
                }

                Widgets.Label(new Rect(55f, position, width - 90f, 20f), label.ToString());
                Widgets.Label(new Rect(55f, position + 20f, width - 90f, 20f),
                    $"Nutrition: {Mathf.CeilToInt(pawn.needs.food.CurLevelPercentage)}%");
                if (pawn.gender != Gender.Male)
                    Widgets.Label(new Rect(55f, position + 40f, width - 90f, 20f),
                        $"Progress: {Mathf.CeilToInt(PawnFullness(pawn) * 100)}%");
            }

            var btn = new Rect(new Vector2(width - 50f, position + 15f), new Vector2(30f, 30f));

            if (Widgets.ButtonImage(btn, TexButton.Drop, Color.white, GenUI.MouseoverColor, true))
            {
                return true;
            }

            return false;
        }

        public override void FillTab()
        {
            if (this.compFarmStorage == null) return;
            if (compFarmStorage.StoredPawns.Count <= 0) return;

            Widgets.Label(new Rect(5.0f, 0.0f, ITab_Farm.WinSize.x, 30f), "Farm Animals");

            var tabRect = new Rect(0.0f, 30.0f, ITab_Farm.WinSize.x, ITab_Farm.WinSize.y-30f).ContractedBy(10f);
            var scrollViewRect = new Rect(tabRect);

            var totalHeight = compFarmStorage.StoredPawns.Count * lineHeight;

            var viewRect = new Rect(0.0f, 0.0f, scrollViewRect.width, totalHeight);

            Widgets.AdjustRectsForScrollView(tabRect, ref scrollViewRect, ref viewRect);
            Widgets.BeginScrollView(scrollViewRect, ref ThingFilterState.scrollPosition, viewRect);
            
            alternate = false;
            var num = 0.0f;
            List<Pawn> removed = new List<Pawn>();
            foreach (var pawn in compFarmStorage.StoredPawns)
            {
                if (DrawLine(num, scrollViewRect.width, pawn))
                {
                    removed.Add(pawn);
                }
                num += lineHeight;
            }

            foreach (var pawn in removed)
            {
                compFarmStorage.ReleaseSingle(compFarmStorage.parent.Map, pawn, true);
            }

            Widgets.EndScrollView();
        }
    }
}
