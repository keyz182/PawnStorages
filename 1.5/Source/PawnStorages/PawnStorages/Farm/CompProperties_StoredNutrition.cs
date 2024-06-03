using Verse;

namespace PawnStorages.Farm
{
    public class CompProperties_StoredNutrition: CompProperties
    {
        public float maxNutrtition = 500f;
        public int ticksToAbsorbNutrients = 50;
        public int animalTickInterval = 250; 
        public CompProperties_StoredNutrition()
        {
            compClass = typeof(CompStoredNutrition);
        }
    }
}
