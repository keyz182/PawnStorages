using RimWorld;
using Verse;
using Verse.Sound;

namespace PawnStorages.CaptureSphere;

public class CompAbilityReturnToSphere : CompAbilityEffect
{
    public new CompProperties_AbilityReturnToSphere Props => (CompProperties_AbilityReturnToSphere) props;
    
    public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
    {
        base.Apply(target, dest);

        ThingWithComps newlySpawnedBall = (ThingWithComps)GenSpawn.Spawn(Props.SphereDef, parent.pawn.Position, parent.pawn.Map, WipeMode.VanishOrMoveAside);
        CompPawnStorage newlySpawnedBallStorageComp = newlySpawnedBall?.GetComp<CompPawnStorage>();
        
        if(newlySpawnedBallStorageComp == null) return;
        
        if(!newlySpawnedBallStorageComp.CanAssign(parent.pawn, true)) return;
        

        if (Props.Effector != null)
        {
            Effecter eff = Props.Effector.Spawn();
            parent.pawn.Map.effecterMaintainer.AddEffecterToMaintain(eff, parent.pawn.Position.ToVector3().ToIntVec3(), 600);
        }
        
        PS_DefOf.PS_CaptureSound.PlayOneShot(new TargetInfo(parent.pawn.Position, parent.pawn.Map));
        newlySpawnedBallStorageComp.StorePawn(parent.pawn);
    }
}