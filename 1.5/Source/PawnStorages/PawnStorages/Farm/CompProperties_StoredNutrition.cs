using Verse;

namespace PawnStorages.Farm
{
    public class CompProperties_StoredNutrition : CompProperties
    {
        public float maxNutrition = 500f;
        public int ticksToAbsorbNutrients = 50;
        public int animalTickInterval = 250;
        public float produceTimeScale = 0.75f;

        public CompProperties_StoredNutrition()
        {
            compClass = typeof(CompStoredNutrition);
        }
    }
}
