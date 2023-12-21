using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace PawnStorages;

[HarmonyPatch(typeof(ITab_Pawn_Character), "PawnToShowInfoAbout", MethodType.Getter)]
public static class ITab_Pawn_Character_PawnToShowInfoAbout_Patch
{
    public static bool Prefix(ref Pawn __result)
    {
        CompPawnStorage comp = Find.Selector.SingleSelectedThing.TryGetComp<CompPawnStorage>();
        if (comp == null || !comp.StoredPawns.Any()) return true;
        __result = comp.StoredPawns.First();
        return false;

    }
}
