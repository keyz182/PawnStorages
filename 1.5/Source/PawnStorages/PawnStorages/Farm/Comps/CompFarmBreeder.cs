using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PawnStorages.Farm.Interfaces;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace PawnStorages.Farm.Comps
{
    public class CompFarmBreeder : ThingComp
    {
        public int AutoSlaughterTarget = 0;
        public IBreederParent Parent => parent as IBreederParent;

        public List<AutoSlaughterConfig> AutoSlaughterSettings = new List<AutoSlaughterConfig>();
        private void TryPopulateMissingAnimals()
        {
            HashSet<ThingDef> hashSet = new HashSet<ThingDef>();
            hashSet.AddRange(AutoSlaughterSettings.Select(c => c.animal));
            foreach (PawnKindDef allDef in DefDatabase<PawnKindDef>.AllDefs)
            {
                if (allDef.race != null && allDef.race.race.Animal && (double) allDef.race.race.wildness < 1.0 && !allDef.race.race.Dryad && !allDef.race.IsCorpse &&
                    !hashSet.Contains(allDef.race))
                {
                    AutoSlaughterConfig config = new AutoSlaughterConfig() { animal = allDef.race };



                    AutoSlaughterSettings.Add(config);
                }
            }
        }

        private Dictionary<PawnKindDef, float> breedingProgress = new();

        public Dictionary<PawnKindDef, float> BreedingProgress
        {
            get
            {
                if (breedingProgress == null) breedingProgress = new();
                return breedingProgress;
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Collections.Look(ref breedingProgress, "breedingProgress", LookMode.Def);

            Scribe_Collections.Look(ref AutoSlaughterSettings, "AutoSlaughterSettings", LookMode.Deep);
            if (Scribe.mode != LoadSaveMode.PostLoadInit)
                return;
            if (AutoSlaughterSettings == null)
                AutoSlaughterSettings = new List<AutoSlaughterConfig>();
            if (AutoSlaughterSettings.RemoveAll(x => x.animal == null || x.animal.IsCorpse) != 0)
                Log.Warning("Some auto-slaughter configs had null animals after loading.");
            TryPopulateMissingAnimals();
        }

        public void ExecutionInt(
            Pawn victim)
        {
            Parent.Release(victim);
            int num = Mathf.Max(GenMath.RoundRandom(victim.BodySize * (float) 8), 1);
            for (int index = 0; index < num; ++index)
                victim.health.DropBloodFilth();

            Find.HistoryEventsManager.RecordEvent(new HistoryEvent(HistoryEventDefOf.SlaughteredAnimal, parent.Named(HistoryEventArgsNames.Doer)));
            BodyPartRecord bodyPartRecord = ExecutionUtility.ExecuteCutPart(victim);
            int partHealth = (int) victim.health.hediffSet.GetPartHealth(bodyPartRecord);
            int amount = Mathf.Min(partHealth - 1, 1);

            DamageInfo dinfo = new DamageInfo(DamageDefOf.ExecutionCut, (float) amount, 999f, instigator: parent, hitPart: bodyPartRecord, instigatorGuilty: false, spawnFilth: true);
            victim.TakeDamage(dinfo);
            if (!victim.Dead)
                victim.Kill(new DamageInfo?(dinfo), (Hediff) null);
            SoundDefOf.Execute_Cut.PlayOneShot((SoundInfo) (Thing) victim);
        }

        public override void CompTick()
        {
            base.CompTick();

            if (!PawnStoragesMod.settings.AllowNeedsDrop) return;


            if (parent.IsHashIntervalTick(Parent.TickInterval))
            {
                var types = (from p in Parent.BreedablePawns
                    group p by p.kindDef
                    into def
                    select def).ToList();

                if (parent.IsHashIntervalTick(Parent.TickInterval * 10))
                {

                    ProfilerBlock profilerBlock = new ProfilerBlock("Check AutoSlaughter");
                    foreach (var type in types)
                    {
                        AutoSlaughterConfig config = AutoSlaughterSettings.FirstOrDefault(s => s.animal == type.Key.race);

                        if (config != null)
                        {
                            // Only slaughter one per cycle
                            var adultMales = type.Where(p => p.gender == Gender.Male && p.ageTracker.Adult).ToList();
                            var adultFemales = type.Where(p => p.gender == Gender.Female && p.ageTracker.Adult).ToList();
                            var youngMales = type.Where(p => p.gender == Gender.Male && !p.ageTracker.Adult).ToList();
                            var youngFemales = type.Where(p => p.gender == Gender.Female && !p.ageTracker.Adult).ToList();

                            if (config.maxMales > 0 && config.maxMales < adultMales.Count())
                            {
                                Pawn pawn = adultMales.OrderByDescending(p => p.ageTracker.ageBiologicalTicksInt).First();
                                ExecutionInt(pawn);
                                break;
                            }
                            else if (config.maxFemales > 0 && config.maxFemales < adultFemales.Count())
                            {
                                Pawn pawn = adultFemales.OrderByDescending(p => p.ageTracker.ageBiologicalTicksInt).First();
                                ExecutionInt(pawn);
                                break;
                            }
                            else if (config.maxMalesYoung > 0 && config.maxMalesYoung < youngMales.Count())
                            {
                                Pawn pawn = youngMales.OrderByDescending(p => p.ageTracker.ageBiologicalTicksInt).FirstOrDefault();
                                ExecutionInt(pawn);
                                break;
                            }
                            else if (config.maxFemalesYoung > 0 && config.maxFemalesYoung < youngFemales.Count())
                            {
                                Pawn pawn = youngFemales.OrderByDescending(p => p.ageTracker.ageBiologicalTicksInt).First();
                                ExecutionInt(pawn);
                                break;
                            }
                            else if (config.maxTotal > 0 && config.maxTotal < type.Count())
                            {
                                Pawn pawn = type.OrderByDescending(p => p.ageTracker.ageBiologicalTicksInt).First();
                                ExecutionInt(pawn);
                                break;
                            }
                        }
                    }
                    profilerBlock.Dispose();
                }

                var deadPawns = Parent.BreedablePawns.Where(p => p.Dead);

                foreach (Pawn deadPawn in deadPawns)
                {
                    Log.Message(deadPawn);
                }

                if (Parent.BreedablePawns.Count < PawnStoragesMod.settings.MaxPawnsInFarm)
                {
                    foreach (var type in types)
                    {
                        var males = type.Any(p => p.gender == Gender.Male);
                        var females = type.Where(p => p.gender != Gender.Male).ToList();

                        if (!females.Any())
                        {
                            // no more females, reset
                            BreedingProgress[type.Key] = 0f;
                        }

                        // no males, stop progress
                        if (!males)
                        {
                            continue;
                        }

                        var gestationTicks = AnimalProductionUtility.GestationDaysEach(type.Key.race) * 60000 * PawnStoragesMod.settings.BreedingScale;

                        var progressPerCycle = Parent.TickInterval / gestationTicks;
                        progressPerCycle *= 25;


                        BreedingProgress.TryAdd(type.Key, 0.0f);

                        BreedingProgress[type.Key] = Mathf.Clamp(BreedingProgress[type.Key] + progressPerCycle * females.Count, 0f, 1f);

                        if (!(BreedingProgress[type.Key] >= 1f)) continue;

                        if (Parent.BreedablePawns.Count >= PawnStoragesMod.settings.MaxPawnsInFarm) break;

                        BreedingProgress[type.Key] = 0f;
                        var newPawn = PawnGenerator.GeneratePawn(
                            new PawnGenerationRequest(
                                type.Key,
                                Faction.OfPlayer,
                                allowDowned: true,
                                forceNoIdeo: true,
                                developmentalStages: DevelopmentalStage.Newborn
                            ));

                        Parent.Notify_PawnBorn(newPawn);
                    }
                }
            }
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (!DebugSettings.ShowDevGizmos) yield break;
            yield return new Command_Action
            {
                defaultLabel = "Make breeding progress 100%",
                action = delegate
                {
                    foreach (var thing in BreedingProgress.Keys)
                    {
                        BreedingProgress[thing] = 1f;
                    }
                },
                icon = ContentFinder<Texture2D>.Get("UI/Buttons/ReleaseAll")
            };
        }
    }
}
