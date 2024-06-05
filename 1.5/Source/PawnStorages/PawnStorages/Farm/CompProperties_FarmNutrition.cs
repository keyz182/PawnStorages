using Verse;

namespace PawnStorages.Farm
{
    public class CompProperties_FarmNutrition : CompProperties
    {
        public bool doesProduction = true;
        public bool doesBreeding = false;

        // production
        public float maxNutrition = 500f;
        public int ticksToAbsorbNutrients = 50;
        public int animalTickInterval = 250;

        public CompProperties_FarmNutrition()
        {
            compClass = typeof(CompFarmNutrition);
        }
    }
}
