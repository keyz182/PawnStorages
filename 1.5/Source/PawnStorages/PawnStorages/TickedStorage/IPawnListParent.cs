using Verse;

namespace PawnStorages.TickedStorage;

public interface IPawnListParent
{
    public ThingOwner GetDirectlyHeldThings();

    public void ReleasePawn(Pawn pawn);

    public bool NeedsDrop();

    public string Label { get; }
    public ThingDef Def { get; }

    public void Destroy(DestroyMode mode = DestroyMode.Vanish);

    public void Notify_PawnAdded(Pawn pawn);
    public void Notify_PawnRemoved(Pawn pawn);

    public Building Building { get; }
}
