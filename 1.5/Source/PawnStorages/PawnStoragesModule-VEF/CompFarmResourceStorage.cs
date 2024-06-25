
using PawnStorages.Farm.Comps;

namespace PawnStorages.VEF;

public class CompFarmResourceStorage : PipeSystem.CompResourceStorage, INutritionStoreAlternative
{
    public float MaxStoreSize { get => AmountCanAccept; }

    public float CurrentStored
    {
        get => AmountStored;
        set
        {
            float toStore = value - CurrentStored;
            if (toStore < 0f)
                DrawResource(-toStore);
            else
                AddResource(toStore);
        }
    }

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        CompFarmNutrition nutrition = parent.GetComp<CompFarmNutrition>();
        nutrition?.SetAlternativeStore(this);
    }

    public override void CompTick()
    {
        base.CompTick();

        if (Props is CompProperties_FarmResourceStorage p)
            p.storageCapacity = PawnStoragesMod.settings.MaxFarmStoredNutrition;
    }
}
