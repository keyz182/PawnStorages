using HarmonyLib;
using RimWorld;
using System.Linq;
using Verse;

namespace PawnStorages
{
    [HarmonyPatch(typeof(ITab_Pawn_Character), "PawnToShowInfoAbout", MethodType.Getter)]
    public static class ITab_Pawn_Character_PawnToShowInfoAbout_Patch
    {
        public static bool Prefix(ref Pawn __result)
        {
            var comp = Find.Selector.SingleSelectedThing.TryGetComp<CompPawnStorage>();
            if (comp != null && comp.storedPawns.Any())
            {
                __result = comp.storedPawns.First();
                return false;
            }
            return true;
        }
    }
}
