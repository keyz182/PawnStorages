using PawnStorages.Farm.Interfaces;
using PipeSystem;

namespace PawnStorages.VEF;

public class CompNutritionNetStorage : CompResourceStorage, INutritionStoreAlternative
{
    public virtual CompProperties_NutritionNetStorage CompProps => (CompProperties_NutritionNetStorage)props;

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
}
