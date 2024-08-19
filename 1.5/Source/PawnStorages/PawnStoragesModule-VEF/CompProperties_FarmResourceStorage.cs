using PipeSystem;

namespace PawnStorages.VEF;

public class CompProperties_FarmResourceStorage : CompProperties_NutritionNetStorage
{
    public CompProperties_FarmResourceStorage() => this.compClass = typeof (CompFarmResourceStorage);

}
