using HarmonyLib;
using RimWorld;
using Verse;

namespace PawnStorages;

[HarmonyPatch(typeof(WorldInterface), nameof(WorldInterface.WorldInterfaceOnGUI))]
public class WorldInterface_Patch
{
    [HarmonyPostfix]
    private static void Postfix(WorldInterface __instance)
    {
        if (!Find.UIRoot.screenshotMode.FiltersCurrentEvent)
            StoredPawnBar.StoredPawnBar.Bar.ColonistBarOnGUI();
    }
}