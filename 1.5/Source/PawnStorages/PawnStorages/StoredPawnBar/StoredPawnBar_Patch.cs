using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace PawnStorages.StoredPawnBar;

[HarmonyPatch(typeof(ColonistBar))]
public static class StoredPawnBar_Patch
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(ColonistBar.Entries), MethodType.Getter)]
    public static bool Entries_Patch(ref List<ColonistBar.Entry> __result, ColonistBar __instance)
    {
        if (__instance is StoredPawnBar bar)
        {
            bar.CheckRecacheEntries_Override();
            __result = bar.cachedEntries;
        }
        else
        {
            __instance.CheckRecacheEntries();
            __result = __instance.cachedEntries;
        }

        return false;
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(ColonistBar.CaravanMemberCaravanAt))]
    public static void Highlight_Patch(ColonistBar __instance, Vector2 at)
    {
        StoredPawnBar.Bar.CaravanMemberCaravanAt(at);
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(ColonistBar.TryGetEntryAt))]
    public static void TryGetEntryAt_Patch(ref bool __result, ColonistBar __instance, Vector2 pos, ref ColonistBar.Entry entry)
    {
        if (!__result)
        {
            __result = StoredPawnBar.Bar.TryGetEntryAt(pos, out entry);
        }
    }
}