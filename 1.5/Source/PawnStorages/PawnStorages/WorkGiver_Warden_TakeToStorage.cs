using System.Linq;
using PawnStorages.Farm;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;

namespace PawnStorages;

public class WorkGiver_Warden_TakeToStorage : WorkGiver_Warden
{
    public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        return !ShouldTakeCareOfPrisoner(pawn, t) ? null : TryMakeJob(pawn, t, forced);
    }

    public static Job TryMakeJob(Pawn pawn, Thing t, bool forced = false)
    {
        Pawn prisoner = (Pawn)t;
        if (!forced || prisoner.CurJobDef == PS_DefOf.PS_Enter) return null;
        Job job2 = TakeToPreferredStorageJob(prisoner, pawn);
        return job2;
    }

    private static Job TakeToPreferredStorageJob(Pawn prisoner, Pawn warden)
    {
        if (!prisoner.Spawned || prisoner.InAggroMentalState || prisoner.IsForbidden(warden) || prisoner.IsFormingCaravan() ||
            !warden.CanReserveAndReach(prisoner, PathEndMode.OnCell, warden.NormalMaxDanger(), 1, -1, null, ignoreOtherReservations: true))
        {
            return null;
        }

        if (GetStorageForPawn(prisoner) is not { } storage)
        {
            return null;
        }

        Job job = JobMaker.MakeJob(PS_DefOf.PS_CaptureInPawnStorage, prisoner, storage);
        job.count = 1;
        return job;
    }

    public static ThingWithComps GetStorageForPawn(Pawn prisoner, bool assign = false)
    {
        ThingWithComps existingAssigned = CompAssignableToPawn_PawnStorage.compAssignables
            .FirstOrDefault(c => c.parent is not Building_PSFarm && c.assignedPawns.Contains(prisoner))?.parent;
        if (existingAssigned != null)
        {
            return existingAssigned;
        }

        if (CompAssignableToPawn_PawnStorage.compAssignables.FirstOrDefault(c => c.parent is not Building_PSFarm && c.HasFreeSlot && c.OwnerType == BedOwnerType.Prisoner) is not
            { } assignable) return null;
        if (assign) assignable.TryAssignPawn(prisoner);
        return assignable.parent;
    }

    public static ThingWithComps GetStorageEntityOrAnimal(Pawn prisoner, bool assign = false, bool asPrisoner = true)
    {
        ThingWithComps existingAssigned = CompAssignableToPawn_PawnStorage.compAssignables
            .FirstOrDefault(c => c.parent is not Building_PSFarm && c.assignedPawns.Contains(prisoner))?.parent;
        if (existingAssigned != null)
        {
            return existingAssigned;
        }

        var bedOwnerType = BedOwnerType.Prisoner;

        if (prisoner.Faction == Faction.OfPlayer) bedOwnerType = BedOwnerType.Colonist;

        if (CompAssignableToPawn_PawnStorage.compAssignables.FirstOrDefault(c => c.parent is not Building_PSFarm && c.HasFreeSlot && (!asPrisoner || c.OwnerType == bedOwnerType))
            is not { } assignable) return null;

        if (assign) assignable.TryAssignPawn(prisoner);
        return assignable.parent;
    }

    public static ThingWithComps GetStorageForFarmAnimal(Pawn prisoner, bool assign = false)
    {
        ThingWithComps existingAssigned = CompAssignableToPawn_PawnStorage.compAssignables.FirstOrDefault(c => c.parent is Building_PSFarm && c.assignedPawns.Contains(prisoner))
            ?.parent;
        if (existingAssigned != null)
        {
            return existingAssigned;
        }

        if (CompAssignableToPawn_PawnStorage.compAssignables.FirstOrDefault(c => c.parent is Building_PSFarm && c.HasFreeSlot && prisoner.RaceProps.hasGenders && prisoner.RaceProps.lifeStageAges.Count > 0) is not { } assignable) return null;
        if (assign) assignable.TryAssignPawn(prisoner);
        return assignable.parent;
    }
}
