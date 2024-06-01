using RimWorld;
using Verse;

namespace PawnStorages.Farm;

public class CompProperties_FarmStorage : CompProperties_PawnStorage
{
    public CompProperties_FarmStorage()
    {
        compClass = typeof(CompFarmStorage);
    }
}
