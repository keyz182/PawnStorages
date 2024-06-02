using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace PawnStorages.Farm
{
    public class CompProperties_StoredNutrition: CompProperties
    {
        public float maxNutrtition = 500f;
        public int ticksToAbsorbNutrients = 250;
        public int ticksToFeedAnimals = 60000; //Daily
        public CompProperties_StoredNutrition()
        {
            compClass = typeof(CompStoredNutrition);
        }
    }
}
