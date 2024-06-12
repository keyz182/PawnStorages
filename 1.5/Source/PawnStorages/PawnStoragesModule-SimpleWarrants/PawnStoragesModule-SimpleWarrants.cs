using HarmonyLib;
using Verse;

namespace PawnStorages.SimpleWarrants;

public class PawnStoragesModule_SimpleWarrants : Mod
{
    public PawnStoragesModule_SimpleWarrants(ModContentPack content) : base(content)
    {
#if DEBUG
        Harmony.DEBUG = true;
#endif
        new Harmony("PawnStorages.Mod.SimpleWarrants").PatchAll();
    }
}
