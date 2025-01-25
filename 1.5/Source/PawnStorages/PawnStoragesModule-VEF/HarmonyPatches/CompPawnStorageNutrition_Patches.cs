using HarmonyLib;
using PipeSystem;
using RimWorld;
using UnityEngine;

namespace PawnStorages.VEF.HarmonyPatches;

[HarmonyPatch(typeof(CompPawnStorageNutrition))]
public static class CompPawnStorageNutrition_Patches
{
    public static bool IsAttachedToNet(CompPawnStorageNutrition comp, out PipeNet pipeNet, out CompResource resource)
    {
        resource = comp.parent.GetComp<CompResource>();
        pipeNet = null;
        if (resource is not { PipeNet: { } net }) return false;
        pipeNet = net;
        return pipeNet.connectors.Count > 1;
    }

    [HarmonyPatch(nameof(CompPawnStorageNutrition.MaxNutrition), MethodType.Getter)]
    [HarmonyPrefix]
    public static bool CompPawnStorageNutrition_MaxNutrition(CompPawnStorageNutrition __instance, ref float __result)
    {
        if (!IsAttachedToNet(__instance, out PipeNet pipeNet, out CompResource resource)) return true;

        __result = pipeNet.AvailableCapacity;

        return false;
    }

    [HarmonyPatch(nameof(CompPawnStorageNutrition.TryAbsorbNutritionFromHopper))]
    [HarmonyPrefix]
    public static bool TryAbsorbNutritionFromHopperPatch(CompPawnStorageNutrition __instance, ref bool __result, float nutrition)
    {
        if (!IsAttachedToNet(__instance, out PipeNet pipeNet, out CompResource resource)) return true;

        __result = !(pipeNet.Stored < nutrition);

        return false;
    }

    [HarmonyPatch(nameof(CompPawnStorageNutrition.AbsorbToFeedIfNeeded))]
    [HarmonyPrefix]
    public static bool AbsorbToFeedIfNeededPatch(CompPawnStorageNutrition __instance, ref bool __result, Need_Food foodNeeds, float desiredFeed, out float amountFed)
    {
        amountFed = 0;
        if (!IsAttachedToNet(__instance, out PipeNet pipeNet, out CompResource resource)) return true;

        amountFed = Mathf.Min(pipeNet.Stored, desiredFeed);
        pipeNet.DrawAmongStorage(amountFed, pipeNet.storages);
        __result = true;
        return false;
    }

    [HarmonyPatch(nameof(CompPawnStorageNutrition.storedNutrition), MethodType.Getter)]
    [HarmonyPostfix]
    public static void storedNutritionPatch(CompPawnStorageNutrition __instance, ref float __result)
    {
        if (!IsAttachedToNet(__instance, out PipeNet pipeNet, out CompResource resource)) return;
        __result = pipeNet.Stored;
    }

    [HarmonyPatch(nameof(CompPawnStorageNutrition.IsPiped), MethodType.Getter)]
    [HarmonyPostfix]
    public static void IsPipedPatch(CompPawnStorageNutrition __instance, ref bool __result)
    {
        if (!IsAttachedToNet(__instance, out PipeNet pipeNet, out CompResource resource)) return;
        __result = true;
    }
}
