using RimWorld;
using Verse;

namespace PawnStorages.Farm;

public class CompProperties_FarmStorageRefuelable : CompProperties_Refuelable
{
    public CompProperties_FarmStorageRefuelable()
    {
        compClass = typeof(CompFarmStorageRefuelable);
    }
}
