using Verse;

namespace PawnStorages.Farm.Interfaces;

public interface INutritionStoreAlternative
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
