using PipeSystem;

namespace PawnStorages.VEF;

public class CompProperties_NutritionNetStorage : CompProperties_ResourceStorage
{
    public CompProperties_NutritionNetStorage() => compClass = typeof (CompFarmResourceStorage);
}
