using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;

namespace PawnStorages
{
    public class CompProperties_PawnStorage : CompProperties
    {
        public int maxStoredPawns = 1;
        public bool releaseOption;
        public bool releaseAllOption;
        public bool convertOption;
        public bool appendOfName;
        public bool showStoredPawn;

        public bool idleResearch;
        public CompProperties_PawnStorage()
        {
            this.compClass = typeof(CompPawnStorage);
        }
    }
    public class CompPawnStorage : ThingComp
    {
        public override void PostDraw()
        {
            base.PostDraw();
            if (Props.showStoredPawn && storedPawns.Any())
            {
                var pos = this.parent.DrawPos;
                pos.y += 0.1f;
                storedPawns.First().Drawer.renderer.RenderPawnAt(pos, Rot4.South);
            }
        }
        public override string TransformLabel(string label)
        {
            if (Props.appendOfName && storedPawns.Any())
            {
                return base.TransformLabel(label) + "PS.Of".Translate(storedPawns.First().LabelCap);
            }
            return base.TransformLabel(label);
        }
        public List<Pawn> storedPawns;
        public List<Pawn> spawnedPawns;
        public CompPawnStorage()
        {
            spawnedPawns = new List<Pawn>();
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
                if (Props.releaseOption)
                {
                    foreach (var pawn in storedPawns)
                    {
                        yield return new FloatMenuOption("PS.Release".Translate(pawn.LabelCap), delegate
                        {
                            Job job = JobMaker.MakeJob(PS_DefOf.PS_Release, this.parent, pawn);
                            selPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
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

        public override void CompTick()
        {
            base.CompTick();
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
            Scribe_Collections.Look(ref spawnedPawns, "spawnedPawns", LookMode.Reference);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (storedPawns is null)
                {
                    storedPawns = new List<Pawn>();
                }
                if (spawnedPawns is null)
                {
                    spawnedPawns = new List<Pawn>();
                }
            }
        }
    }

    public class Recipe_ReleasePawn : RecipeWorker
    {
        public override void ConsumeIngredient(Thing ingredient, RecipeDef recipe, Map map)
        {

        }
        public override void Notify_IterationCompleted(Pawn billDoer, List<Thing> ingredients)
        {
            base.Notify_IterationCompleted(billDoer, ingredients);
            var pawnStorage = ingredients.FirstOrDefault(x => x.TryGetComp<CompPawnStorage>() != null);
            var comp = pawnStorage.TryGetComp<CompPawnStorage>();
            var pawn = comp.storedPawns.First();
            GenSpawn.Spawn(pawn, billDoer.Position, billDoer.Map);
            comp.storedPawns.Remove(pawn);
        }
    }

    [HarmonyPatch(typeof(Bill), "IsFixedOrAllowedIngredient", new Type[] { typeof(Thing) })]
    public static class IsFixedOrAllowedIngredient_Patch
    {
        private static bool Prefix(ref bool __result, Bill __instance, Thing thing)
        {
            if (thing != null && __instance.recipe?.Worker is Recipe_ReleasePawn && thing.TryGetComp<CompPawnStorage>() is CompPawnStorage comp && (!comp?.storedPawns?.Any() ?? false))
            {
                __result = false;
                return false;
            }
            return true;
        }
    }

    public class JobDriver_Enter : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
            Toil toil = Toils_General.Wait(60);
            toil.FailOnCannotTouch(TargetIndex.A, PathEndMode.InteractionCell);
            toil.WithProgressBarToilDelay(TargetIndex.A);
            yield return toil;
            Toil enter = new Toil();
            enter.initAction = delegate
            {
                Pawn actor = enter.actor;
                var comp = TargetA.Thing.TryGetComp<CompPawnStorage>();
                if (comp.Props.maxStoredPawns > comp.storedPawns.Count)
                {
                    actor.DeSpawn();
                    comp.storedPawns.Add(actor);
                }
            };
            enter.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return enter;
        }
    }

    public class JobDriver_Release : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
        }
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch);
            Toil toil = Toils_General.Wait(60);
            toil.FailOnCannotTouch(TargetIndex.A, PathEndMode.ClosestTouch);
            toil.WithProgressBarToilDelay(TargetIndex.A);
            yield return toil;
            Toil enter = new Toil();
            enter.initAction = delegate
            {
                Pawn actor = enter.actor;
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
            enter.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return enter;
        }
    }

    [DefOf]
    public static class PS_DefOf
    {
        public static JobDef PS_Enter;
        public static JobDef PS_Release;
        public static JobDef PS_ReleaseAll;
    }

    [StaticConstructorOnStartup]
    public static class HarmonyInit
    {
        public static Harmony harmonyInstance;
        static HarmonyInit()
        {
            harmonyInstance = new Harmony("PawnStorages.Mod");
            harmonyInstance.PatchAll();
        }
    }

    [HarmonyPatch(typeof(ITab_Pawn_Character), "PawnToShowInfoAbout", MethodType.Getter)]
    public static class ITab_Pawn_Character_PawnToShowInfoAbout_Patch
    {
        public static bool Prefix(ref Pawn __result)
        {
            var comp = Find.Selector.SingleSelectedThing.TryGetComp<CompPawnStorage>();
            if (comp != null && comp.storedPawns.Any())
            {
                __result = comp.storedPawns.First();
                return false;
            }
            return true;
        }
    }
}
