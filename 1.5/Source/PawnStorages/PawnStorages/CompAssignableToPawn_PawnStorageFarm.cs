using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace PawnStorages;
public class CompAssignableToPawn_PawnStorageFarm : CompAssignableToPawn_PawnStorage
{

    public override IEnumerable<Pawn> AssigningCandidates
    {
        get
        {
            return !parent.Spawned ? Enumerable.Empty<Pawn>() : parent.Map.mapPawns.FreeColonists.OrderByDescending(p => CanAssignTo(p).Accepted);
        }
    }
}