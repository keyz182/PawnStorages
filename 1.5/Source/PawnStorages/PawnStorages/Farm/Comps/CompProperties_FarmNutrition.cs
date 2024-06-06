using Verse;

namespace PawnStorages.Farm.Comps
{
    public class CompProperties_FarmNutrition : CompProperties
    {
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
