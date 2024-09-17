using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DefensivePositions;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnStorages.DefensivePositions;

[HarmonyPatch(typeof(CompPawnStorage))]
public static class CompPawnStorage_Patch
{
    [HarmonyPatch(nameof(CompPawnStorage.CompGetGizmosExtra))]
    [HarmonyPostfix]
    public static void CompGetGizmosExtra_Patch(CompPawnStorage __instance, ref IEnumerable<Gizmo> __result)
    {
        Command_Action action = new Command_Action();
        action.defaultLabel = "PS_ReleaseAllToDefensivePositions".Translate();
        action.defaultDesc = "PS_ReleaseAllToDefensivePositions_Tooltop".Translate();
        action.icon = ContentFinder<Texture2D>.Get("UIPositionLargeActive", true);

        action.action = delegate
        {

            for (int num = __instance.innerContainer.Count - 1; num >= 0; num--)
            {
                Pawn pawn = (Pawn) __instance.GetAt(num);
                __instance.innerContainer.Remove(pawn);
                GenSpawn.Spawn(pawn, __instance.parent.Position, __instance.parent.Map);
                __instance.parent.Map.mapDrawer.MapMeshDirty(__instance.parent.Position, MapMeshFlagDefOf.Things);
                PawnSavedPositionHandler mgr = DefensivePositionsManager.Instance.GetHandlerForPawn(pawn);

                MethodInfo hasSavedPositionMethod = typeof(PawnSavedPositionHandler).GetMethod("HasSavedPosition", BindingFlags.NonPublic | BindingFlags.Instance);
                MethodInfo getPositionMethod = typeof(PawnSavedPositionHandler).GetMethod("GetPosition", BindingFlags.NonPublic | BindingFlags.Instance);
                MethodInfo draftToPositionMethod = typeof(PawnSavedPositionHandler).GetMethod("DraftToPosition", BindingFlags.NonPublic | BindingFlags.Instance);

                if (hasSavedPositionMethod == null || getPositionMethod == null || draftToPositionMethod == null || !(bool) hasSavedPositionMethod.Invoke(mgr, [0]))
                {
                    continue;
                }

                IntVec3 pos = (IntVec3) getPositionMethod.Invoke(mgr, [0]);
                draftToPositionMethod.Invoke(mgr, [pos]);
                DefensivePositionsManager.Instance.Reporter.ReportPawnInteraction(ScheduledReportManager.ReportType.SentToSavedPosition, pawn, true, 0);
            }
        };

        List<Gizmo> listOut = __result.ToList();
        listOut.Add(action);
        __result = listOut.AsEnumerable();
    }
}
