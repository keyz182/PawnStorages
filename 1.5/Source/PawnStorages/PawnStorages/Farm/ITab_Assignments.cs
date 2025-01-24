using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PawnStorages.Farm.Comps;
using RimWorld;
using UnityEngine;
using Verse;
using Color = UnityEngine.Color;

namespace PawnStorages.Farm
{
    public class ITab_Assignments : ITab
    {
        private static readonly Vector2 WinSize = new(300f, 480f);
        private Vector2 scrollPos = new(0, 0);
        public const float LineHeight = 60f;

        public CompFarmStorage compFarmStorage => SelThing.TryGetComp<CompFarmStorage>();

        public ITab_Assignments()
        {
            size = ITab_PenAnimals.WinSize;
            labelKey = "PS_AssignmentTab";
        }

        public void DrawLine(float position, float width, Pawn animal, ref bool alternate)
        {
            Rect rect = new Rect(0.0f, position, width, LineHeight);

            FarmJob_MapComponent comp = SelThing.Map.GetComponent<FarmJob_MapComponent>();

            if (alternate)
            {
                Widgets.DrawRectFast(rect, new Color(1f, 1f, 1f, ITab_Pawn_Log_Utility.AlternateAlpha));
            }

            alternate = !alternate;

            Widgets.DefIcon(new Rect(5f, position + 7.5f, 45f, 45f), animal.def, drawPlaceholder: true, color: Listing_TreeThingFilter.NoMatchColor);

            Widgets.Label(new Rect(55f, position, width - 90f, 20f),"PS_FarmTab_Name".Translate(animal.NameFullColored));
            Widgets.Label(new Rect(55f, position + 30f, width - 90f, 20f), comp != null && comp.farmStorageAssignments.TryGetValue(animal, out CompFarmStorage assignment) ? "PS_AssignmentTab_Assigned".Translate(animal.NameFullColored, assignment.parent.Label).ToString() : "");

            Rect btn = new Rect(new Vector2(width - 100f, position + 15f), new Vector2(80f, 30f));

            if (!Widgets.ButtonText(btn, "PS_AssignmentTab_Set".Translate(), true, false, true)) return;


            if (comp == null) return;

            Messages.Message(
                comp.farmStorageAssignments.TryGetValue(animal, out CompFarmStorage storageAssignment)
                    ? "PS_AssignmentTab_Reassigned".Translate(animal.NameFullColored, storageAssignment.parent.Label, compFarmStorage.parent.Label)
                    : "PS_AssignmentTab_Assigned".Translate(animal.NameFullColored, compFarmStorage.parent.Label), MessageTypeDefOf.RejectInput, false);
            comp.farmStorageAssignments[animal] = compFarmStorage;
        }

        public override void FillTab()
        {
            if (compFarmStorage == null) return;

            Widgets.Label(new Rect(5.0f, 0.0f, WinSize.x, 30f), "PS_AssignmentTab_TopLabel".Translate());

            Rect tabRect = new Rect(0.0f, 30.0f, WinSize.x, WinSize.y - 30f).ContractedBy(10f);
            Rect scrollViewRect = new Rect(tabRect);

            List<Pawn> animals = Find.CurrentMap.mapPawns.PawnsInFaction(Faction.OfPlayer).Where(p => p.IsNonMutantAnimal).ToList();

            float totalHeight = animals.Count * LineHeight;

            Rect viewRect = new Rect(0.0f, 0.0f, scrollViewRect.width, totalHeight);

            Widgets.AdjustRectsForScrollView(tabRect, ref scrollViewRect, ref viewRect);
            Widgets.BeginScrollView(scrollViewRect, ref scrollPos, viewRect);

            bool alternate = false;
            float num = 0.0f;
            List<Pawn> removed = [];
            foreach (Pawn thing in animals)
            {
                Pawn animal = (Pawn) thing;
                DrawLine(num, scrollViewRect.width, animal, ref alternate);
                num += LineHeight;
            }

            Widgets.EndScrollView();
        }
    }
}
