using RimWorld;
using UnityEngine;
using Verse;

namespace PawnStorages;

public class Projectile_Capturing : Projectile
{
    public Map ThingMap;
    public ThingWithComps Equipment;
    public Thing HitThing;
    public bool BlockedByShield;
    private int ticksToCapture = -1;
    public ThingWithComps NewlySpawnedBall;
    public CompPawnStorage NewlySpawnedBallStorageComp => NewlySpawnedBall?.GetComp<CompPawnStorage>();
    public Pawn LauncherPawn => this.launcher as Pawn;

    public CompPawnStorage ProjectileStorage => GetComp<CompPawnStorage>();
    
    
    [SerializeField] protected Vector3 m_from = new Vector3(0.0F, 45.0F, 0.0F);
    [SerializeField] protected Vector3 m_to = new Vector3(0.0F, -45.0F, 0.0F);
    [SerializeField] protected float m_frequency = 4F;
    
    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref this.ticksToCapture, "ticksToDetonation", -1);
        Scribe_Values.Look(ref this.BlockedByShield, "BlockedByShield", false);
        Scribe_References.Look(ref Equipment, "Equipment");
        Scribe_References.Look(ref HitThing, "HitThing");
        Scribe_References.Look(ref HitThing, "HitThing");
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
            DoRotate(ref rotation);
        }
        if (this.def.projectile.useGraphicClass)
            this.Graphic.Draw(vector3, this.Rotation, (Thing) this, rotation.eulerAngles.y);
        else
            Graphics.DrawMesh(MeshPool.GridPlane(this.def.graphicData.drawSize), vector3, rotation, this.DrawMat, 0);
        this.Comps_PostDraw();
    }

    public void DoRotate(ref Quaternion rot)
    {
        if ((ticksToCapture > 30 && ticksToCapture <= 60) || (ticksToCapture > 90 && ticksToCapture <= 120) ||
            (ticksToCapture > 150 && ticksToCapture <= 180))
        {
            Quaternion from = Quaternion.Euler(this.m_from);
            Quaternion to = Quaternion.Euler(this.m_to);

            float lerp = 0.5F * (1.0F + Mathf.Sin(Mathf.PI * Time.realtimeSinceStartup * this.m_frequency));
            var newRot = Quaternion.Lerp(from, to, lerp);
            rot *= newRot;
        }
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
            if ((storageComp.GetDirectlyHeldThings()?.Count ?? 0) > 0)
            {
                Release();
            }
            else
            {
                if (!Capture())
                {
                    ProjectileStorage.ReleaseContents(ThingMap);
                }
            }
        }
        
        this.Destroy(DestroyMode.Vanish);
    }

    public override void Impact(Thing hitThing, bool blockedByShield = false)
    {
        landed = true;
        HitThing = hitThing;
        BlockedByShield = blockedByShield;
        ticksToCapture = 180;
        ThingMap = hitThing.Map;

        if (Equipment != null)
        {
            var storageComp = Equipment.GetComp<CompPawnStorage>();
            if ((storageComp.GetDirectlyHeldThings()?.Count ?? 0) <= 0)
            {
                if (!PrepCapture())
                {
                    ticksToCapture = -1;
                    this.Destroy(DestroyMode.Vanish);
                }
            }
        }
    }

    public bool PrepCapture()
    {
        if(BlockedByShield) return false;
        
        if (HitThing is not Pawn pawn) return false;

        if (pawn.Faction != LauncherPawn?.Faction)
        {
            if (!pawn.Downed) return false;
            pawn.guest?.CapturedBy(LauncherPawn?.Faction, LauncherPawn);
        }
        
        Equipment.Destroy();
        
        ProjectileStorage.StorePawn(pawn);

        return true;
    }

    public bool Capture()
    {
        if (HitThing is not Pawn pawn) return false;
        

        NewlySpawnedBall = (ThingWithComps)GenSpawn.Spawn(this.equipmentDef, HitThing.Position, ThingMap, WipeMode.VanishOrMoveAside);
        
        if(NewlySpawnedBallStorageComp == null) return false;
        
        if(!NewlySpawnedBallStorageComp.CanAssign(pawn, true)) return false;
        
        if(NewlySpawnedBallStorageComp == null) return false;
        ProjectileStorage.TransferPawn(NewlySpawnedBallStorageComp, pawn);
        return true;
    }

    public void Release()
    {
        var position = HitThing?.Position ?? this.Position;
        var map = ThingMap ?? Find.CurrentMap;
        
        NewlySpawnedBallStorageComp.ReleaseContentsAt(map, position);
        var thing = (ThingWithComps)GenSpawn.Spawn(this.equipmentDef, position, map);
        Equipment.Destroy();
    }
    
}