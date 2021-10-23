using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;

namespace PawnStorages
{
    public class CompPawnStorage : ThingComp
    {
        public Pawn toStore;

        public override string TransformLabel(string label)
        {
            if (Props.appendOfName && storedPawns.Any())
            {
                return base.TransformLabel(label) + "PS.Of".Translate(storedPawns.First().LabelCap);
            }
            return base.TransformLabel(label);
        }

        private List<Pawn> storedPawns;

        public List<Pawn> StoredPawns => storedPawns;
        public CompPawnStorage()
        {
            storedPawns = new List<Pawn>();
        }
        public CompProperties_PawnStorage Props => base.props as CompProperties_PawnStorage;
        public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
        {
            foreach (var f in base.CompFloatMenuOptions(selPawn))
            {
                yield return f;
            }
            if (storedPawns.Any())
            {
                if (Props.releaseAllOption)
                {
                    yield return new FloatMenuOption("PS.ReleaseAll".Translate(), delegate
                    {
                        Job job = JobMaker.MakeJob(PS_DefOf.PS_ReleaseAll, this.parent);
                        selPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                    });
                }
                Log.Message("Props.releaseOption: " + Props.releaseOption);
                if (Props.releaseOption && CanRelease(selPawn))
                {
                    foreach (var pawn in storedPawns)
                    {
                        yield return new FloatMenuOption("PS.Release".Translate(pawn.LabelCap), delegate
                        {
                            selPawn.jobs.TryTakeOrderedJob(ReleaseJob(selPawn, pawn), JobTag.Misc);
                        });
                    }
                }
            }

            if (Props.convertOption && Props.maxStoredPawns > this.storedPawns.Count)
            {
                yield return new FloatMenuOption("PS.Enter".Translate(), delegate
                {
                    Job job = JobMaker.MakeJob(PS_DefOf.PS_Enter, this.parent);
                    selPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                });
            }
        }

        public void ReleasePawn(Pawn pawn, IntVec3 cell, Map map)
        {
            this.storedPawns.Remove(pawn);
            GenSpawn.Spawn(pawn, cell, map);
            if (this.Props.lightEffect)
            {
                FleckMaker.ThrowLightningGlow(cell.ToVector3Shifted(), map, 0.5f);
            }
            if (this.Props.transformEffect)
            {
                FleckMaker.ThrowExplosionCell(cell, map, FleckDefOf.ExplosionFlash, Color.white);
            }
        }
        public void StorePawn(Pawn pawn)
        {
            if (this.Props.lightEffect)
            {
                FleckMaker.ThrowLightningGlow(pawn.Position.ToVector3Shifted(), pawn.Map, 0.5f);
            }
            if (this.Props.transformEffect)
            {
                FleckMaker.ThrowExplosionCell(pawn.Position, pawn.Map, FleckDefOf.ExplosionFlash, Color.white);
            }
            pawn.DeSpawn();
            this.storedPawns.Add(pawn);

        }
        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            for (int num = this.storedPawns.Count - 1; num >= 0; num--)
            {
                var pawn = this.storedPawns[num];
                this.storedPawns.Remove(pawn);
                GenSpawn.Spawn(pawn, this.parent.Position, previousMap);
            }
            base.PostDestroy(mode, previousMap);
        }
        public virtual bool CanRelease(Pawn releaser)
        {
            return true;
        }
        public virtual Job ReleaseJob(Pawn releaser, Pawn toRelease)
        {
            return JobMaker.MakeJob(PS_DefOf.PS_Release, this.parent, toRelease);
        }

        public override IEnumerable<FloatMenuOption> CompMultiSelectFloatMenuOptions(List<Pawn> selPawns)
        {
            var selPawnsCopy = selPawns.ListFullCopy();
            if (Props.convertOption && Props.maxStoredPawns > this.storedPawns.Count)
            {
                yield return new FloatMenuOption("PS.Enter".Translate(), delegate
                {
                    var diff = Props.maxStoredPawns - this.storedPawns.Count;
                    for (var i = 0; i < diff; i++)
                    {
                        if (i < selPawnsCopy.Count)
                        {
                            Job job = JobMaker.MakeJob(PS_DefOf.PS_Enter, this.parent);
                            selPawnsCopy[i].jobs.TryTakeOrderedJob(job, JobTag.Misc);
                        }
                    }
                });
            }

            foreach (var f in base.CompMultiSelectFloatMenuOptions(selPawns))
            {
                yield return f;
            }
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (var g in base.CompGetGizmosExtra())
            {
                yield return g;
            }
            if (Props.releaseAllOption)
            {
                yield return new Command_Action
                {
                    defaultLabel = "PS.ReleaseAll".Translate(),
                    action = delegate
                    {
                        for (int num = this.storedPawns.Count - 1; num >= 0; num--)
                        {
                            var pawn = this.storedPawns[num];
                            this.storedPawns.Remove(pawn);
                            GenSpawn.Spawn(pawn, this.parent.Position, this.parent.Map);
                        }
                    },
                    icon = ContentFinder<Texture2D>.Get("UI/Buttons/ReleaseAll")
                };
            }
        }
        public override void CompTick()
        {
            base.CompTick();
            if (toStore != null)
            {
                toStore.DeSpawn();
                this.storedPawns.Add(toStore);
                toStore = null;
            }
            if (Props.idleResearch && (this.storedPawns?.Any() ?? false) && Find.ResearchManager.currentProj != null)
            {
                foreach (var pawn in this.storedPawns)
                {
                    float statValue = pawn.GetStatValue(StatDefOf.ResearchSpeed);
                    statValue *= 0.5f;
                    Find.ResearchManager.ResearchPerformed(statValue, pawn);
                    pawn.skills.Learn(SkillDefOf.Intellectual, 0.1f);
                }
            }
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Collections.Look(ref storedPawns, "storedPawns", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (storedPawns is null)
                {
                    storedPawns = new List<Pawn>();
                }
            }
        }
    }
}
