using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace PawnStorages;

public class CompPawnStorage : ThingComp
{
    public const int TICKRATE = 60;

    private CompAssignableToPawn_PawnStorage compAssignable;
    private bool labelDirty = true;
    public bool schedulingEnabled;
    private List<Pawn> storedPawns;
    private string transformLabelCache;

    public CompPawnStorage()
    {
        storedPawns = new List<Pawn>();
    }

    public CompProperties_PawnStorage Props => props as CompProperties_PawnStorage;
    public List<Pawn> StoredPawns => storedPawns;
    public bool CanStore => storedPawns.Count < Props.maxStoredPawns;

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        compAssignable = parent.TryGetComp<CompAssignableToPawn_PawnStorage>();
    }

    public override void PostExposeData()
    {
        Scribe_Collections.Look(ref storedPawns, "storedPawns", LookMode.Deep);
        Scribe_Values.Look(ref schedulingEnabled, "schedulingEnabled");
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
            if (storedPawns is null)
                storedPawns = new List<Pawn>();
    }

    public override void PostDestroy(DestroyMode mode, Map previousMap)
    {
        for (var num = storedPawns.Count - 1; num >= 0; num--)
        {
            Pawn pawn = storedPawns[num];
            storedPawns.Remove(pawn);
            GenSpawn.Spawn(pawn, parent.Position, previousMap);
        }

        base.PostDestroy(mode, previousMap);
    }

    public override void CompTick()
    {
        if (Find.TickManager.TicksGame % TICKRATE == 0)
        {
            if (Props.idleResearch && Find.ResearchManager.currentProj != null)
                foreach (Pawn pawn in storedPawns)
                    if (pawn.RaceProps.Humanlike)
                    {
                        var value = pawn.GetStatValue(StatDefOf.ResearchSpeed);
                        value *= 0.5f;
                        Find.ResearchManager.ResearchPerformed(value * TICKRATE, pawn);
                        pawn.skills.Learn(SkillDefOf.Intellectual, 0.1f * TICKRATE);

                        if (Props.pawnRestIncreaseTick != 0) pawn.needs.rest.CurLevel += Props.pawnRestIncreaseTick * TICKRATE;
                    }

            if (schedulingEnabled && compAssignable != null)
                foreach (Pawn pawn in compAssignable.AssignedPawns)
                    if (pawn.Spawned && pawn.timetable.CurrentAssignment == PS_DefOf.PS_Home && pawn.CurJobDef != PS_DefOf.PS_Enter)
                    {
                        Job job = EnterJob(pawn);
                        pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                    }
                    else if (!pawn.Spawned && StoredPawns.Contains(pawn) && pawn.timetable.CurrentAssignment != PS_DefOf.PS_Home)
                    {
                        ReleasePawn(pawn, parent.Position, parent.Map);
                    }
        }
    }


    public override string TransformLabel(string label)
    {
        if (labelDirty)
        {
            if (!StoredPawns.NullOrEmpty())
                transformLabelCache = $"{base.TransformLabel(label)} {"PS_Filled".Translate()}";
            else
                transformLabelCache = $"{base.TransformLabel(label)} {"PS_Empty".Translate()}";
            labelDirty = false;
        }

        return transformLabelCache;
    }

    public override bool AllowStackWith(Thing other)
    {
        if (!StoredPawns.NullOrEmpty()) return false;
        return base.AllowStackWith(other);
    }

    public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
    {
        foreach (FloatMenuOption f in base.CompFloatMenuOptions(selPawn)) yield return f;

        if (Props.convertOption && CanStore)
            yield return new FloatMenuOption("PS_Enter".Translate(), delegate
            {
                Job job = EnterJob(selPawn);
                selPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
            });

        if (storedPawns.Any())
        {
            if (Props.releaseAllOption)
                yield return new FloatMenuOption("PS_ReleaseAll".Translate(), delegate
                {
                    Job job = JobMaker.MakeJob(PS_DefOf.PS_Release, parent);
                    selPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                });
            if (Props.releaseOption && CanRelease(selPawn))
                foreach (Pawn pawn in storedPawns)
                    yield return new FloatMenuOption("PS_Release".Translate(pawn.LabelCap), delegate { selPawn.jobs.TryTakeOrderedJob(ReleaseJob(selPawn, pawn), JobTag.Misc); });
        }
    }

    //Funcs
    public void ReleasePawn(Pawn pawn, IntVec3 cell, Map map)
    {
        if (!cell.Walkable(map))
            for (var i = 0; i < GenRadial.RadialPattern.Length; i++)
            {
                IntVec3 intVec = pawn.Position + GenRadial.RadialPattern[i];
                if (!intVec.Walkable(map)) continue;
                cell = intVec;
                break;
            }

        storedPawns.Remove(pawn);
        GenSpawn.Spawn(pawn, cell, map);

        //Spawn the release effecter
        Props.releaseEffect?.Spawn(cell, map);

        if (Props.lightEffect) FleckMaker.ThrowLightningGlow(cell.ToVector3Shifted(), map, 0.5f);
        if (Props.transformEffect) FleckMaker.ThrowExplosionCell(cell, map, FleckDefOf.ExplosionFlash, Color.white);

        map.mapDrawer.MapMeshDirty(cell, MapMeshFlag.Things);

        labelDirty = true;
    }

    public void StorePawn(Pawn pawn)
    {
        if (Props.lightEffect) FleckMaker.ThrowLightningGlow(pawn.Position.ToVector3Shifted(), pawn.Map, 0.5f);
        if (Props.transformEffect) FleckMaker.ThrowExplosionCell(pawn.Position, pawn.Map, FleckDefOf.ExplosionFlash, Color.white);
        //Spawn the store effecter
        Props.storeEffect?.Spawn(pawn.Position, parent.Map);

        pawn.DeSpawn();
        storedPawns.Add(pawn);

        parent.Map.mapDrawer.MapMeshDirty(parent.Position, MapMeshFlag.Things);

        if (compAssignable != null && !compAssignable.AssignedPawns.Contains(pawn)) compAssignable.TryAssignPawn(pawn);
        labelDirty = true;
    }

    public virtual bool CanRelease(Pawn releaser)
    {
        if (parent.def.EverHaulable && parent.def.category == ThingCategory.Item && Props.storageStation != null)
            return GenClosest.ClosestThingReachable(releaser.Position, releaser.Map,
                ThingRequest.ForDef(Props.storageStation), PathEndMode.InteractionCell, TraverseParms.For(releaser),
                9999f, x => releaser.CanReserve(x)) != null;
        return true;
    }

    public virtual Job ReleaseJob(Pawn releaser, Pawn toRelease)
    {
        if (parent.def.EverHaulable && parent.def.category == ThingCategory.Item && Props.storageStation != null)
        {
            Thing station = GenClosest.ClosestThingReachable(releaser.Position, releaser.Map, ThingRequest.ForDef(Props.storageStation), PathEndMode.InteractionCell,
                TraverseParms.For(releaser), 9999f, x => releaser.CanReserve(x));
            Job job = JobMaker.MakeJob(PS_DefOf.PS_Release, parent, station, toRelease);
            job.count = 1;
            return job;
        }

        return JobMaker.MakeJob(PS_DefOf.PS_Release, parent, null, toRelease);
    }

    public virtual Job EnterJob(Pawn enterer)
    {
        //Check is storage is item with station
        if (parent.def.EverHaulable && parent.def.category == ThingCategory.Item && Props.storageStation != null)
        {
            Thing station = GenClosest.ClosestThingReachable(enterer.Position, enterer.Map, ThingRequest.ForDef(Props.storageStation), PathEndMode.InteractionCell,
                TraverseParms.For(enterer), 9999f, x => enterer.CanReserve(x));
            Job job = JobMaker.MakeJob(PS_DefOf.PS_Enter, parent, station);
            job.count = 1;
            return job;
        }

        //Store in side parent directly
        return JobMaker.MakeJob(PS_DefOf.PS_Enter, parent);
    }

    public override IEnumerable<FloatMenuOption> CompMultiSelectFloatMenuOptions(List<Pawn> selPawns)
    {
        var selPawnsCopy = selPawns.ListFullCopy();
        if (Props.convertOption && Props.maxStoredPawns > storedPawns.Count)
            yield return new FloatMenuOption("PS_Enter".Translate(), delegate
            {
                var diff = Props.maxStoredPawns - storedPawns.Count;
                for (var i = 0; i < diff; i++)
                    if (i < selPawnsCopy.Count)
                    {
                        Job job = JobMaker.MakeJob(PS_DefOf.PS_Enter, parent);
                        selPawnsCopy[i].jobs.TryTakeOrderedJob(job, JobTag.Misc);
                    }
            });

        foreach (FloatMenuOption f in base.CompMultiSelectFloatMenuOptions(selPawns)) yield return f;
    }

    public override string CompInspectStringExtra()
    {
        StringBuilder sb = new(base.CompInspectStringExtra());
        if (StoredPawns.Any())
        {
            sb.AppendLine();
            sb.AppendLine("PS_StoredPawns".Translate());
            foreach (Pawn pawn in StoredPawns) sb.AppendLine($"    - {pawn.LabelCap}");
        }

        return sb.ToString().TrimStart().TrimEnd();
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (Gizmo g in base.CompGetGizmosExtra()) yield return g;
        if (Props.releaseAllOption)
            yield return new Command_Action
            {
                defaultLabel = "PS_ReleaseAll".Translate(),
                action = delegate
                {
                    for (var num = storedPawns.Count - 1; num >= 0; num--)
                    {
                        Pawn pawn = storedPawns[num];
                        storedPawns.Remove(pawn);
                        GenSpawn.Spawn(pawn, parent.Position, parent.Map);
                    }
                },
                icon = ContentFinder<Texture2D>.Get("UI/Buttons/ReleaseAll")
            };

        if (Props.canBeScheduled)
            yield return new Command_Toggle
            {
                defaultLabel = "PS_EnableScheduling".Translate(),
                toggleAction = () => schedulingEnabled = !schedulingEnabled,
                isActive = () => schedulingEnabled,
                icon = ContentFinder<Texture2D>.Get("UI/Buttons/ReleaseAll")
            };
    }
}
