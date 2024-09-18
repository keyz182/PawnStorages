using System.Numerics;
using HarmonyLib;
using RimWorld;
using Verse;

namespace PawnStorages;

[HarmonyPatch(typeof(PawnRenderer))]
public static class PawnRenderer_Patch
{
    // [HarmonyPatch("GetBodyPos")]
    // [HarmonyPostfix]
    // public static void GetBodyPos_Patch(PawnRenderer __instance, ref bool showBody)
    // {
    //     if (__instance.pawn.holdingOwner is { owner: CompPawnStorage comp } && comp.Props.hideBodyWhenRenderingPawn)
    //     {
    //         showBody = true;
    //     }
    // }

    [HarmonyPatch("ParallelGetPreRenderResults")]
    [HarmonyPostfix]
    public static void ParallelGetPreRenderResults_Patch(ref PawnRenderer.PreRenderResults __result)
    {
        Log.Message("PawnRenderer::ParallelGetPreRenderResults");
        if (__result.parms.pawn.holdingOwner is { owner: CompPawnStorage comp } && comp.Props.hideBodyWhenRenderingPawn)
        {
            __result.showBody = false;
        }
    }
}
