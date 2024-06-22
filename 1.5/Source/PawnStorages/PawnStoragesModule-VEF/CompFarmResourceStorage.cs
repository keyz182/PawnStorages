
using PawnStorages.Farm.Comps;

namespace PawnStorages.VEF;

public class CompFarmResourceStorage : PipeSystem.CompResourceStorage, INutritionStoreAlternative
{
    public float MaxStoreSize { get => AmountCanAccept; }

    public float CurrentStored
    {
        get => AmountStored;
        set
        {
            var toStore = value - CurrentStored;
            if (toStore < 0f)
                DrawResource(-toStore);
            else
                AddResource(toStore);
        }
    }

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        var nutrition = parent.GetComp<CompFarmNutrition>();
        nutrition?.SetAlternativeStore(this);
    }
}