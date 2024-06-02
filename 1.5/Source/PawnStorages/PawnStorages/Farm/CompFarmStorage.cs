using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;

namespace PawnStorages.Farm
{
    [StaticConstructorOnStartup]
    public class CompFarmStorage : CompPawnStorage
    {
        public new bool CanAssign(Pawn pawn, bool couldMakePrisoner) =>
            compAssignable != null && pawn.Faction == Faction.OfPlayer &&
             !pawn.RaceProps.Humanlike &&
             (compAssignable.AssignedPawns.Contains(pawn) || compAssignable.HasFreeSlot);

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            compAssignable = parent.TryGetComp<CompAssignableToPawn_PawnStorageFarm>();
        }


        public new void StorePawn(Pawn pawn)
        {
            // if (Props.lightEffect) FleckMaker.ThrowLightningGlow(pawn.Position.ToVector3Shifted(), pawn.Map, 0.5f);
            // if (Props.transformEffect) FleckMaker.ThrowExplosionCell(pawn.Position, pawn.Map, FleckDefOf.ExplosionFlash, Color.white);
            //Spawn the store effecter
            // Props.storeEffect?.Spawn(pawn.Position, parent.Map);

            pawn.DeSpawn();
            storedPawns.Add(pawn);

            parent.Map.mapDrawer.MapMeshDirty(parent.Position, MapMeshFlagDefOf.Things);

            if (compAssignable != null && !compAssignable.AssignedPawns.Contains(pawn)) compAssignable.TryAssignPawn(pawn);
            labelDirty = true;
        }

        public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
        {
            foreach (FloatMenuOption floatMenuOption in base.CompFloatMenuOptions(selPawn))
            {
                yield return floatMenuOption;
            }
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            yield break;

        }
    }
}
