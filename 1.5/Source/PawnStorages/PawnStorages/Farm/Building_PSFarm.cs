using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace PawnStorages.Farm
{
    public class Building_PSFarm : Building, IHaulDestination
    {
        public bool StorageTabVisible => true;

        public StorageSettings allowedNutritionSettings;

        public override void PostMake()
        {
            base.PostMake();
            this.allowedNutritionSettings = new StorageSettings((IStoreSettingsParent)this);
            if (this.def.building.defaultStorageSettings != null)
                this.allowedNutritionSettings.CopyFrom(this.def.building.defaultStorageSettings);
        }

        public bool Accepts(Thing t) => this.GetStoreSettings().AllowedToAccept(t);
        
        public StorageSettings GetStoreSettings() => this.GetStoreSettings();
        
        public StorageSettings GetParentStoreSettings() => this.def.building.fixedStorageSettings;

        public void Notify_SettingsChanged()
        {
        }

    }
}
