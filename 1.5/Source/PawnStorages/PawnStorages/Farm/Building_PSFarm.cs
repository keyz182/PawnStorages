using RimWorld;
using System.Collections.Generic;
using System.Text;
using Verse;

namespace PawnStorages.Farm
{
    public class Building_PSFarm : Building, IStoreSettingsParent
    {
        public CompFarmStorage pawnStorage;
        public CompStoredNutrition StoredNutrition;
        private StorageSettings allowedNutritionSettings;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            pawnStorage = GetComp<CompFarmStorage>();
            StoredNutrition = GetComp<CompStoredNutrition>();
            base.SpawnSetup(map, respawningAfterLoad);
            this.allowedNutritionSettings = new StorageSettings((IStoreSettingsParent)this);
            if (this.def.building.defaultStorageSettings == null)
                return;
            this.allowedNutritionSettings.CopyFrom(this.def.building.defaultStorageSettings);
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
                yield return gizmo;
            Designator_Build allowedDesignator = BuildCopyCommandUtility.FindAllowedDesignator((BuildableDef)ThingDefOf.Hopper);
            if (allowedDesignator != null)
                yield return (Gizmo)allowedDesignator;
        }

        public override string GetInspectString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine(base.GetInspectString());
            stringBuilder.AppendLine("PS_NutritionPerDay".Translate(pawnStorage.NutritionRequiredPerDay()));
            stringBuilder.AppendLine("PS_NutritionStored".Translate(StoredNutrition.storedNutrition, StoredNutrition.Props.maxNutrtition));
            return stringBuilder.ToString().Trim();
        }

        public StorageSettings GetStoreSettings()
        {
            return this.allowedNutritionSettings;
        }

        public StorageSettings GetParentStoreSettings()
        {
            return this.def.building.fixedStorageSettings;
        }

        public void Notify_SettingsChanged()
        {
        }

        public bool StorageTabVisible => true;
    }
}
