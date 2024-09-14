using Verse;

namespace PawnStorages.TickedStorage;

public interface IPawnListParent
{
    public ThingOwner GetDirectlyHeldThings();

    public void ReleasePawn(Pawn pawn);

    public bool NeedsDrop();
}
