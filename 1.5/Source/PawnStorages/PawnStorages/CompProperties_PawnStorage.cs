using Verse;

namespace PawnStorages;

public class CompProperties_PawnStorage : CompProperties
{
    public bool appendOfName;
    public bool canBeScheduled;
    public bool convertOption;
    public bool idleResearch;
    public bool needsDrop;
    public bool lightEffect;
    public int maxStoredPawns = 1;
    public float pawnRestIncreaseTick;
    public bool releaseAllOption;
    public EffecterDef releaseEffect;
    public bool releaseOption;
    public bool allowNonColonist;

    public bool showStoredPawn;

    public ThingDef storageStation;

    public EffecterDef storeEffect;
    public bool transformEffect;

    public CompProperties_PawnStorage()
    {
        compClass = typeof(CompPawnStorage);
    }
}
