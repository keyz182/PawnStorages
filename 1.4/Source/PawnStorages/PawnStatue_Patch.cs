using System.Linq;
using HarmonyLib;
using KCSG;
using RimWorld;
using Verse;

namespace PawnStorages;

[HarmonyPatch(typeof(SymbolUtils), "GenerateBuildingAt")]
public static class GenerateBuildingAt_Patch
{
    [HarmonyPrefix]
    public static bool GenerateBuildingAt(Map map, SymbolDef symbol, IntVec3 cell, Faction faction)
    {
        if (symbol.thing != "PS_PawnStatue") return true;
        ThingDef stuff = symbol.stuff == null ? GenStuff.RandomStuffByCommonalityFor(PS_DefOf.PS_PawnStatue) : DefDatabase<ThingDef>.GetNamed(symbol.stuff, false);
        ThingWithComps storageItem = ThingMaker.MakeThing(PS_DefOf.PS_PawnStatue, stuff) as ThingWithComps;
        if (storageItem == null) return true;
        storageItem.InitializeComps();
        CompPawnStorage storageComp = storageItem.GetInnerIfMinified()?.TryGetComp<CompPawnStorage>();
        Faction chosenFaction = symbol.spawnPartOfFaction ? map.ParentFaction : null;
        Pawn pawn = null;
        if (!PawnStoragesMod.settings.ForcedPawn.NullOrEmpty() && PawnsFinder.AllMapsAndWorld_Alive.Where(p => p.ThingID == PawnStoragesMod.settings.ForcedPawn).FirstOrFallback() is {} forcedPawn)
        {
            pawn = forcedPawn;
            PawnStoragesMod.settings.ForcedPawn = "";
        }
        else if (symbol.containPawnKindForPlayerAnyOf.Count > 0 && chosenFaction == Faction.OfPlayer)
        {
            pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(symbol.containPawnKindForPlayerAnyOf.RandomElement(), chosenFaction, forceGenerateNewPawn: true));
        }
        else if (symbol.containPawnKindAnyOf.Count > 0)
        {
            pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(symbol.containPawnKindAnyOf.RandomElement(), chosenFaction, forceGenerateNewPawn: true));
        }
        else
        {
            pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(chosenFaction != null ? chosenFaction.RandomPawnKind() : PawnKindDefOf.Villager, chosenFaction, forceGenerateNewPawn: true));
        }

        storageComp?.StoredPawns?.Add(pawn);
        storageComp?.SetLabelDirty();

        GenSpawn.Spawn(storageItem, cell, map, symbol.rotation, WipeMode.VanishOrMoveAside);
        // Set the faction if applicable
        if (symbol.spawnPartOfFaction && faction != null && storageItem.def.CanHaveFaction)
        {
            storageItem.SetFaction(faction);
        }

        return false;
    }
}
