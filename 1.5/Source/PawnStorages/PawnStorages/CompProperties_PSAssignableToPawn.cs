using RimWorld;

namespace PawnStorages
{
    public class CompProperties_PSAssignableToPawn: CompProperties_AssignableToPawn
    {
        public bool colonyAnimalsOnly = false;
        public bool showGizmo = true;
        public bool drawAsFrozenInCarbonite = false;
        public CompProperties_PSAssignableToPawn() => compClass = typeof(CompAssignableToPawn_PawnStorage);
    }
}
