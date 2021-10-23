using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace PawnStorages
{
    public class JobDriver_ReleasePawnDisc : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed))
            {
                return pawn.Reserve(job.targetC, job, 1, -1, null, errorOnFailed);
            }
            return false;
        }
        public override IEnumerable<Toil> MakeNewToils()
        {
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch);
            yield return Toils_Haul.StartCarryThing(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.C, PathEndMode.InteractionCell);
            Toil toil = Toils_General.Wait(60);
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
                        var pawn = comp.StoredPawns[num];
                        comp.ReleasePawn(pawn, TargetA.Cell, actor.Map);
                    }
                }
                else
                {
                    comp.ReleasePawn(TargetB.Pawn, TargetA.Cell, actor.Map);
                }
            };
            release.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return release;
            yield return Toils_Haul.PlaceCarriedThingInCellFacing(TargetIndex.C);
            yield return new Toil
            {
                initAction = delegate
                {
                    TargetA.Thing.Destroy();
                }
            };
        }
    }
}
