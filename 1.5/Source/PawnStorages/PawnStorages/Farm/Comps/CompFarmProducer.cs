using RimWorld;
using System.Collections.Generic;
using PawnStorages.Farm.Interfaces;
using UnityEngine;
using Verse;

namespace PawnStorages.Farm.Comps
{
    public class CompFarmProducer: ThingComp
    {
        public IProductionParent Parent => parent as IProductionParent;


        protected List<Thing> DaysProduce = new();
        public bool ProduceNow = false;
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Collections.Look(ref DaysProduce, "daysProduce", LookMode.Value);
        }

        public override void CompTick()
        {
            base.CompTick();

            if (!PawnStoragesMod.settings.AllowNeedsDrop) return;
            var pawnsToRemove = new List<Pawn>();


            if (parent.IsHashIntervalTick(Parent.TickInterval))
            {
                var healthyPawns = new List<Pawn>();

                foreach (Pawn pawn in Parent.ProducingPawns)
                {
                    if (pawn.TryGetComp(out CompEggLayer compLayer) && pawn.gender != Gender.Male)
                    {
                        EggLayerTick(compLayer, Parent.TickInterval);
                    }

                    if (pawn.TryGetComp(out CompHasGatherableBodyResource compGatherable) &&
                        pawn.gender != Gender.Male)
                    {
                        GatherableTick(compGatherable, Parent.TickInterval);
                    }
                }
            }

            if (!ProduceNow && (!parent.IsHashIntervalTick(60000) || DaysProduce.Count <= 0 || !Parent.IsActive)) return;
            foreach (Thing thing in DaysProduce)
            {
                GenPlace.TryPlaceThing(thing, parent.Position, parent.Map, ThingPlaceMode.Near);
            }
            DaysProduce.Clear();
        }

        public void EggLayerTick(CompEggLayer layer, int tickInterval = 1)
        {
            if (!layer.Active) return;
            float eggReadyIncrement = (float)(1f / ((double)layer.Props.eggLayIntervalDays * 60000f));
            eggReadyIncrement *= PawnUtility.BodyResourceGrowthSpeed(layer.parent as Pawn);
            // we're not doing this every tick so bump the progress
            eggReadyIncrement *= tickInterval;
            eggReadyIncrement *= PawnStoragesMod.settings.ProductionScale;
            layer.eggProgress += eggReadyIncrement;
            layer.eggProgress = Mathf.Clamp(layer.eggProgress, 0f, 1f);

            if (!(layer.eggProgress >= 1f)) return;
            Thing thing = layer.ProduceEgg();
            if (thing != null)
            {
                DaysProduce.Add(thing);
            }
        }

        public void GatherableTick(CompHasGatherableBodyResource gatherable, int tickInterval = 1)
        {
            if (!gatherable.Active) return;
            float gatherableReadyIncrement = (float)(1f / ((double)gatherable.GatherResourcesIntervalDays * 60000f));
            gatherableReadyIncrement *= PawnUtility.BodyResourceGrowthSpeed(gatherable.parent as Pawn);
            // we're not doing this every tick so bump the progress
            gatherableReadyIncrement *= tickInterval;
            gatherableReadyIncrement *= PawnStoragesMod.settings.ProductionScale;
            gatherable.fullness += gatherableReadyIncrement;
            gatherable.fullness = Mathf.Clamp(gatherable.fullness, 0f, 1f);

            if (!gatherable.ActiveAndFull) return;
            int amountToGenerate = GenMath.RoundRandom(gatherable.ResourceAmount * gatherable.fullness);
            while (amountToGenerate > 0f)
            {
                int generateThisLoop = Mathf.Clamp(amountToGenerate, 1, gatherable.ResourceDef.stackLimit);
                amountToGenerate -= generateThisLoop;
                Thing thing = ThingMaker.MakeThing(gatherable.ResourceDef);
                thing.stackCount = generateThisLoop;
                DaysProduce.Add(thing);
            }

            gatherable.fullness = 0f;
        }



        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (!DebugSettings.ShowDevGizmos) yield break;
            yield return new Command_Action
            {
                defaultLabel = "Make all animals ready to produce",
                action = delegate
                {
                    foreach (Pawn storedPawn in Parent.ProducingPawns)
                    {
                        storedPawn.needs.food.CurLevel = storedPawn.needs.food.MaxLevel;

                        if (storedPawn.TryGetComp(out CompEggLayer compLayer))
                        {
                            compLayer.eggProgress = 1f;
                        }

                        if (storedPawn.TryGetComp(out CompHasGatherableBodyResource compGatherable))
                        {
                            compGatherable.fullness = 1f;
                        }

                    }
                },
                icon = ContentFinder<Texture2D>.Get("UI/Buttons/ReleaseAll")
            };
            yield return new Command_Action
            {
                defaultLabel = "Produce on next tick",
                action = delegate
                {
                    ProduceNow = true;
                },
                icon = ContentFinder<Texture2D>.Get("UI/Buttons/ReleaseAll")
            };
        }
    }
}
