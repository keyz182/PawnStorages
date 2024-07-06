using System;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace PawnStorages.CaptureSphere;

public class Projectile_Capturing : Projectile
{
    public Map ThingMap;
    public ThingWithComps Equipment;
    public Thing HitThing;
    public bool BlockedByShield;
    private int ticksToCapture = -1;
    public ThingWithComps NewlySpawnedBall;
    public CompPawnStorage NewlySpawnedBallStorageComp => NewlySpawnedBall?.GetComp<CompPawnStorage>();
    public CompPawnStorage EquipmentStorageComp => Equipment?.GetComp<CompPawnStorage>();
    public Pawn LauncherPawn => this.launcher as Pawn;

    public EffecterDef EffecterDef;

    public CompPawnStorage ProjectileStorage => GetComp<CompPawnStorage>();
    
    protected Vector3 wiggleAngleFrom = new Vector3(0.0F, 45.0F, 0.0F);
    protected Vector3 wiggleAngleTo = new Vector3(0.0F, -45.0F, 0.0F);
    protected float WiggleFrequency = 4F;
    
    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref this.ticksToCapture, "ticksToDetonation", -1);
        Scribe_Values.Look(ref this.BlockedByShield, "BlockedByShield", false);
        Scribe_References.Look(ref Equipment, "Equipment");
        Scribe_References.Look(ref HitThing, "HitThing");
        Scribe_References.Look(ref NewlySpawnedBall, "NewlySpawnedBall");
        Scribe_Deep.Look(ref ThingMap, "ThingMap");
    }

    public override void Launch(
        Thing launcher,
        Vector3 origin,
        LocalTargetInfo usedTarget,
        LocalTargetInfo intendedTarget,
        ProjectileHitFlags hitFlags,
        bool preventFriendlyFire = false,
        Thing equipment = null,
        ThingDef targetCoverDef = null)
    {
        this.Equipment = (ThingWithComps) equipment;
        base.Launch(launcher,origin,usedTarget, intendedTarget, hitFlags, preventFriendlyFire, equipment, base.targetCoverDef);
    }
    
    public override void DrawAt(Vector3 drawLoc, bool flip = false)
    {
        float height = this.ArcHeightFactor * GenMath.InverseParabola(this.DistanceCoveredFractionArc);
        Vector3 drawLoc1 = drawLoc;
        Vector3 vector3 = drawLoc1 + new Vector3(0.0f, 0.0f, 1f) * height;
        if ((double) this.def.projectile.shadowSize > 0.0)
            this.DrawShadow(drawLoc1, height);
        Quaternion rotation = this.ExactRotation;
        if ((double) this.def.projectile.spinRate != 0.0)
        {
            float num = 60f / this.def.projectile.spinRate;
            rotation = Quaternion.AngleAxis((float) ((double) Find.TickManager.TicksGame % (double) num / (double) num * 360.0), Vector3.up);
        }

        if (ticksToCapture > 0)
        {
            DoWiggle(ref rotation, ref vector3);
        }
        if (this.def.projectile.useGraphicClass)
            this.Graphic.Draw(vector3, this.Rotation, (Thing) this, rotation.eulerAngles.y);
        else
            Graphics.DrawMesh(MeshPool.GridPlane(this.def.graphicData.drawSize), vector3, rotation, this.DrawMat, 0);
        this.Comps_PostDraw();
    }

    public void DoWiggle(ref Quaternion rot, ref Vector3 pos)
    {
        if (ticksToCapture is (<= 30 or > 60) and (<= 90 or > 120) and (<= 150 or > 180)) return;
        
        var from = Quaternion.Euler(this.wiggleAngleFrom);
        var to = Quaternion.Euler(this.wiggleAngleTo);
        
        var val = Mathf.PI * (this.ticksToCapture / 60f) * this.WiggleFrequency;
        var lerp = 0.5F * (1.0F + Mathf.Sin(val));
        var lerpz = 0.5F * (1.0F + Mathf.Sin(val * 2));
            
        rot *=  Quaternion.Lerp(from, to, lerp);
        pos.x += Mathf.Lerp(0.25f, -0.25f, lerp);
        pos.z += Mathf.Lerp(0, -0.05f, lerpz);
    }

    public override void Tick()
    {
        base.Tick();
        if (this.ticksToCapture <= 0)
            return;
        --this.ticksToCapture;
        if (this.ticksToCapture > 0)
            return;
        this.TicksReached();
    }

    public void TicksReached()
    {
        this.ticksToCapture = -1;

        if (Equipment != null)
        {
            var storageComp = Equipment.GetComp<CompPawnStorage>();
            if (storageComp == null)
            {
                storageComp = (CompPawnStorage) Activator.CreateInstance(typeof(CompPawnStorage));
                storageComp.parent = Equipment;
                Equipment.comps.Add(storageComp);
            }

            if ((storageComp.GetDirectlyHeldThings()?.Count ?? 0) <= 0)
            {
                if (!Capture())
                {
                    ProjectileStorage.ReleaseContents(ThingMap);
                }
            }
        }
        
        Destroy();
    }

    public override void Impact(Thing hitThing, bool blockedByShield = false)
    {
        landed = true;
        HitThing = hitThing;
        BlockedByShield = blockedByShield;
        ticksToCapture = 180;
        ThingMap = hitThing?.Map ?? Find.CurrentMap;

        if (Equipment != null)
        {
            var storageComp = Equipment.GetComp<CompPawnStorage>();
            if ((storageComp.GetDirectlyHeldThings()?.Count ?? 0) <= 0)
            {
                if (!PrepCapture())
                {
                    ticksToCapture = -1;
                    Destroy();
                    return;
                }
            }
            else
            {
                Release();
                PS_DefOf.PS_ReleaseSound.PlayOneShot((SoundInfo) new TargetInfo(this.Position, this.Map));
                Equipment.Destroy();
                Destroy();
            }
        }
    }

    public bool PrepCapture()
    {
        if(BlockedByShield) return false;
        
        if (HitThing is not Pawn pawn) return false;
        
        Equipment.Destroy();

        if (pawn.Faction != LauncherPawn?.Faction && pawn.SlaveFaction != LauncherPawn?.Faction && !pawn.IsPrisonerOfColony)
        {
            if (!pawn.Downed) return false;
            pawn.guest?.CapturedBy(LauncherPawn?.Faction, LauncherPawn);
        }
        else
        {
            return !TryAddToNewBall(pawn, out NewlySpawnedBall);
        }
        
        ProjectileStorage.StorePawn(pawn);
        PS_DefOf.PS_CaptureSound.PlayOneShot((SoundInfo) new TargetInfo(this.Position, this.Map));

        return true;
    }

    public void RunEffector()
    {
        if (this.def.projectile.explosionEffect != null)
        {
            Effecter eff = this.def.projectile.explosionEffect.Spawn();
            if (this.def.projectile.explosionEffectLifetimeTicks != 0)
            {
                Map.effecterMaintainer.AddEffecterToMaintain(eff, this.Position.ToVector3().ToIntVec3(), this.def.projectile.explosionEffectLifetimeTicks);
            }
            else
            {
                eff.Trigger(new TargetInfo(this.Position, Map), new TargetInfo(this.Position, Map));
                eff.Cleanup();
            }
        }
    }

    public bool TryAddToNewBall(Pawn pawn, out ThingWithComps ball, CompPawnStorage transferTarget = null)
    {
        RunEffector();
        ball = (ThingWithComps)GenSpawn.Spawn(this.equipmentDef, HitThing.Position, ThingMap, WipeMode.VanishOrMoveAside);
        if (!ball.TryGetComp<CompPawnStorage>(out var comp)) return false;
        if (comp?.CanAssign(pawn, true) ?? false)
        {
            if (transferTarget is not null)
            {
                transferTarget.TransferPawn(comp, pawn);
            }
            else
            {
                comp.StorePawn(pawn);
            }
        
            var hediff = pawn.health.GetOrAddHediff(PS_DefOf.PS_CapturedPawn);
            hediff.visible = false;
        }

        return true;
    }

    public bool Capture()
    {
        if (HitThing is not Pawn pawn) return false;
        return TryAddToNewBall(pawn, out NewlySpawnedBall, ProjectileStorage);
    }

    public void Release()
    {
        var position = HitThing?.Position ?? this.Position;
        
        EquipmentStorageComp?.ReleaseContentsAt(ThingMap, position);
    }
    
}