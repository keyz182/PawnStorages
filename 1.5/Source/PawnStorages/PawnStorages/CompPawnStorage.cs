using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace PawnStorages;

public class CompPawnStorage : ThingComp
{
    public const int TICKRATE = 60;
    public const int NEEDS_INTERVAL = 150;

    private CompAssignableToPawn_PawnStorage compAssignable;
    private bool labelDirty = true;
    public bool schedulingEnabled;
    private List<Pawn> storedPawns;
    private Dictionary<int, int> pawnStoringTick;
    private string transformLabelCache;

    public Rot4 Rotation = default;

    public CompPawnStorage()
    {
        storedPawns = [];
        pawnStoringTick = new Dictionary<int, int>();
    }

    public CompProperties_PawnStorage Props => props as CompProperties_PawnStorage;
    public List<Pawn> StoredPawns => storedPawns;
    public bool CanStore => storedPawns.Count < Props.maxStoredPawns;

    public bool CanAssign(Pawn pawn, bool couldMakePrisoner) =>
        compAssignable?.OwnerType switch
        {
            BedOwnerType.Colonist => pawn.IsColonist,
            BedOwnerType.Slave => pawn.IsSlave,
            BedOwnerType.Prisoner => pawn.IsPrisoner || couldMakePrisoner,
            _ => true
        } && (compAssignable == null || compAssignable.AssignedPawns.Contains(pawn) || compAssignable.HasFreeSlot);

    public void TryAssignPawn(Pawn pawn) => compAssignable?.TryAssignPawn(pawn);

    public void SetLabelDirty() => labelDirty = true;

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        compAssignable = parent.TryGetComp<CompAssignableToPawn_PawnStorage>();
    }

    public override void PostExposeData()
    {
        Scribe_Collections.Look(ref pawnStoringTick, "pawnStoringTick", LookMode.Value, LookMode.Value);
        Scribe_Collections.Look(ref storedPawns, "storedPawns", LookMode.Deep);
        Scribe_Values.Look(ref schedulingEnabled, "schedulingEnabled");
        Scribe_Values.Look(ref Rotation, "Rotation");
        if (Scribe.mode != LoadSaveMode.PostLoadInit) return;
        storedPawns ??= [];
        pawnStoringTick ??= new Dictionary<int, int>();
    }

    public override void PostDestroy(DestroyMode mode, Map previousMap)
    {
        for (int num = storedPawns.Count - 1; num >= 0; num--)
        {
            Pawn pawn = storedPawns[num];
            storedPawns.Remove(pawn);
            GenSpawn.Spawn(pawn, parent.Position, previousMap);
        }

        base.PostDestroy(mode, previousMap);
    }

    public override void CompTick()
    {
        if (Find.TickManager.TicksGame % TICKRATE != 0) return;
        if (Props.idleResearch && Find.ResearchManager.currentProj != null)
            foreach (Pawn pawn in storedPawns)
                if (pawn.RaceProps.Humanlike)
                {
                    float value = pawn.GetStatValue(StatDefOf.ResearchSpeed);
                    value *= 0.5f;
                    Find.ResearchManager.ResearchPerformed(value * TICKRATE, pawn);
                    pawn.skills.Learn(SkillDefOf.Intellectual, 0.1f * TICKRATE);

                    if (Props.pawnRestIncreaseTick != 0) pawn.needs.rest.CurLevel += Props.pawnRestIncreaseTick * TICKRATE;
                }

        if (!schedulingEnabled || compAssignable == null) return;
        {
            foreach (Pawn pawn in compAssignable.AssignedPawns)
                switch (pawn.Spawned)
                {
                    case true when pawn.timetable.CurrentAssignment == PS_DefOf.PS_Home &&
                                   pawn.CurJobDef != PS_DefOf.PS_Enter &&
                                   pawn.health.State == PawnHealthState.Mobile &&
                                   !pawn.CurJob.restUntilHealed &&
                                   !HealthAIUtility.ShouldSeekMedicalRest(pawn):
                    {
                        Job job = EnterJob(pawn);
                        pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                        break;
                    }
                    case false when StoredPawns.Contains(pawn) && pawn.timetable.CurrentAssignment != PS_DefOf.PS_Home:
                        ReleasePawn(pawn, parent.Position, parent.Map);
                        break;
                }
        }
    }


    public override string TransformLabel(string label)
    {
        if (!labelDirty) return transformLabelCache;
        transformLabelCache = !StoredPawns.NullOrEmpty() ? $"{base.TransformLabel(label)} {"PS_Filled".Translate()}" : $"{base.TransformLabel(label)} {"PS_Empty".Translate()}";
        labelDirty = false;

        return transformLabelCache;
    }

    public override bool AllowStackWith(Thing other)
    {
        return StoredPawns.NullOrEmpty()
               && base.AllowStackWith(other)
               && (other.TryGetComp<CompPawnStorage>()?.storedPawns?.NullOrEmpty() ?? true);
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

        if (!storedPawns.Any()) yield break;
        {
            if (Props.releaseAllOption && !(Props.releaseOption && storedPawns.Count == 1))
                yield return new FloatMenuOption("PS_ReleaseAll".Translate(), delegate
                {
                    Job job = JobMaker.MakeJob(PS_DefOf.PS_Release, parent);
                    selPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                });
            if (!Props.releaseOption || !CanRelease(selPawn)) yield break;
            foreach (Pawn pawn in storedPawns)
                yield return new FloatMenuOption("PS_Release".Translate(pawn.LabelCap), delegate { selPawn.jobs.TryTakeOrderedJob(ReleaseJob(selPawn, pawn), JobTag.Misc); });
        }
    }

    //Funcs
    public void ReleasePawn(Pawn pawn, IntVec3 cell, Map map)
    {
        if (!cell.Walkable(map))
            foreach (IntVec3 t in GenRadial.RadialPattern)
            {
                IntVec3 intVec = pawn.Position + t;
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
        parent.Map.mapDrawer.MapMeshDirty(parent.Position, MapMeshFlagDefOf.Things);

        labelDirty = true;
        ApplyNeedsForStoredPeriodFor(pawn);
        pawn.guest?.WaitInsteadOfEscapingFor(1250);
    }

    public virtual void ApplyNeedsForStoredPeriodFor(Pawn pawn)
    {
        int storedAtTick = pawnStoringTick.TryGetValue(pawn.thingIDNumber, -1);
        pawnStoringTick.Remove(pawn.thingIDNumber);
        if (storedAtTick <= 0 || !PawnStoragesMod.settings.AllowNeedsDrop) return;

        // We drop one tick interval to make sure we don't boost need drops from being at home, the slight reduction can be seen as a benefit of being at home.
        int ticksStored = Mathf.Max(0, Find.TickManager.TicksGame - storedAtTick - NEEDS_INTERVAL);
        if (!Props.needsDrop) return;

        foreach (Need need in pawn.needs.AllNeeds)
        {
            switch (need)
            {
                case Need_Food foodNeed:
                    foodNeed.CurLevel -= foodNeed.FoodFallPerTick * ticksStored;
                    continue;
                case Need_Chemical chemicalNeed:
                    chemicalNeed.CurLevel -= chemicalNeed.ChemicalFallPerTick * ticksStored;
                    continue;
                case Need_Chemical_Any { Disabled: false } chemicalNeedAny:
                    chemicalNeedAny.CurLevel -= chemicalNeedAny.FallPerNeedIntervalTick / NEEDS_INTERVAL * ticksStored;
                    continue;
            }
        }
    }

    public void StorePawn(Pawn pawn)
    {
        if (Props.lightEffect) FleckMaker.ThrowLightningGlow(pawn.Position.ToVector3Shifted(), pawn.Map, 0.5f);
        if (Props.transformEffect) FleckMaker.ThrowExplosionCell(pawn.Position, pawn.Map, FleckDefOf.ExplosionFlash, Color.white);
        //Spawn the store effecter
        Props.storeEffect?.Spawn(pawn.Position, parent.Map);

        pawn.DeSpawn();
        storedPawns.Add(pawn);

        parent.Map.mapDrawer.MapMeshDirty(parent.Position, MapMeshFlagDefOf.Things);

        if (compAssignable != null && !compAssignable.AssignedPawns.Contains(pawn)) compAssignable.TryAssignPawn(pawn);
        labelDirty = true;
        pawnStoringTick.SetOrAdd(pawn.thingIDNumber, Find.TickManager.TicksGame);
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
        if (StoredPawns?.Any() != true) return sb.ToString().TrimStart().TrimEnd();
        sb.AppendLine();
        sb.AppendLine("PS_StoredPawns".Translate());
        foreach (Pawn pawn in StoredPawns) sb.AppendLine($"    - {pawn.LabelCap}");

        return sb.ToString().TrimStart().TrimEnd();
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (Gizmo g in base.CompGetGizmosExtra()) yield return g;
        if (Props.releaseAllOption && storedPawns.Any())
        {
            yield return new Command_Action
            {
                defaultLabel = storedPawns.Count == 1
                    ? "PS_Release".Translate(storedPawns[0].Name.ToStringShort)
                    : "PS_ReleaseAll".Translate(),
                action = delegate
                {
                    for (int num = storedPawns.Count - 1; num >= 0; num--)
                    {
                        Pawn pawn = storedPawns[num];
                        storedPawns.Remove(pawn);
                        GenSpawn.Spawn(pawn, parent.Position, parent.Map);
                        parent.Map.mapDrawer.MapMeshDirty(parent.Position, MapMeshFlagDefOf.Things);
                    }
                },
                icon = ContentFinder<Texture2D>.Get("UI/Buttons/ReleaseAll")
            };

            if (Find.Selector.SelectedObjectsListForReading
                    .Select(o => (o as ThingWithComps)?.TryGetComp<CompPawnStorage>())
                    .Where(o => o?.storedPawns?.Any() == true)
                    .ToList() is { Count: > 1 } comps
                && this == comps.First())
            {
                yield return new Command_Action
                {
                    defaultLabel = "PS_ReleaseAllSelected".Translate(),
                    action = delegate
                    {
                        foreach (CompPawnStorage compPawnStorage in comps)
                        {
                            for (int num = compPawnStorage.storedPawns.Count - 1; num >= 0; num--)
                            {
                                Pawn pawn = compPawnStorage.storedPawns[num];
                                compPawnStorage.storedPawns.Remove(pawn);
                                GenSpawn.Spawn(pawn, compPawnStorage.parent.Position, compPawnStorage.parent.Map);
                                compPawnStorage.parent.Map.mapDrawer.MapMeshDirty(compPawnStorage.parent.Position, MapMeshFlagDefOf.Things);
                            }
                        }
                    },
                    icon = ContentFinder<Texture2D>.Get("UI/Buttons/ReleaseAll")
                };
                if (PawnStoragesMod.settings.SpecialReleaseAll && ModsConfig.anomalyActive)
                {
                    yield return new Command_Action
                    {
                        defaultLabel = Translator.PseudoTranslated("PS_ReleaseAll".Translate()),
                        action = delegate
                        {
                            Pawn p;
                            if (ModsConfig.IsActive("taggerung.grignrhappensby"))
                            {
                                p = PawnGenerator.GeneratePawn(DefDatabase<PawnKindDef>.GetNamed("Taggerung_ShardOfGrignr"), Faction.OfHoraxCult);
                                p.Name = new NameTriple("Grignr", "PS_All".Translate(), "Grignrson");
                            }
                            else
                            {
                                p = PawnGenerator.GeneratePawn(PawnKindDefOf.Ghoul, Faction.OfHoraxCult);
                                p.Name = new NameSingle("PS_All".Translate());
                            }

                            p.health.AddHediff(HediffDefOf.Inhumanized);
                            p.health.AddHediff(HediffDefOf.ShardHolder);
                            GenSpawn.Spawn(p, parent.Position, parent.Map);
                            Messages.Message(Translator.PseudoTranslated("PS_ReleaseAll_Anomaly_Message".Translate()), new LookTargets(p), MessageTypeDefOf.ThreatBig, false);
                            PawnStoragesMod.settings.AllReleased();

                            GridShapeMaker.IrregularLump(parent.Position, parent.Map, 5)
                                .InRandomOrder()
                                .Where(cell => cell.InBounds(parent.Map))
                                .Do(intVec3 => parent.Map.terrainGrid.SetTerrain(intVec3, TerrainDefOf.Voidmetal));

                            EffecterDefOf.Skip_EntryNoDelay.Spawn(p, parent.Map).Cleanup();
                            p.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Berserk, forced: true);
                        },
                        icon = ContentFinder<Texture2D>.Get("UI/Buttons/ReleaseAll")
                    };
                }
            }
        }

        if (Props.canBeScheduled)
            yield return new Command_Toggle
            {
                defaultLabel = "PS_EnableScheduling".Translate(),
                toggleAction = () => schedulingEnabled = !schedulingEnabled,
                isActive = () => schedulingEnabled,
                icon = ContentFinder<Texture2D>.Get("UI/Buttons/ReleaseAll")
            };

        yield return new Command_Toggle
        {
            defaultLabel = "PS_Rotate".Translate(),
            toggleAction = () => { Rotation.Rotate(RotationDirection.Clockwise); },
            isActive = () => true,
            icon = ContentFinder<Texture2D>.Get("UI/Buttons/PS_Rotate")
        };

        if (Props.allowNonColonist && compAssignable != null) yield return new Command_SetPawnStorageOwnerType(compAssignable);
    }
}
