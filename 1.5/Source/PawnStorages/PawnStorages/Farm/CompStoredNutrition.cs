using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace PawnStorages.Farm
{
    public class CompStoredNutrition: ThingComp
    {
        public float storedNutrition = 0f;

        private List<IntVec3> cachedAdjCellsCardinal;

        protected List<Thing> daysProduce = new List<Thing>();

        public CompPowerTrader compPowerTrader => parent.TryGetComp<CompPowerTrader>();

        public CompProperties_StoredNutrition Props => props as CompProperties_StoredNutrition;

        public CompFarmStorage compFarmStorage => parent.GetComp<CompFarmStorage>();

        // returns true if we disable power consumption
        public bool IsPowered => compPowerTrader == null || compPowerTrader.ShouldBeLitNow();

        public bool IsWellFed => storedNutrition > 0f;

        public override void PostExposeData()
        {
            Scribe_Values.Look(ref storedNutrition, "storedNutrition");
        }

        public override void CompTick()
        {
            base.CompTick();

            if(!compFarmStorage.Props.needsDrop) return;

            if (this.parent.IsHashIntervalTick(60000) && daysProduce.Count > 0 && IsPowered)
            {
                foreach (var thing in daysProduce)
                {
                    GenPlace.TryPlaceThing(thing, this.parent.Position, this.parent.Map, ThingPlaceMode.Near);
                }
                daysProduce.Clear();
            }

            var pawnsToRemove = new List<Pawn>();

            if (this.parent.IsHashIntervalTick(Props.ticksToAbsorbNutrients) && IsPowered)
            {
                if (storedNutrition <= Props.maxNutrtition)
                {
                    TryAbsorbNutritionFromHopper(Props.maxNutrtition - storedNutrition);
                }
            }

            if (this.parent.IsHashIntervalTick(Props.animalTickInterval))
            {
                foreach (var pawn in compFarmStorage.StoredPawns)
                {
                    //Need fall ticker
                    var foodNeeds = pawn.needs?.food;
                    if (foodNeeds != null)
                    {
                        foodNeeds.CurLevel -= foodNeeds.FoodFallPerTick * Props.animalTickInterval;
                        if (!foodNeeds.Starving)
                            foodNeeds.lastNonStarvingTick = Find.TickManager.TicksGame;

                        // Need_Food.NeedInterval hardcodes 150 ticks, so adjust
                        var adjustedMalnutritionSeverityPerInterval =
                            (foodNeeds.MalnutritionSeverityPerInterval / 150f) * Props.animalTickInterval;

                        if (foodNeeds.Starving)
                            HealthUtility.AdjustSeverity(pawn, HediffDefOf.Malnutrition, adjustedMalnutritionSeverityPerInterval);
                        else
                            HealthUtility.AdjustSeverity(pawn, HediffDefOf.Malnutrition, -adjustedMalnutritionSeverityPerInterval);
                    }

                    foreach (var hediff in pawn.health.hediffSet.hediffs)
                    {
                        if (hediff.def == HediffDefOf.Malnutrition)
                        {
                            if (hediff.Severity >= 0.75f)
                            {
                                // hediff.DoMTBDeath();
                                compFarmStorage.ReleaseSingle(this.parent.Map, pawn, false);
                                pawnsToRemove.Add(pawn);
                                SendStavingLetter(pawn);
                                continue;
                            }
                            break;

                        }
                    }

                    // if not powered
                    if (!IsPowered) continue;
                    
                    //Hopper absorbtion ticker
                    if (storedNutrition <= 0)
                    {
                        TryAbsorbNutritionFromHopper(Props.maxNutrtition - storedNutrition);
                        // Still no food available, no point trying to feed
                        if (storedNutrition <= 0) continue;
                    }

                    if (foodNeeds != null)
                    {
                        if (foodNeeds.TicksUntilHungryWhenFed <= 0)
                        {
                            var available = Mathf.Min(foodNeeds.NutritionWanted, storedNutrition);
                            storedNutrition -= available;
                             
                            foodNeeds.CurLevel += available;
                            pawn.records.AddTo(RecordDefOf.NutritionEaten, available);

                        }
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
            }

            compFarmStorage.StoredPawns.RemoveAll((p) => pawnsToRemove.Contains(p));
        }

        public void SendStavingLetter(Pawn pawn)
        {
            LookTargets targets = new LookTargets(pawn);
            ChoiceLetter letter = LetterMaker.MakeLetter(
                "PS_PawnEjectedStarvationTitle".Translate(pawn.LabelShort),
                "PS_PawnEjectedStarvation".Translate(pawn.LabelShort, this.parent.LabelShort),
                LetterDefOf.NegativeEvent,
                targets,
                null,
                null,
                null
                );
            Find.LetterStack.ReceiveLetter(letter, null);
        }

        public void EggLayerTick(CompEggLayer layer, int tickInterval = 1)
        {
            if(!layer.Active) return;
            var eggReadyIncrement = (float)(1f / ((double)layer.Props.eggLayIntervalDays * 60000f));
            eggReadyIncrement *= PawnUtility.BodyResourceGrowthSpeed(layer.parent as Pawn);
            // we're not doing this every tick so bump the progress
            eggReadyIncrement *= tickInterval;
            eggReadyIncrement *= Props.produceTimeScale;
            layer.eggProgress += eggReadyIncrement;
            layer.eggProgress = Mathf.Clamp(layer.eggProgress, 0f, 1f);

            if(!layer.ProgressStoppedBecauseUnfertilized)
                return;
            layer.eggProgress = layer.Props.eggProgressUnfertilizedMax;

            if (!(layer.eggProgress >= 1f)) return;
            var thing = layer.ProduceEgg();
            if (thing != null)
            {
                daysProduce.Add(thing);
                // GenPlace.TryPlaceThing(thing, this.parent.Position, this.parent.Map, ThingPlaceMode.Near);
            }
        }

        public void GatherableTick(CompHasGatherableBodyResource gatherable, int tickInterval = 1)
        {
            if(!gatherable.Active) return;
            var gatherableReadyIncrement = (float)(1f / ((double)gatherable.GatherResourcesIntervalDays * 60000f)); 
            gatherableReadyIncrement *= PawnUtility.BodyResourceGrowthSpeed(gatherable.parent as Pawn);
            // we're not doing this every tick so bump the progress
            gatherableReadyIncrement *= tickInterval;
            gatherableReadyIncrement *= Props.produceTimeScale;
            gatherable.fullness += gatherableReadyIncrement;
            gatherable.fullness = Mathf.Clamp(gatherable.fullness, 0f, 1f);

            if (!gatherable.ActiveAndFull) return;
            var amountToGenerate = GenMath.RoundRandom((float)gatherable.ResourceAmount * gatherable.fullness);
            while (amountToGenerate > 0f)
            {
                var generateThisLoop = Mathf.Clamp(amountToGenerate, 1, gatherable.ResourceDef.stackLimit);
                amountToGenerate -= generateThisLoop;
                var thing = ThingMaker.MakeThing(gatherable.ResourceDef);
                thing.stackCount = generateThisLoop;
                daysProduce.Add(thing);
                // GenPlace.TryPlaceThing(thing, this.parent.Position, this.parent.Map, ThingPlaceMode.Near);
            }

            gatherable.fullness = 0f;
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
                    defaultLabel = "Empty Nutrition",
                    action = delegate { storedNutrition = 0; },
                    icon = ContentFinder<Texture2D>.Get("UI/Buttons/ReleaseAll")
                };
                yield return new Command_Action
                {
                    defaultLabel = "Absorb Nutrition from Hopper",
                    action = delegate { TryAbsorbNutritionFromHopper(Props.maxNutrtition - storedNutrition); },
                    icon = ContentFinder<Texture2D>.Get("UI/Buttons/ReleaseAll")
                };
            }
        }
    }
}
