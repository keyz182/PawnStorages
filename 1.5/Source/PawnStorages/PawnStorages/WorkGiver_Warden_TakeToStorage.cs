using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;

namespace PawnStorages;

public class WorkGiver_Warden_TakeToStorage : WorkGiver_Warden
{
	public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
	{
		if (!ShouldTakeCareOfPrisoner(pawn, t))
		{
			return null;
		}
		return TryMakeJob(pawn, t, forced);
	}

	public static Job TryMakeJob(Pawn pawn, Thing t, bool forced = false)
	{
		Pawn prisoner = (Pawn)t;
		if (!forced || prisoner.CurJobDef == PS_DefOf.PS_Enter) return null;
		// TODO actually do this one
		// Job job = TakeDownedToStorageJob(prisoner, pawn);
		// if (job != null)
		// {
		// 	return job;
		// }
		Job job2 = TakeToPreferredStorageJob(prisoner, pawn);
		if (job2 != null)
		{
			return job2;
		}
		return null;
	}

	private static Job TakeToPreferredStorageJob(Pawn prisoner, Pawn warden)
	{
		if (prisoner.Downed || !warden.CanReserve(prisoner))
		{
			return null;
		}
		if (CompAssignableToPawn_PawnStorage.compAssiblables.FirstOrDefault(c => c.assignedPawns.Contains(prisoner)) is not {} comp)
		{
			return null;
		}
		Job job = JobMaker.MakeJob(PS_DefOf.PS_CaptureInPawnStorage, prisoner, comp.parent);
		job.count = 1;
		return job;
	}

	private static Job TakeDownedToStorageJob(Pawn prisoner, Pawn warden)
	{
		// TODO actually do this one currently just copy pasta for reference
		if (!prisoner.Downed || !HealthAIUtility.ShouldSeekMedicalRest(prisoner) || prisoner.InBed() || !warden.CanReserve(prisoner))
		{
			return null;
		}
		Building_Bed building_Bed = RestUtility.FindBedFor(prisoner, warden, checkSocialProperness: true, ignoreOtherReservations: false, GuestStatus.Prisoner);
		if (building_Bed != null)
		{
			Job job = JobMaker.MakeJob(JobDefOf.TakeWoundedPrisonerToBed, prisoner, building_Bed);
			job.count = 1;
			return job;
		}
		return null;
	}

	public static void TryTakePrisonerToBed(Pawn prisoner, Pawn warden)
	{
		if (prisoner.Spawned && !prisoner.InAggroMentalState && !prisoner.IsForbidden(warden) && !prisoner.IsFormingCaravan() && warden.CanReserveAndReach(prisoner, PathEndMode.OnCell, warden.NormalMaxDanger(), 1, -1, null, ignoreOtherReservations: true))
		{
			Job job = TryMakeJob(warden, prisoner, forced: true);
			if (job != null)
			{
				warden.jobs.StartJob(job, JobCondition.InterruptForced);
			}
		}
	}
}
