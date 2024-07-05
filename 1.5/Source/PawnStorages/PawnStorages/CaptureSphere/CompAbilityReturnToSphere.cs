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
        
        var eff = Props.Effector.Spawn();
        
        var newlySpawnedBall = (ThingWithComps)GenSpawn.Spawn(Props.SphereDef, this.parent.pawn.Position, this.parent.pawn.Map, WipeMode.VanishOrMoveAside);
        var newlySpawnedBallStorageComp = newlySpawnedBall?.GetComp<CompPawnStorage>();
        
        if(newlySpawnedBallStorageComp == null) return;
        
        if(!newlySpawnedBallStorageComp.CanAssign(this.parent.pawn, true)) return;
        
        this.parent.pawn.Map.effecterMaintainer.AddEffecterToMaintain(eff, this.parent.pawn.Position.ToVector3().ToIntVec3(), 600);
        
        PS_DefOf.PS_CaptureSound.PlayOneShot((SoundInfo) new TargetInfo(parent.pawn.Position, parent.pawn.Map));
        newlySpawnedBallStorageComp.StorePawn(this.parent.pawn);
    }
}