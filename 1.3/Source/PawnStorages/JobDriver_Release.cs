using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace PawnStorages
{
    public class JobDriver_Release : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
        }
        public override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch);
            Toil toil = Toils_General.Wait(60);
            toil.FailOnCannotTouch(TargetIndex.A, PathEndMode.ClosestTouch);
            toil.WithProgressBarToilDelay(TargetIndex.A);
            yield return toil;
            Toil release = new Toil();
            release.initAction = delegate
            {
                Pawn actor = release.actor;
                var comp = TargetA.Thing.TryGetComp<CompPawnStorage>();
                if (TargetB.Thing is null)
                {
                    for (int num = comp.StoredPawns.Count - 1; num >= 0; num--)
                    {
                        comp.ReleasePawn(comp.StoredPawns[num], TargetA.Cell, actor.Map);
                    }
                }
                else
                {
                    comp.ReleasePawn(TargetB.Pawn, TargetA.Cell, actor.Map);
                }

            };
            release.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return release;
        }
    }
}
