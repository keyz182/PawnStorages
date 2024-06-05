namespace PawnStorages.Farm;

public class CompProperties_FarmStorage : CompProperties_PawnStorage
{
    public int maxStoredPawns => PawnStoragesMod.settings.MaxPawnsInFarm;
    public CompProperties_FarmStorage()
    {
        compClass = typeof(CompFarmStorage);
    }
}
