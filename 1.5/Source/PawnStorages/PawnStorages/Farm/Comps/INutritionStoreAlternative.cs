using Verse;

namespace PawnStorages.Farm.Comps;

public interface INutritionStoreAlternative: ILoadReferenceable
{
    public float MaxStoreSize
    {
        get;
    }
    public float CurrentStored
    {
        get;
        set;
    }
}
