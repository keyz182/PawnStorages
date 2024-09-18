using System;
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
    public static IEnumerable<Gizmo> CompGetGizmosExtra_Patch(IEnumerable<Gizmo> __result, CompPawnStorage __instance)
    {
        foreach (Gizmo g in __result) yield return g;
        Lazy<Command_Action> action = new Lazy<Command_Action>(() => new Command_Action
        {
            defaultLabel = "PS_ReleaseAllToDefensivePositions".Translate(),
            defaultDesc = "PS_ReleaseAllToDefensivePositions_Tooltop".Translate(),
            icon = ContentFinder<Texture2D>.Get("UIPositionLargeActive", true)
        });
        action.Value.action = delegate
        {

            for (int num = __instance.innerContainer.Count - 1; num >= 0; num--)
            {
                Pawn pawn = (Pawn) __instance.GetAt(num);
                Utility.ReleasePawn(__instance, pawn, __instance.parent.Position, __instance.parent.Map);

                PawnSavedPositionHandler mgr = DefensivePositionsManager.Instance.GetHandlerForPawn(pawn);

                Lazy<MethodInfo> hasSavedPositionMethod = new(()=>typeof(PawnSavedPositionHandler).GetMethod("HasSavedPosition", BindingFlags.NonPublic | BindingFlags.Instance));
                Lazy<MethodInfo> getPositionMethod = new(()=>typeof(PawnSavedPositionHandler).GetMethod("GetPosition", BindingFlags.NonPublic | BindingFlags.Instance));
                Lazy<MethodInfo> draftToPositionMethod = new(()=>typeof(PawnSavedPositionHandler).GetMethod("DraftToPosition", BindingFlags.NonPublic | BindingFlags.Instance));

                if (hasSavedPositionMethod.Value == null || getPositionMethod.Value == null || draftToPositionMethod.Value == null || !(bool) hasSavedPositionMethod.Value.Invoke(mgr, [0]))
                {
                    continue;
                }

                IntVec3 pos = (IntVec3) getPositionMethod.Value.Invoke(mgr, [0]);
                draftToPositionMethod.Value.Invoke(mgr, [pos]);
                DefensivePositionsManager.Instance.Reporter.ReportPawnInteraction(ScheduledReportManager.ReportType.SentToSavedPosition, pawn, true, 0);
            }
        };
        yield return action.Value;
    }
}
