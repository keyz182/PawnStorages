using PipeSystem;

namespace PawnStorages.VEF;

public class CompProperties_FarmResourceStorage : CompProperties_ResourceStorage
{
    public CompProperties_FarmResourceStorage() => this.compClass = typeof (CompFarmResourceStorage);
    
}