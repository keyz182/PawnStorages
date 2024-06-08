using UnityEngine;
using HarmonyLib;
using RimWorld;
using Verse;

namespace PawnStorages.StoredPawnBar;

[HarmonyPatch(typeof(ColonistBarDrawLocsFinder), nameof(ColonistBarDrawLocsFinder.GetDrawLoc))]
public class StoredPawnBarDrawLocsFinder_Patch
{
    public static bool Prefix(ref Vector2 __result, 
        ColonistBarDrawLocsFinder __instance, 
        ref float groupStartX,
        ref float groupStartY,
        ref int group,
        ref int numInGroup,
        ref float scale)
    {
        if (__instance is StoredPawnBarDrawLocsFinder locsFinder)
        {
            __result = locsFinder.GetDrawLoc_Override(groupStartX, groupStartY, group, numInGroup, scale);
            return false;
        }

        return true;
    }
}

[HarmonyPatch(typeof(ColonistBarDrawLocsFinder), nameof(ColonistBarDrawLocsFinder.ColonistBar), MethodType.Getter)]
public class StoredPawnBarDrawLocsFinder_Bar_Patch
{
    public static bool Prefix(ref ColonistBar __result, ColonistBarDrawLocsFinder __instance)
    {
        if (__instance is StoredPawnBarDrawLocsFinder finder)
        {
            __result = finder.ColonistBarOverride;
        }
        else
        {
            __result = Find.ColonistBar;
        }

        return false;
    }
}

