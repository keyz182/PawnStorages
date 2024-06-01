using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace PawnStorages;

public class CompAssignableToPawn_PawnStorage : CompAssignableToPawn
{
    public static HashSet<CompAssignableToPawn_PawnStorage> compAssiblables = [];
    public BedOwnerType OwnerType = BedOwnerType.Colonist;
    public new CompProperties_PSAssignableToPawn Props => props as CompProperties_PSAssignableToPawn;

    public override IEnumerable<Pawn> AssigningCandidates
    {
        get
        {
            if (Props.colonyAnimalsOnly)
            {
                return parent.Map.mapPawns.FreeColonists.OrderByDescending(p => p.RaceProps.Animal);
            }

            return !parent.Spawned ? Enumerable.Empty<Pawn>() : OwnerType switch
            {
                BedOwnerType.Colonist => parent.Map.mapPawns.FreeColonists.OrderByDescending(p => CanAssignTo(p).Accepted),
                BedOwnerType.Prisoner => parent.Map.mapPawns.PrisonersOfColony.OrderByDescending(p => CanAssignTo(p).Accepted),
                BedOwnerType.Slave => parent.Map.mapPawns.SlavesOfColonySpawned.OrderByDescending(p => CanAssignTo(p).Accepted),
                _ => Enumerable.Empty<Pawn>()
            };
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

    public override void TryAssignPawn(Pawn pawn)
    {
        foreach (CompAssignableToPawn_PawnStorage otherStorage in compAssiblables)
        {
            if (otherStorage != this) otherStorage.TryUnassignPawn(pawn);
        }

        base.TryAssignPawn(pawn);
    }


    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref OwnerType, "ownerType", BedOwnerType.Colonist);
    }
}
