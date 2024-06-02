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

            if (this.parent.IsHashIntervalTick(Props.ticksToFeedAnimals))
            {
                if (storedNutrition > 0)
                {
                    storedNutrition -= compFarmStorage.NutritionRequiredPerDay();
                }
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
