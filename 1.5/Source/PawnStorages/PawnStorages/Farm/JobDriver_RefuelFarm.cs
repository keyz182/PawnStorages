using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;

namespace PawnStorages.Farm
{
    public class JobDriver_RefuelFarm : JobDriver
    {

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (pawn.Reserve(job.GetTarget(TargetIndex.A).Thing, job, 1, -1, null, errorOnFailed))
            {
                return pawn.Reserve(job.GetTarget(TargetIndex.B).Thing, job, 1, -1, null, errorOnFailed);
            }
            return false;
        }

        public override IEnumerable<Toil> MakeNewToils()
        {
            var compFarmStorageRefuelable = job.GetTarget(TargetIndex.A).Thing.TryGetComp<CompFarmStorageRefuelable>();

            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            AddEndCondition(() => !compFarmStorageRefuelable.IsFull ? JobCondition.Ongoing : JobCondition.Succeeded);
            AddFailCondition(() => !job.playerForced && !compFarmStorageRefuelable.ShouldAutoRefuelNowIgnoringFuelPct);
            AddFailCondition(() => !compFarmStorageRefuelable.allowAutoRefuel && !job.playerForced);
            yield return Toils_General.DoAtomic(delegate
            {
                job.count = compFarmStorageRefuelable.GetFuelCountToFullyRefuel();
            });
            Toil reserveFuel = Toils_Reserve.Reserve(TargetIndex.B);
            yield return reserveFuel;
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch).FailOnDespawnedNullOrForbidden(TargetIndex.B).FailOnSomeonePhysicallyInteracting(TargetIndex.B);
            yield return Toils_Haul.StartCarryThing(TargetIndex.B, putRemainderInQueue: false, subtractNumTakenFromJobCount: true).FailOnDestroyedNullOrForbidden(TargetIndex.B);
            yield return Toils_Haul.CheckForGetOpportunityDuplicate(reserveFuel, TargetIndex.B, TargetIndex.None, takeFromValidStorage: true);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            yield return Toils_General.Wait(240).FailOnDestroyedNullOrForbidden(TargetIndex.B).FailOnDestroyedNullOrForbidden(TargetIndex.A)
                .FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch)
                .WithProgressBarToilDelay(TargetIndex.A);
            yield return Toils_Refuel.FinalizeRefueling(TargetIndex.A, TargetIndex.B);
        }
    }
}
