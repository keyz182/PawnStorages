namespace PawnStorages.Farm.Comps;

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