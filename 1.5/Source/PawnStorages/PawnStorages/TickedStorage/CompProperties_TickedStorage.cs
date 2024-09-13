namespace PawnStorages.TickedStorage;

public class CompProperties_TickedStorage: CompProperties_PawnStorage
{
    public bool TickHediffs = true;
    public bool TickAge = false;
    public bool TickNutrition => needsDrop;

    public int TickInterval = 300;

    public CompProperties_TickedStorage()
    {
        compClass = typeof(CompTickedStorage);
    }
}
