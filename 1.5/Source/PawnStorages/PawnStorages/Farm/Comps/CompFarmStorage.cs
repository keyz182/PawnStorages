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

        public IEnumerable<PawnKindDef> HeldPawnTypes => this.innerContainer.innerList.Select(p => p.kindDef).Distinct();

        public Dialog_AutoSlaughter.AnimalCountRecord CountForType(ThingDef def)
        {
            List<Pawn> pawns = innerContainer.innerList.Where(p => p.kindDef.race == def).ToList();
            int total = pawns.Count;
            int male = pawns.Count(p => p.gender == Gender.Male && p.ageTracker.Adult);
            int maleYoung = pawns.Count(p => p.gender == Gender.Male && !p.ageTracker.Adult);
            int female = pawns.Count(p => p.gender == Gender.Female && p.ageTracker.Adult);
            int femaleYoung = pawns.Count(p => p.gender == Gender.Female && !p.ageTracker.Adult);

            return new Dialog_AutoSlaughter.AnimalCountRecord(total, male, maleYoung, female, femaleYoung, 0, 0);
        }

        public void StorePawn(Pawn pawn, bool despawnFirst = true)
        {
            if(despawnFirst)
                pawn.DeSpawn();

            if (innerContainer.Count >= MaxStoredPawns())
            {
                Messages.Message("PS_StorageFull".Translate(parent.LabelCap, pawn.LabelCap), (Thing) pawn, MessageTypeDefOf.NeutralEvent);

                PawnComponentsUtility.AddComponentsForSpawn(pawn);
                compAssignable?.TryUnassignPawn(pawn);
                GenDrop.TryDropSpawn(pawn, parent.Position, parent.Map, ThingPlaceMode.Near, out Thing _);
                FilthMaker.TryMakeFilth(parent.Position, parent.Map, ThingDefOf.Filth_Slime, new IntRange(3, 6).RandomInRange);
                return;
            }
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
