using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;
using static Unity.Burst.Intrinsics.X86.Avx;

namespace PawnStorages.Farm
{
    [StaticConstructorOnStartup]
    public class CompFarmStorage : CompPawnStorage
    {
        public CompProperties_FarmStorage Props => props as CompProperties_FarmStorage;
        public new bool CanAssign(Pawn pawn, bool couldMakePrisoner=false) =>
            compAssignable != null && pawn.Faction == Faction.OfPlayer &&
             !pawn.RaceProps.Humanlike &&
             (compAssignable.AssignedPawns.Contains(pawn) || compAssignable.HasFreeSlot);

        public float NutritionRequiredPerDay() => compAssignable.AssignedPawns.Sum(animal =>
            SimplifiedPastureNutritionSimulator.NutritionConsumedPerDay(animal.def, animal.ageTracker.CurLifeStage));


        public new void StorePawn(Pawn pawn)
        {
            pawn.DeSpawn();
            storedPawns.Add(pawn);

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
            if (storedPawns.Count > 0)
            {
                yield return new Command_Action
                {
                    defaultLabel = "PS_ReleaseAnimals".Translate(),
                    action = delegate
                    {
                        ReleaseContents(parent.Map);
                    },
                    icon = ContentFinder<Texture2D>.Get("UI/Buttons/PS_Release")
                };
                yield return new Command_Action
                {
                    defaultLabel = "PS_EjectAnimals".Translate(),
                    action = delegate
                    {
                        EjectContents(parent.Map);
                    },
                    icon = ContentFinder<Texture2D>.Get("UI/Buttons/PS_Eject")
                };
            }

            if (DebugSettings.ShowDevGizmos)
            {
                yield return new Command_Action
                {
                    defaultLabel = "Produce",
                    action = delegate { TryProduce(); },
                    icon = ContentFinder<Texture2D>.Get("UI/Buttons/ReleaseAll")
                };
            }

        }

        public bool GetEggLayerProduct(CompEggLayer layer)
        {

        }

        public bool GetProduct(Pawn animal, out ThingDef resource, out int amount)
        {
            resource = null;
            amount = 0; 

            //Milkable or shearable
            var bodyResource = animal.GetComp<CompHasGatherableBodyResource>();
            if (bodyResource is { Active: true })
            {
                resource = bodyResource.ResourceDef;
                amount = bodyResource.ResourceAmount;
                return true;
            }

            //EggLayer 
            var eggLayer = animal.GetComp<CompEggLayer>();
            if (eggLayer is { Active: true })
            {
                resource = eggLayer.Props.eggUnfertilizedDef;
                amount = eggLayer.Props.eggCountRange.RandomInRange;
                return true;
            }

            return false;
        }

        public void TryProduce()
        {
            foreach (var pawn in storedPawns)
            {
                if (!GetProduct(pawn, out var thingDef, out var amount)) continue;
                while (amount > 0)
                {
                    var toSpawn = Mathf.Clamp(amount, 1, thingDef.stackLimit);
                    amount -= toSpawn;
                    var thingStack = ThingMaker.MakeThing(thingDef);
                    thingStack.stackCount = toSpawn;
                    GenPlace.TryPlaceThing(thingStack, parent.Position, parent.Map, ThingPlaceMode.Near);
                }


            }
        }
    }
}
