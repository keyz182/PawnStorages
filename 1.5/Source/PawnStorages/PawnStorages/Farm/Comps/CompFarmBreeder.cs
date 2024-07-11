﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PawnStorages.Farm.Interfaces;
using RimWorld;
using UnityEngine;
using Verse;

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
                    AutoSlaughterSettings.Add(new AutoSlaughterConfig() { animal = allDef.race });
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
            if (AutoSlaughterSettings.RemoveAll(x => x.animal == null || x.animal.IsCorpse) != 0)
                Log.Warning("Some auto-slaughter configs had null animals after loading.");
            TryPopulateMissingAnimals();
        }

        public override void CompTick()
        {
            base.CompTick();

            if (!PawnStoragesMod.settings.AllowNeedsDrop) return;


            if (parent.IsHashIntervalTick(Parent.TickInterval))
            {
                if (Parent.BreedablePawns.Count < PawnStoragesMod.settings.MaxPawnsInFarm)
                {
                    var types = from p in Parent.BreedablePawns
                        group p by p.kindDef
                        into def
                        select def;

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

        public AutoSlaughterGizmo _autoSlaughterGizmo;

        public AutoSlaughterGizmo AutoSlaughterGizmo {
            get
            {
                if (_autoSlaughterGizmo is null)
                    _autoSlaughterGizmo = new AutoSlaughterGizmo(this);

                return _autoSlaughterGizmo;
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
            yield return AutoSlaughterGizmo;
        }

        public override string CompInspectStringExtra()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("PS_AutoSlaughterDesc".Translate(this.AutoSlaughterTarget));
            return sb.ToString();
        }
    }
}
