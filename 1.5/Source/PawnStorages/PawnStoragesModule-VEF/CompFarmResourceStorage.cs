
using PawnStorages.Farm.Comps;
using PawnStorages.Farm.Interfaces;
using Verse;

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

    public override void CompTick()
    {
        base.CompTick();

        if (Props is CompProperties_FarmResourceStorage p)
            p.storageCapacity = PawnStoragesMod.settings.MaxFarmStoredNutrition;
    }
}
