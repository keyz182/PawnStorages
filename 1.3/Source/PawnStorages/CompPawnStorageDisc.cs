using Verse;
using Verse.AI;

namespace PawnStorages
{
    public class CompPawnStorageDisc : CompPawnStorage
    {
        public override bool CanRelease(Pawn releaser)
        {
            var bench = GenClosest.ClosestThingReachable(releaser.Position, releaser.Map, ThingRequest.ForDef(PS_DefOf.PS_DigitalBench), PathEndMode.InteractionCell
                , TraverseParms.For(releaser), 9999f, (Thing x) => releaser.CanReserve(x));
            Log.Message("bench: " + bench);
            if (bench != null)
            {
                return true;
            }
            return false;
        }
        public override Job ReleaseJob(Pawn releaser, Pawn toRelease)
        {
            var bench = GenClosest.ClosestThingReachable(releaser.Position, releaser.Map, ThingRequest.ForDef(PS_DefOf.PS_DigitalBench), PathEndMode.InteractionCell, 
                TraverseParms.For(releaser), 9999f, (Thing x) => releaser.CanReserve(x));
            var job = JobMaker.MakeJob(PS_DefOf.PS_ReleasePawnDisc, this.parent, toRelease, bench);
            job.count = 1;
            return job;
        }

        public override Job EnterJob(Pawn enterer)
        {
            var bench = GenClosest.ClosestThingReachable(enterer.Position, enterer.Map, ThingRequest.ForDef(PS_DefOf.PS_DigitalBench), PathEndMode.InteractionCell,
       TraverseParms.For(enterer), 9999f, (Thing x) => enterer.CanReserve(x));
            var job = JobMaker.MakeJob(PS_DefOf.PS_EnterPawnDisc, this.parent, bench);
            job.count = 1;
            return job;
        }
    }
}
