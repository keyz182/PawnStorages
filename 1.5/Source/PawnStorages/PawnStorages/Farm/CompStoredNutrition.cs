using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;

namespace PawnStorages.Farm
{
    public class CompStoredNutrition: ThingComp
    {
        public float storedNutrition = 0f;

        private List<IntVec3> cachedAdjCellsCardinal;

        public CompProperties_StoredNutrition Props => props as CompProperties_StoredNutrition;

        public CompFarmStorage compFarmStorage => parent.GetComp<CompFarmStorage>();

        public bool IsWellFed => storedNutrition > 0f;

        public override void PostExposeData()
        {
            Scribe_Values.Look(ref storedNutrition, "storedNutrition");
        }

        public override void CompTick()
        {
            base.CompTick();

            if (this.parent.IsHashIntervalTick(Props.ticksToAbsorbNutrients))
            {
                if (storedNutrition <= Props.maxNutrtition)
                {
                    TryAbsorbNutritionFromHopper(Props.maxNutrtition - storedNutrition);
                }
            }

            if (this.parent.IsHashIntervalTick(Props.animalTickInterval))
            {
                if (storedNutrition > 0)
                {
                    foreach (var pawn in compFarmStorage.StoredPawns)
                    {
                        var feed = true;

                        if (storedNutrition <= 0)
                        {
                            TryAbsorbNutritionFromHopper(Props.maxNutrtition - storedNutrition);
                            // Still no food available, no point trying to feed
                            if (storedNutrition <= 0) feed = false;
                        }

                        if (feed)
                        {
                            var foodNeeds = pawn.needs?.food;
                            if (foodNeeds != null)
                            {
                                if (foodNeeds.TicksUntilHungryWhenFed <= 0)
                                {
                                    var needed = foodNeeds.NutritionWanted;
                                    var available = Mathf.Min(needed, storedNutrition);
                                    storedNutrition -= available;

                                    foodNeeds.CurLevel += available;
                                    pawn.records.AddTo(RecordDefOf.NutritionEaten, available);

                                }
                            }
                        }
                        else
                        {
                            // todo: starvation
                        }

                        if (ThingCompUtility.TryGetComp<CompEggLayer>(pawn, out var compLayer))
                        {
                            EggLayerTick(compLayer, Props.animalTickInterval);
                        }

                        if (ThingCompUtility.TryGetComp<CompHasGatherableBodyResource>(pawn, out var compGatherable))
                        {
                            GatherableTick(compGatherable, Props.animalTickInterval);
                        }

                    }
                    storedNutrition -= compFarmStorage.NutritionRequiredPerDay();
                }
            }
        }

        public void EggLayerTick(CompEggLayer layer, int tickInterval = 1)
        {
            if(!layer.Active) return;
            var eggReadyIncrement = (float)(1f / ((double)layer.Props.eggLayIntervalDays * 60000f));
            eggReadyIncrement *= PawnUtility.BodyResourceGrowthSpeed(layer.parent as Pawn);
            // we're not doing this every tick so bump the progress
            eggReadyIncrement *= tickInterval;
            layer.eggProgress += eggReadyIncrement;
            layer.eggProgress = Mathf.Clamp(layer.eggProgress, 0f, 1f);

            if(!layer.ProgressStoppedBecauseUnfertilized)
                return;
            layer.eggProgress = layer.Props.eggProgressUnfertilizedMax;

            if (layer.eggProgress >= 1f)
            {
                var thing = layer.ProduceEgg();
                if (thing != null)
                {
                    GenPlace.TryPlaceThing(thing, this.parent.Position, this.parent.Map, ThingPlaceMode.Near);
                }
            }
        }

        public void GatherableTick(CompHasGatherableBodyResource gatherable, int tickInterval = 1)
        {
            if(!gatherable.Active) return;
            var eggReadyIncrement = (float)(1f / ((double)gatherable.GatherResourcesIntervalDays * 60000f));
            eggReadyIncrement *= PawnUtility.BodyResourceGrowthSpeed(gatherable.parent as Pawn);
            // we're not doing this every tick so bump the progress
            eggReadyIncrement *= tickInterval;
            gatherable.fullness += eggReadyIncrement;
            gatherable.fullness = Mathf.Clamp(gatherable.fullness, 0f, 1f);

            if (gatherable.ActiveAndFull)
            {
                var amountToGenerate = GenMath.RoundRandom((float)gatherable.ResourceAmount * gatherable.fullness);
                while (amountToGenerate > 0f)
                {
                    var generateThisLoop = Mathf.Clamp(amountToGenerate, 1, gatherable.ResourceDef.stackLimit);
                    amountToGenerate -= generateThisLoop;
                    var thing = ThingMaker.MakeThing(gatherable.ResourceDef);
                    thing.stackCount = generateThisLoop;
                    GenPlace.TryPlaceThing(thing, this.parent.Position, this.parent.Map, ThingPlaceMode.Near);
                }

                gatherable.fullness = 0f;
            }
        }

        public List<IntVec3> AdjCellsCardinalInBounds
        {
            get
            {
                if (this.cachedAdjCellsCardinal == null)
                    this.cachedAdjCellsCardinal = GenAdj.CellsAdjacentCardinal((Thing)this.parent).Where<IntVec3>((Func<IntVec3, bool>)(c => c.InBounds(this.parent.Map))).ToList<IntVec3>();
                return this.cachedAdjCellsCardinal;
            }
        }

        public bool TryAbsorbNutritionFromHopper(float nutrition)
        {
            if (nutrition <= 0) return false;
            if (!this.HasEnoughFeedstockInHoppers()) return false;

            Thing feedInAnyHopper = this.FindFeedInAnyHopper();
            if (feedInAnyHopper == null)
            {
                return false;
            }
            int count = Mathf.Min(feedInAnyHopper.stackCount, Mathf.CeilToInt(nutrition / feedInAnyHopper.GetStatValue(StatDefOf.Nutrition)));
            storedNutrition += (float)count * feedInAnyHopper.GetStatValue(StatDefOf.Nutrition);

            feedInAnyHopper.SplitOff(count);

            return true;
        }
        
        public virtual Thing FindFeedInAnyHopper()
        {
            for (int index1 = 0; index1 < this.AdjCellsCardinalInBounds.Count; ++index1)
            {
                Thing feedInAnyHopper = (Thing)null;
                Thing thing1 = (Thing)null;
                List<Thing> thingList = this.AdjCellsCardinalInBounds[index1].GetThingList(this.parent.Map);
                for (int index2 = 0; index2 < thingList.Count; ++index2)
                {
                    Thing thing2 = thingList[index2];
                    if (Building_NutrientPasteDispenser.IsAcceptableFeedstock(thing2.def))
                        feedInAnyHopper = thing2;
                    if (thing2.IsHopper())
                        thing1 = thing2;
                }
                if (feedInAnyHopper != null && thing1 != null)
                    return feedInAnyHopper;
            }
            return (Thing)null;
        }

        public virtual bool HasEnoughFeedstockInHoppers()
        {
            float num = 0.0f;
            for (int index1 = 0; index1 < this.AdjCellsCardinalInBounds.Count; ++index1)
            {
                IntVec3 cellsCardinalInBound = this.AdjCellsCardinalInBounds[index1];
                Thing thing1 = (Thing)null;
                Thing thing2 = (Thing)null;
                Map map = this.parent.Map;
                List<Thing> thingList = cellsCardinalInBound.GetThingList(map);
                for (int index2 = 0; index2 < thingList.Count; ++index2)
                {
                    Thing thing3 = thingList[index2];
                    if (Building_NutrientPasteDispenser.IsAcceptableFeedstock(thing3.def))
                        thing1 = thing3;
                    if (thing3.IsHopper())
                        thing2 = thing3;
                }
                if (thing1 != null && thing2 != null)
                    num += (float)thing1.stackCount * thing1.GetStatValue(StatDefOf.Nutrition);
                if ((double)num >= (double)this.parent.def.building.nutritionCostPerDispense)
                    return true;
            }
            return false;
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {

            if (DebugSettings.ShowDevGizmos)
            {
                yield return new Command_Action
                {
                    defaultLabel = "Fill Nutrition",
                    action = delegate { storedNutrition = Props.maxNutrtition; },
                    icon = ContentFinder<Texture2D>.Get("UI/Buttons/ReleaseAll")
                };
                yield return new Command_Action
                {
                    defaultLabel = "Absorb Nutrition from Hopper",
                    action = delegate { TryAbsorbNutritionFromHopper(Props.maxNutrtition - storedNutrition); },
                    icon = ContentFinder<Texture2D>.Get("UI/Buttons/ReleaseAll")
                };
                yield return new Command_Action
                {
                    defaultLabel = "Consume Nutrition",
                    action = delegate { storedNutrition -= compFarmStorage.NutritionRequiredPerDay(); },
                    icon = ContentFinder<Texture2D>.Get("UI/Buttons/ReleaseAll")
                };
            }
        }
    }
}
