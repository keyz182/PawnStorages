using System.Collections.Generic;
using System.Linq;
using System.Text;
using PawnStorages.Farm.Comps;
using PawnStorages.Farm.Interfaces;
using RimWorld;
using Verse;

namespace PawnStorages.Farm
{
    public class Building_PSFarm : Building, IStoreSettingsParent, INutritionStorageParent, IBreederParent, IProductionParent
    {
        public CompFarmStorage pawnStorage;
        public CompFarmNutrition FarmNutrition;
        private StorageSettings allowedNutritionSettings;

        public bool NutritionAvailable = true;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            pawnStorage = GetComp<CompFarmStorage>();
            FarmNutrition = GetComp<CompFarmNutrition>();
            base.SpawnSetup(map, respawningAfterLoad);
            allowedNutritionSettings = new StorageSettings(this);
            if (def.building.defaultStorageSettings == null)
                return;
            allowedNutritionSettings.CopyFrom(def.building.defaultStorageSettings);
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
                yield return gizmo;
            Designator_Build allowedDesignator = BuildCopyCommandUtility.FindAllowedDesignator(ThingDefOf.Hopper);
            if (allowedDesignator != null)
                yield return allowedDesignator;
        }

        public override string GetInspectString()
        {
            StringBuilder stringBuilder = new();
            stringBuilder.AppendLine(base.GetInspectString());
            stringBuilder.AppendLine("PS_NutritionPerDay".Translate(pawnStorage.NutritionRequiredPerDay()));
            stringBuilder.AppendLine("PS_NutritionStored".Translate(FarmNutrition.storedNutrition, FarmNutrition.Props.maxNutrition));
            return stringBuilder.ToString().Trim();
        }

        public StorageSettings GetStoreSettings()
        {
            return allowedNutritionSettings;
        }

        public StorageSettings GetParentStoreSettings()
        {
            return def.building.fixedStorageSettings;
        }

        public void Notify_SettingsChanged()
        {
        }

        public bool StorageTabVisible => true;

        public bool IsActive => NutritionAvailable;
        public void ReleasePawn(Pawn pawn)
        {
            pawnStorage.ReleaseSingle(this.Map, pawn, true, true);
        }

        public bool HasSuggestiveSilos => true;
        public bool HasStoredPawns => true;
        public List<Pawn> StoredPawns => pawnStorage.StoredPawns;
        public void Notify_NutrtitionEmpty() => NutritionAvailable = false;

        public void Notify_NutrtitionNotEmpty() => NutritionAvailable = true;

        public List<Pawn> BreedablePawns => pawnStorage.StoredPawns.Where(p => p.ageTracker.Adult && !p.health.Dead && !p.health.Downed).ToList();
        public List<Pawn> ProducingPawns => pawnStorage.StoredPawns
            .Where(p => p.ageTracker.Adult && !p.health.Dead && !p.health.Downed).ToList();

        public int TickInterval => 250;
        public void Notify_PawnBorn(Pawn newPawn)
        {
            pawnStorage.StorePawn(newPawn);
        }
    }
}
