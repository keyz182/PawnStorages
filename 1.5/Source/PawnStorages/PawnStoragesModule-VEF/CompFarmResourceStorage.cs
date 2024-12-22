using PawnStorages.Farm.Comps;
using Verse;

namespace PawnStorages.VEF;

public class CompFarmResourceStorage : CompNutritionNetStorage
{
    public new CompProperties_FarmResourceStorage Props => (CompProperties_FarmResourceStorage)props;

    public override void CompTick()
    {
        base.CompTick();
        if (Props.storageCapacity <= 0) Props.storageCapacity = PawnStoragesMod.settings.MaxFarmStoredNutrition;
    }
}
