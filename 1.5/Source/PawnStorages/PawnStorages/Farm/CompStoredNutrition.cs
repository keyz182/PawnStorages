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
        public bool produceNow = false;

        private List<IntVec3> cachedAdjCellsCardinal;

        protected List<Thing> daysProduce = new();

        public CompPowerTrader compPowerTrader => parent.TryGetComp<CompPowerTrader>();

        public CompProperties_StoredNutrition Props => props as CompProperties_StoredNutrition;

        public CompFarmStorage compFarmStorage => parent.GetComp<CompFarmStorage>();

        public bool NeedsDrop => PawnStoragesMod.settings.AllowNeedsDrop || compFarmStorage.Props.needsDrop;

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

            if(!NeedsDrop) return;

            var pawnsToRemove = new List<Pawn>();

            if (parent.IsHashIntervalTick(Props.ticksToAbsorbNutrients) && IsPowered)
            {
                if (storedNutrition <= Props.maxNutrition)
                {
                    TryAbsorbNutritionFromHopper(Props.maxNutrition - storedNutrition);
                }
            }

            if (parent.IsHashIntervalTick(Props.animalTickInterval))
            {
                foreach (Pawn pawn in compFarmStorage.StoredPawns)
                {
                    //Need fall ticker
                    Need_Food foodNeeds = pawn.needs?.food;
                    if (foodNeeds != null)
                    {
                        foodNeeds.CurLevel -= foodNeeds.FoodFallPerTick * Props.animalTickInterval;
                        if (!foodNeeds.Starving)
                            foodNeeds.lastNonStarvingTick = Find.TickManager.TicksGame;

                        // Need_Food.NeedInterval hardcodes 150 ticks, so adjust
                        float adjustedMalnutritionSeverityPerInterval =
                            (foodNeeds.MalnutritionSeverityPerInterval / 150f) * Props.animalTickInterval;

                        if (foodNeeds.Starving)
                            HealthUtility.AdjustSeverity(pawn, HediffDefOf.Malnutrition, adjustedMalnutritionSeverityPerInterval);
                        else
                            HealthUtility.AdjustSeverity(pawn, HediffDefOf.Malnutrition, -adjustedMalnutritionSeverityPerInterval);
                    }

                    if (pawn.health.hediffSet.TryGetHediff(HediffDefOf.Malnutrition, out Hediff malnutritionHediff) && malnutritionHediff.Severity >= 0.75f)
                    {
                        compFarmStorage.ReleaseSingle(parent.Map, pawn, false, true);
                        pawnsToRemove.Add(pawn);
                        SendStavingLetter(pawn);
                        continue;
                    }

                    // if not powered
                    if (!IsPowered) continue;
                    
                    //Hopper absorption ticker
                    if (storedNutrition <= 0)
                    {
                        TryAbsorbNutritionFromHopper(Props.maxNutrition - storedNutrition);
                        // Still no food available, no point trying to feed
                        if (storedNutrition <= 0) continue;
                    }

                    if (foodNeeds is { TicksUntilHungryWhenFed: <= 0 })
                    {
                        float available = Mathf.Min(foodNeeds.NutritionWanted, storedNutrition);
                        storedNutrition -= available;
                             
                        foodNeeds.CurLevel += available;
                        pawn.records.AddTo(RecordDefOf.NutritionEaten, available);
                    }

                    if (pawn.TryGetComp(out CompEggLayer compLayer) && pawn.gender != Gender.Male)
                    {
                        EggLayerTick(compLayer, Props.animalTickInterval);
                    }

                    if (pawn.TryGetComp(out CompHasGatherableBodyResource compGatherable) && pawn.gender != Gender.Male)
                    {
                        GatherableTick(compGatherable, Props.animalTickInterval);
                    }

                }
            }

            compFarmStorage.StoredPawns.RemoveAll((p) => pawnsToRemove.Contains(p));

            if (!produceNow && (!parent.IsHashIntervalTick(60000) || daysProduce.Count <= 0 || !IsPowered)) return;
            foreach (Thing thing in daysProduce)
            {
                GenPlace.TryPlaceThing(thing, parent.Position, parent.Map, ThingPlaceMode.Near);
            }
            daysProduce.Clear();
        }

        public void SendStavingLetter(Pawn pawn)
        {
            LookTargets targets = new(pawn);
            ChoiceLetter letter = LetterMaker.MakeLetter(
                "PS_PawnEjectedStarvationTitle".Translate(pawn.LabelShort),
                "PS_PawnEjectedStarvation".Translate(pawn.LabelShort, parent.LabelShort),
                LetterDefOf.NegativeEvent,
                targets
                );
            Find.LetterStack.ReceiveLetter(letter);
        }

        public void EggLayerTick(CompEggLayer layer, int tickInterval = 1)
        {
            if(!layer.Active) return;
            float eggReadyIncrement = (float)(1f / ((double)layer.Props.eggLayIntervalDays * 60000f));
            eggReadyIncrement *= PawnUtility.BodyResourceGrowthSpeed(layer.parent as Pawn);
            // we're not doing this every tick so bump the progress
            eggReadyIncrement *= tickInterval;
            eggReadyIncrement *= Props.produceTimeScale;
            layer.eggProgress += eggReadyIncrement;
            layer.eggProgress = Mathf.Clamp(layer.eggProgress, 0f, 1f);
            
            if (!(layer.eggProgress >= 1f)) return;
            Thing thing = layer.ProduceEgg();
            if (thing != null)
            {
                daysProduce.Add(thing);
            }
        }

        public void GatherableTick(CompHasGatherableBodyResource gatherable, int tickInterval = 1)
        {
            if(!gatherable.Active) return;
            float gatherableReadyIncrement = (float)(1f / ((double)gatherable.GatherResourcesIntervalDays * 60000f));
            gatherableReadyIncrement *= PawnUtility.BodyResourceGrowthSpeed(gatherable.parent as Pawn);
            // we're not doing this every tick so bump the progress
            gatherableReadyIncrement *= tickInterval;
            gatherableReadyIncrement *= Props.produceTimeScale;
            gatherable.fullness += gatherableReadyIncrement;
            gatherable.fullness = Mathf.Clamp(gatherable.fullness, 0f, 1f);

            if (!gatherable.ActiveAndFull) return;
            int amountToGenerate = GenMath.RoundRandom(gatherable.ResourceAmount * gatherable.fullness);
            while (amountToGenerate > 0f)
            {
                int generateThisLoop = Mathf.Clamp(amountToGenerate, 1, gatherable.ResourceDef.stackLimit);
                amountToGenerate -= generateThisLoop;
                Thing thing = ThingMaker.MakeThing(gatherable.ResourceDef);
                thing.stackCount = generateThisLoop;
                daysProduce.Add(thing);
            }

            gatherable.fullness = 0f;
        }

        public List<IntVec3> AdjCellsCardinalInBounds =>
            cachedAdjCellsCardinal ??= GenAdj.CellsAdjacentCardinal(parent)
                .Where(c => c.InBounds(parent.Map))
                .ToList();

        public bool TryAbsorbNutritionFromHopper(float nutrition)
        {
            if (nutrition <= 0) return false;
            if (!HasEnoughFeedstockInHoppers()) return false;

            Thing feedInAnyHopper = FindFeedInAnyHopper();
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
            for (int index1 = 0; index1 < AdjCellsCardinalInBounds.Count; ++index1)
            {
                Thing feedInAnyHopper = null;
                Thing hopper = null;
                List<Thing> thingList = AdjCellsCardinalInBounds[index1].GetThingList(parent.Map);
                foreach (Thing maybeHopper in thingList)
                {
                    if (Building_NutrientPasteDispenser.IsAcceptableFeedstock(maybeHopper.def))
                        feedInAnyHopper = maybeHopper;
                    if (maybeHopper.IsHopper())
                        hopper = maybeHopper;
                }
                if (feedInAnyHopper != null && hopper != null)
                    return feedInAnyHopper;
            }
            return null;
        }

        public virtual bool HasEnoughFeedstockInHoppers()
        {
            float num = 0.0f;
            foreach (IntVec3 cellsCardinalInBound in AdjCellsCardinalInBounds)
            {
                Thing feedStockThing = null;
                Thing hopper = null;
                Map map = parent.Map;
                List<Thing> potentialFeedStockThings = cellsCardinalInBound.GetThingList(map);
                foreach (Thing potentialFeedStockThing in potentialFeedStockThings)
                {
                    if (Building_NutrientPasteDispenser.IsAcceptableFeedstock(potentialFeedStockThing.def))
                        feedStockThing = potentialFeedStockThing;
                    if (potentialFeedStockThing.IsHopper())
                        hopper = potentialFeedStockThing;
                }
                if (feedStockThing != null && hopper != null)
                    num += feedStockThing.stackCount * feedStockThing.GetStatValue(StatDefOf.Nutrition);
                if (num >= (double)parent.def.building.nutritionCostPerDispense)
                    return true;
            }
            return false;
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (!DebugSettings.ShowDevGizmos) yield break;
            yield return new Command_Action
            {
                defaultLabel = "Fill Nutrition",
                action = delegate { storedNutrition = Props.maxNutrition; },
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
                action = delegate { TryAbsorbNutritionFromHopper(Props.maxNutrition - storedNutrition); },
                icon = ContentFinder<Texture2D>.Get("UI/Buttons/ReleaseAll")
            };
            yield return new Command_Action
            {
                defaultLabel = "Make all animals ready to produce",
                action = delegate
                {
                    foreach (Pawn storedPawn in compFarmStorage.StoredPawns)
                    {
                        storedPawn.needs.food.CurLevel = storedPawn.needs.food.MaxLevel;

                        if (storedPawn.TryGetComp(out CompEggLayer compLayer))
                        {
                            compLayer.eggProgress = 1f;
                        }

                        if (storedPawn.TryGetComp(out CompHasGatherableBodyResource compGatherable))
                        {
                            compGatherable.fullness = 1f;
                        }

                    }
                },
                icon = ContentFinder<Texture2D>.Get("UI/Buttons/ReleaseAll")
            };
            yield return new Command_Action
            {
                defaultLabel = "Produce on next tick",
                action = delegate
                {
                    produceNow = true;
                },
                icon = ContentFinder<Texture2D>.Get("UI/Buttons/ReleaseAll")
            };
        }
    }
}
