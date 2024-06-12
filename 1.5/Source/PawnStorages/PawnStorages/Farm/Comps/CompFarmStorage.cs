using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnStorages.Farm.Comps
{
    [StaticConstructorOnStartup]
    public class CompFarmStorage : CompPawnStorage
    {
        public override string PawnTypeLabel =>
            "PS_StoredAnimals".Translate();
        public new CompProperties_FarmStorage Props => props as CompProperties_FarmStorage;

        public override int MaxStoredPawns() => PawnStoragesMod.settings.MaxPawnsInFarm;

        public new bool CanAssign(Pawn pawn, bool couldMakePrisoner = false) =>
            compAssignable != null && pawn.Faction == Faction.OfPlayer &&
            !pawn.RaceProps.Humanlike &&
            (compAssignable.AssignedPawns.Contains(pawn) || compAssignable.HasFreeSlot);

        public float NutritionRequiredPerDay() => compAssignable.AssignedPawns.Sum(animal =>
            SimplifiedPastureNutritionSimulator.NutritionConsumedPerDay(animal.def, animal.ageTracker.CurLifeStage));

        public new void StorePawn(Pawn pawn)
        {
            pawn.DeSpawn();
            innerContainer.TryAdd(pawn);

            parent.Map.mapDrawer.MapMeshDirty(parent.Position, MapMeshFlagDefOf.Things);

            if (compAssignable != null && !compAssignable.AssignedPawns.Contains(pawn))
            {
                compAssignable.TryAssignPawn(pawn);
            }

            labelDirty = true;
        }

        public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
        {
            foreach (FloatMenuOption floatMenuOption in base.CompFloatMenuOptions(selPawn))
            {
                yield return floatMenuOption;
            }
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (innerContainer.Count > 0)
            {
                yield return new Command_Action
                {
                    defaultLabel = "PS_ReleaseAnimals".Translate(),
                    action = delegate { ReleaseContents(parent.Map); },
                    icon = ContentFinder<Texture2D>.Get("UI/Buttons/PS_Release")
                };
                yield return new Command_Action
                {
                    defaultLabel = "PS_EjectAnimals".Translate(),
                    action = delegate { EjectContents(parent.Map); },
                    icon = ContentFinder<Texture2D>.Get("UI/Buttons/PS_Eject")
                };
            }

            if (DebugSettings.ShowDevGizmos)
            {
                yield return new Command_Action
                {
                    defaultLabel = "Produce",
                    action = TryProduce,
                    icon = ContentFinder<Texture2D>.Get("UI/Buttons/ReleaseAll")
                };
            }
        }

        public bool GetProduct(Pawn animal, out ThingDef resource, out int amount)
        {
            resource = null;
            amount = 0;

            //Milkable or shearable
            CompHasGatherableBodyResource bodyResource = animal.TryGetComp<CompHasGatherableBodyResource>();
            if (bodyResource is { Active: true })
            {
                resource = bodyResource.ResourceDef;
                amount = bodyResource.ResourceAmount;
                return true;
            }

            //EggLayer 
            CompEggLayer eggLayer = animal.TryGetComp<CompEggLayer>();
            if (eggLayer is not { Active: true }) return false;
            resource = eggLayer.Props.eggUnfertilizedDef;
            amount = eggLayer.Props.eggCountRange.RandomInRange;
            return true;
        }

        public void TryProduce()
        {
            foreach (Pawn pawn in innerContainer)
            {
                if (!GetProduct(pawn, out ThingDef thingDef, out int amount)) continue;
                while (amount > 0)
                {
                    int toSpawn = Mathf.Clamp(amount, 1, thingDef.stackLimit);
                    amount -= toSpawn;
                    Thing thingStack = ThingMaker.MakeThing(thingDef);
                    thingStack.stackCount = toSpawn;
                    GenPlace.TryPlaceThing(thingStack, parent.Position, parent.Map, ThingPlaceMode.Near);
                }
            }
        }

        public override string CompInspectStringExtra()
        {
            StringBuilder sb = new();
            if (innerContainer?.Any<Pawn>() != true) return sb.ToString().TrimStart().TrimEnd();
            sb.AppendLine();
            sb.AppendLine("PS_StoredPawns".Translate());
            foreach (Pawn pawn in innerContainer)
            {
                sb.AppendLine(pawn.needs.food.Starving
                    ? $"    - {pawn.LabelCap} [Starving!]"
                    : $"    - {pawn.LabelCap}");
            }

            return sb.ToString().TrimStart().TrimEnd();
        }
    }
}
