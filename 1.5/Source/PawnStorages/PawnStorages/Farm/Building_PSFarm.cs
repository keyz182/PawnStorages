using System.Collections.Generic;
using System.Linq;
using System.Text;
using PawnStorages.Farm.Comps;
using PawnStorages.Farm.Interfaces;
using RimWorld;
using Verse;

namespace PawnStorages.Farm
{
    public class Building_PSFarm : Building, IStoreSettingsParent, INutritionStorageParent, IBreederParent, IProductionParent, IFarmTabParent, IThingHolder
    {
        public CompFarmStorage pawnStorage;
        public CompFarmNutrition FarmNutrition;
        public CompFarmBreeder FarmBreeder;
        public CompFarmProducer FarmProducer;
        private StorageSettings allowedNutritionSettings;

        public ThingOwner GetDirectlyHeldThings() => this.innerContainer;

        protected Dictionary<ThingDef, bool> allowedThings;

        public Dictionary<ThingDef, bool> AllowedThings => allowedThings;

        public ThingOwner innerContainer;
        public Building_PSFarm()
        {
            this.innerContainer = new ThingOwner<Pawn>((IThingHolder) this);
        }

        public bool Allowed(ThingDef potentialDef)
        {
            return allowedThings.GetValueOrDefault(potentialDef, false);
        }

        public bool IsBreeder => FarmBreeder != null;
        public bool IsProducer => FarmProducer != null;

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref allowedThings, "allowedThings", LookMode.Def);
            Scribe_Deep.Look<ThingOwner>(ref this.innerContainer, "innerContainer", (object) this);
            base.ExposeData();
        }

        public void AllowAll()
        {
            foreach (var allowedThingsKey in AllowedThings.Keys)
            {
                AllowedThings[allowedThingsKey] = true;
            }
        }

        public void DenyAll()
        {
            foreach (var allowedThingsKey in AllowedThings.Keys)
            {
                AllowedThings[allowedThingsKey] = false;
            }
        }

        public bool NutritionAvailable = true;

        public List<ThingDef> AllowableThing => Utility.Animals(IsProducer);

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            pawnStorage = GetComp<CompFarmStorage>();
            FarmNutrition = GetComp<CompFarmNutrition>();
            FarmBreeder = GetComp<CompFarmBreeder>();
            FarmProducer = GetComp<CompFarmProducer>();
            base.SpawnSetup(map, respawningAfterLoad);
            allowedNutritionSettings = new StorageSettings(this);
            if (def.building.defaultStorageSettings == null)
                return;
            allowedNutritionSettings.CopyFrom(def.building.defaultStorageSettings);

            allowedThings ??= new();

            foreach (var thingDef in AllowableThing.Where(t => !allowedThings.Keys.Contains(t)))
            {
                AllowedThings[thingDef] = true;
            }
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
                yield return gizmo;
            Designator_Build allowedDesignator = BuildCopyCommandUtility.FindAllowedDesignator(ThingDefOf.Hopper);
            if (allowedDesignator != null)
                yield return allowedDesignator;
            foreach (Thing thing in (IEnumerable<Thing>) innerContainer)
            {
                Gizmo gizmo;
                if ((gizmo = Building.SelectContainedItemGizmo(thing, thing)) != null)
                    yield return gizmo;
            }
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
        public List<Pawn> StoredPawns => innerContainer.Select(p=>p as Pawn).ToList();

        public void Notify_NutrtitionEmpty() => NutritionAvailable = false;

        public void Notify_NutrtitionNotEmpty() => NutritionAvailable = true;

        public List<Pawn> BreedablePawns
        {
            get
            {
                if (!IsBreeder) return [];
                 return innerContainer.Select(p => p as Pawn).Where<Pawn>(pawn => pawn.ageTracker.Adult && !pawn.health.Dead && !pawn.health.Downed).ToList();
            }
        }

        public List<Pawn> ProducingPawns
        {
            get
            {
                if (!IsProducer) return [];
                return innerContainer.Select(p => p as Pawn).Where<Pawn>(pawn => pawn.ageTracker.Adult && !pawn.health.Dead && !pawn.health.Downed).ToList();
            }
        }

        public int TickInterval => 250;

        public void Notify_PawnBorn(Pawn newPawn)
        {
            pawnStorage.StorePawn(newPawn);
        }
        
        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, (IList<Thing>) this.GetDirectlyHeldThings());
        }
        
    }
}
