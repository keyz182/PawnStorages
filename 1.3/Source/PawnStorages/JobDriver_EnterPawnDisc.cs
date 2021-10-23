using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace PawnStorages
{
    public class JobDriver_EnterPawnDisc : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed))
            {
                return pawn.Reserve(job.targetB, job, 1, -1, null, errorOnFailed);
            }
            return false;
        }
        public override IEnumerable<Toil> MakeNewToils()
        {
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch);
            yield return Toils_Haul.StartCarryThing(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.InteractionCell);
            Toil toil = Toils_General.Wait(60);
            toil.WithProgressBarToilDelay(TargetIndex.B);
            yield return toil;
            Toil release = new Toil();
            release.initAction = delegate
            {
                Pawn actor = release.actor;
                var comp = TargetA.Thing.TryGetComp<CompPawnStorage>();
                comp.StorePawn(pawn);
            };
            release.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return release;
        }
    }
}
