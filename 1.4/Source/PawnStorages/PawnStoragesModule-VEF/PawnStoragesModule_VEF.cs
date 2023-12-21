using HarmonyLib;
using Verse;

namespace PawnStorages.VEF;

public class PawnStoragesModule_VEF : Mod
{
    public PawnStoragesModule_VEF(ModContentPack content) : base(content)
    {
#if DEBUG
        Harmony.DEBUG = true;
#endif
        new Harmony("PawnStorages.Mod.VEF").PatchAll();
    }
}
