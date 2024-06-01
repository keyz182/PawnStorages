using Mono.Unix.Native;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;

namespace PawnStorages.Farm
{
    [StaticConstructorOnStartup]
    public class CompFarmStorageRefuelable : CompRefuelable, ISuspendableThingHolder, IStoreSettingsParent
    {

        public ThingOwner innerContainer;
        public StorageSettings allowedNutritionSettings;
        public bool StorageTabVisible => true;


        private CompAssignableToPawn_PawnStorageFarm compAssignable;

        private List<Pawn> storedPawns;

        public CompFarmStorageRefuelable()
        {
            storedPawns = [];
            innerContainer = new ThingOwner<Thing>(this);
        }
        public Thing Parent => parent;

        public new CompProperties_FarmStorageRefuelable Props => props as CompProperties_FarmStorageRefuelable;
        public List<Pawn> StoredPawns { get; set; }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            PS_Farm_MapComponent mapComponent = parent.Map.GetComponent<PS_Farm_MapComponent>();
            if (mapComponent != null)
            {
                mapComponent.comps.Add(this);
            }

            base.PostSpawnSetup(respawningAfterLoad);
            compAssignable = parent.TryGetComp<CompAssignableToPawn_PawnStorageFarm>();
        }

        public override void PostDeSpawn(Map map)
        {

            PS_Farm_MapComponent mapComponent = map.GetComponent<PS_Farm_MapComponent>();
            if (mapComponent != null)
            {
                mapComponent.comps.Remove(this);
            }

            EjectContents(map);
            base.PostDeSpawn(map);
        }

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            allowedNutritionSettings = new StorageSettings(this);

            if (parent.def.building.defaultStorageSettings != null)
            {
                allowedNutritionSettings.CopyFrom(parent.def.building.defaultStorageSettings);
            }
        }

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
        }

        public ThingOwner GetDirectlyHeldThings() => innerContainer;

        public bool IsContentsSuspended { get; }

        public StorageSettings GetStoreSettings()
        {
            return allowedNutritionSettings;
        }
        public StorageSettings GetParentStoreSettings()
        {
            return parent.def.building.fixedStorageSettings;
        }

        public void Notify_SettingsChanged()
        {
        }

        public void EjectAndKillContents(Map map)
        {
            ThingDef filth_Slime = ThingDefOf.Filth_Slime;
            foreach (Thing item in innerContainer)
            {
                Pawn pawn = item as Pawn;
                if (pawn != null)
                {
                    PawnComponentsUtility.AddComponentsForSpawn(pawn);
                    pawn.filth.GainFilth(filth_Slime);
                    pawn.Kill(null);
                }
            }
            innerContainer.TryDropAll(parent.InteractionCell, map, ThingPlaceMode.Near);

        }

        public void EjectContents(Map map)
        {
            if (map == null) map = parent.Map;

            innerContainer.TryDropAll(parent.InteractionCell, map, ThingPlaceMode.Near);
            FilthMaker.TryMakeFilth(parent.InteractionCell, map, ThingDefOf.Filth_Slime, new IntRange(3, 6).RandomInRange);
        }


        public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
        {
            foreach (FloatMenuOption floatMenuOption in base.CompFloatMenuOptions(selPawn))
            {
                yield return floatMenuOption;
            }
        }

        public override string CompInspectStringExtra()
        {
            StringBuilder sb = new(base.CompInspectStringExtra());
            sb.Append("\nTODO: Add nutrition deets");

            return sb.ToString().TrimStart().TrimEnd();
        }
    }
}
