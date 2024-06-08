using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnStorages;

[HarmonyPatch(typeof(MapInterface), "MapInterfaceOnGUI_BeforeMainTabs")]
public class MapInterface_Patch
{
    [HarmonyPostfix]
    private static void Postfix(MapInterface __instance)
    {
        if (!Find.UIRoot.screenshotMode.FiltersCurrentEvent)
            StoredPawnBar.StoredPawnBar.Bar.ColonistBarOnGUI();
    }
}