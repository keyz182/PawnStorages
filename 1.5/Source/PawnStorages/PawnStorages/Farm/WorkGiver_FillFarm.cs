using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse.AI;
using Verse;
using Verse.Noise;
using UnityEngine.Networking.Types;

namespace PawnStorages.Farm
{
    public class WorkGiver_FillFarm : WorkGiver_Scanner
    {

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            return pawn.Map.GetComponent<PS_Farm_MapComponent>().comps.Select(x => x.parent);
        }

        public override PathEndMode PathEndMode
        {
            get
            {
                return PathEndMode.Touch;
            }
        }

        public virtual bool CanRefuelThing(Thing t)
        {
            return !(t is Building_Turret);
        }

        public Thing GetBestFuel(Pawn pawn, ThingFilter filter) => GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, filter.BestThingRequest,
                PathEndMode.ClosestTouch, TraverseParms.For(pawn), 9999f,
                delegate (Thing x)
                {
                    if (x.IsForbidden(pawn) || !pawn.CanReserve(x)) return false;
                    return filter.Allows(x);
                });

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            var compFarmStorageRefuelable = t.TryGetComp<CompFarmStorageRefuelable>();

            if (compFarmStorageRefuelable == null || compFarmStorageRefuelable.IsFull || t.IsForbidden(pawn) ||
                !pawn.CanReserve(t, 1, -1, null, forced) || t.Faction != pawn.Faction)
            {
                return false;
            }

            var filter = compFarmStorageRefuelable.GetStoreSettings().filter;

            var fuel = GetBestFuel(pawn, filter);

            if (fuel == null)
            {
                JobFailReason.Is("NoFuelToRefuel".Translate(filter.Summary));
                return false;
            }
            return true;

        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            var compFarmStorageRefuelable = t.TryGetComp<CompFarmStorageRefuelable>();
            var filter = compFarmStorageRefuelable.GetStoreSettings().filter;

            if (!compFarmStorageRefuelable.Props.atomicFueling)
            {
                var fuel = GetBestFuel(pawn, filter);
                return JobMaker.MakeJob(PS_DefOf.PS_RefuelFarm, t, fuel);
            }

            var fuelToFul = compFarmStorageRefuelable.GetFuelCountToFullyRefuel();
            var fuels = FindEnoughReservableThings(pawn, compFarmStorageRefuelable.parent.Position,
                new IntRange(fuelToFul, fuelToFul), t => filter.Allows(t));


            Job job = JobMaker.MakeJob(PS_DefOf.PS_RefuelFarm_Atomic, t);
            job.targetQueueB = fuels.Select((Thing f) => new LocalTargetInfo(f)).ToList();
            return job;
        }

        public static List<Thing> FindEnoughReservableThings(Pawn pawn, IntVec3 rootCell, IntRange desiredQuantity, Predicate<Thing> validThing)
        {
            Predicate<Thing> validator = delegate (Thing x)
            {
                if (x.IsForbidden(pawn) || !pawn.CanReserve(x))
                {
                    return false;
                }
                return validThing(x) ? true : false;
            };
            Region region2 = rootCell.GetRegion(pawn.Map);
            TraverseParms traverseParams = TraverseParms.For(pawn);
            RegionEntryPredicate entryCondition = (Region from, Region r) => r.Allows(traverseParams, isDestination: false);
            List<Thing> chosenThings = new List<Thing>();
            int accumulatedQuantity = 0;
            ThingListProcessor(rootCell.GetThingList(region2.Map), region2);
            if (accumulatedQuantity < desiredQuantity.max)
            {
                RegionTraverser.BreadthFirstTraverse(region2, entryCondition, RegionProcessor, 99999);
            }
            if (accumulatedQuantity >= desiredQuantity.min)
            {
                return chosenThings;
            }
            return null;
            bool RegionProcessor(Region r)
            {
                List<Thing> things2 = r.ListerThings.ThingsMatching(ThingRequest.ForGroup(ThingRequestGroup.HaulableEver));
                return ThingListProcessor(things2, r);
            }
            bool ThingListProcessor(List<Thing> things, Region region)
            {
                for (int i = 0; i < things.Count; i++)
                {
                    Thing thing = things[i];
                    if (validator(thing) && !chosenThings.Contains(thing) && ReachabilityWithinRegion.ThingFromRegionListerReachable(thing, region, PathEndMode.ClosestTouch, pawn))
                    {
                        chosenThings.Add(thing);
                        accumulatedQuantity += thing.stackCount;
                        if (accumulatedQuantity >= desiredQuantity.max)
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
        }
    }
}
