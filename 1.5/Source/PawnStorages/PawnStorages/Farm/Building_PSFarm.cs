using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace PawnStorages.Farm
{
    public class Building_PSFarm : Building
    {
        public static bool IsAcceptableFeedstock(ThingDef def)
        {
            return def.IsNutritionGivingIngestible && def.ingestible.preferability != FoodPreferability.Undefined && (def.ingestible.foodType & FoodTypeFlags.Plant) != FoodTypeFlags.Plant && (def.ingestible.foodType & FoodTypeFlags.Tree) != FoodTypeFlags.Tree;
        }
    }
}
