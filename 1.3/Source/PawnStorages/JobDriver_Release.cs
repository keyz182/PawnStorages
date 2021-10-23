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
                    for (int num = comp.storedPawns.Count - 1; num >= 0; num--)
                    {
                        var pawn = comp.storedPawns[num];
                        comp.storedPawns.Remove(pawn);
                        GenSpawn.Spawn(pawn, TargetA.Cell, actor.Map);
                    }
                }
                else
                {
                    var pawn = TargetB.Pawn;
                    comp.storedPawns.Remove(pawn);
                    GenSpawn.Spawn(pawn, TargetA.Cell, actor.Map);
                }

            };
            release.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return release;
        }
    }
}
