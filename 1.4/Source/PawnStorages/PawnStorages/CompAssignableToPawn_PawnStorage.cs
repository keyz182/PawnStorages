using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace PawnStorages;

public class CompAssignableToPawn_PawnStorage : CompAssignableToPawn
{
    public static HashSet<CompAssignableToPawn_PawnStorage> compAssiblables = new();

    public override IEnumerable<Pawn> AssigningCandidates
    {
        get
        {
            return !parent.Spawned
                ? Enumerable.Empty<Pawn>()
                : parent.Map.mapPawns.FreeColonists.OrderByDescending(p => CanAssignTo(p).Accepted);
        }
    }

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        compAssiblables.Add(this);
    }

    public override void PostDeSpawn(Map map)
    {
        base.PostDeSpawn(map);
        compAssiblables.Remove(this);
    }

    public override string GetAssignmentGizmoDesc()
    {
        return "PS_CommandSetOwnerDesc".Translate();
    }

    public override bool AssignedAnything(Pawn pawn)
    {
        return compAssiblables.Any(x => x != this && x.AssignedPawns.Contains(pawn));
    }

    public override bool ShouldShowAssignmentGizmo()
    {
        return parent.Faction == Faction.OfPlayer;
    }
}
