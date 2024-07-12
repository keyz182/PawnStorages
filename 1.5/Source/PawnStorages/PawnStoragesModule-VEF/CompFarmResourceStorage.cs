
using PawnStorages.Farm.Comps;
using Verse;

namespace PawnStorages.VEF;

public class CompFarmResourceStorage : PipeSystem.CompResourceStorage, INutritionStoreAlternative
{
    public int Id = -1;
    public float MaxStoreSize { get => AmountCanAccept; }

    public float CurrentStored
    {
        get => AmountStored;
        set
        {
            float toStore = value - CurrentStored;
            if (toStore < 0f)
                DrawResource(-toStore);
            else
                AddResource(toStore);
        }
    }

    public void Initialize()
    {
        if (this.Id == -1)
            this.Id = Find.UniqueIDsManager.GetNextThingID();
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look<int>(ref this.Id, "Id", -1);
        if (Scribe.mode == LoadSaveMode.LoadingVars)
        {
            if (this.Id == -1)
                this.Id = Find.UniqueIDsManager.GetNextThingID();
            this.Initialize();
        }
    }

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        CompFarmNutrition nutrition = parent.GetComp<CompFarmNutrition>();
        nutrition?.SetAlternativeStore(this);
    }

    public override void CompTick()
    {
        base.CompTick();

        if (Props is CompProperties_FarmResourceStorage p)
            p.storageCapacity = PawnStoragesMod.settings.MaxFarmStoredNutrition;
    }

    public string GetUniqueLoadID() => "CompFarmResourceStorage_" + (object) this.Id;
}
