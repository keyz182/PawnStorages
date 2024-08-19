using Verse;

namespace PawnStorages;

public class CompProperties_PawnStorageNutrition : CompProperties
{
    public float maxNutrition = 0;
    public int ticksToAbsorbNutrients = 0;
    public int pawnTickInterval = 0;
    public float MaxNutrition => maxNutrition > 0 ? maxNutrition : PawnStoragesMod.settings.MaxFarmStoredNutrition;
    public int TicksToAbsorbNutrients => ticksToAbsorbNutrients > 0 ? ticksToAbsorbNutrients : PawnStoragesMod.settings.TicksToAbsorbNutrients;
    public virtual int PawnTickInterval => pawnTickInterval > 0 ? pawnTickInterval : PawnStoragesMod.settings.AnimalTickInterval;

    public CompProperties_PawnStorageNutrition()
    {
        compClass = typeof(CompPawnStorageNutrition);
    }
}
