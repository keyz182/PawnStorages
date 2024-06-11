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
        var holder = Find.Selector.SingleSelectedThing as IThingHolder;
        
        if (comp == null || !holder.GetDirectlyHeldThings().Any()) return true;
        __result = (Pawn)holder.GetDirectlyHeldThings().First();
        return false;

    }
}
