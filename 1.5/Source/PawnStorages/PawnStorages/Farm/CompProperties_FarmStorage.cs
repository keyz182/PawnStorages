namespace PawnStorages.Farm;

public class CompProperties_FarmStorage : CompProperties_PawnStorage
{
    public int ticksToProduce = 60000;  // Daily
    public bool alwaysProduce = false; //Produce even if no nutrients

    public CompProperties_FarmStorage()
    {
        compClass = typeof(CompFarmStorage);
    }
}
