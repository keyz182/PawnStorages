using HarmonyLib;
using Verse;

namespace PawnStorages
{
    [HarmonyPatch(typeof(GenRecipe))]
    [HarmonyPatch("PostProcessProduct")]
    public static class Patch_PostProcessProduct
    {
        [HarmonyPostfix]
        public static void Postfix(ref Thing product, RecipeDef recipeDef, Pawn worker)
        {
            if (recipeDef == PS_DefOf.PS_Make_PawnDisc)
            {
                var comp = product.TryGetComp<CompPawnStorage>();
                comp.toStore = worker;
            }
        }
    }
}
