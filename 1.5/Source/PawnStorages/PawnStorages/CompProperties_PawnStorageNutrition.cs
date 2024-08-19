using Verse;

namespace PawnStorages;

public class CompProperties_PawnStorageNutrition : CompProperties
{
    public float maxNutrition => PawnStoragesMod.settings.MaxFarmStoredNutrition;
    public int ticksToAbsorbNutrients => PawnStoragesMod.settings.TicksToAbsorbNutrients;
    public int pawnTickInterval => PawnStoragesMod.settings.AnimalTickInterval;

    public CompProperties_PawnStorageNutrition()
    {
        compClass = typeof(CompPawnStorageNutrition);
    }
}
