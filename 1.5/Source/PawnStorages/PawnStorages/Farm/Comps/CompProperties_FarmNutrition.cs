using Verse;

namespace PawnStorages.Farm.Comps
{
    public class CompProperties_FarmNutrition : CompProperties
    {
        // production
        public float maxNutrition => PawnStoragesMod.settings.MaxFarmStoredNutrition;
        public int ticksToAbsorbNutrients => PawnStoragesMod.settings.TicksToAbsorbNutrients;
        public int animalTickInterval => PawnStoragesMod.settings.AnimalTickInterval;

        public CompProperties_FarmNutrition()
        {
            compClass = typeof(CompFarmNutrition);
        }
    }
}
