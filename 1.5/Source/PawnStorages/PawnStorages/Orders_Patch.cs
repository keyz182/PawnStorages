using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace PawnStorages;

[HarmonyPatch(typeof(FloatMenuMakerMap), "AddHumanlikeOrders")]
public static class OrdersPatch
{
    public static TargetingParameters ForStoring(Pawn arrester)
    {
        return new TargetingParameters()
        {
            canTargetPawns = true,
            canTargetBuildings = false,
            mapObjectTargetsMustBeAutoAttackable = false,
            validator = targ =>
                targ is { HasThing: true, Thing: Pawn thing } && thing != arrester &&
                WorkGiver_Warden_TakeToStorage.GetStorageForPawn(thing, assign: false) != null &&
                (thing.CarriedBy == arrester || thing.IsPrisoner || (thing.CanBeArrestedBy(arrester) && (!thing.Downed || !thing.guilt.IsGuilty)))
        };
    }

    public static TargetingParameters ForStoringIn(Pawn pawn, Pawn carrier)
    {
        return new TargetingParameters()
        {
            canTargetPawns = false,
            canTargetBuildings = true,
            mapObjectTargetsMustBeAutoAttackable = false,
            validator = targ =>
                targ is { HasThing: true, Thing: ThingWithComps thing } && (thing.TryGetComp<CompPawnStorage>() is { } comp && comp.CanAssign(pawn, pawn.CanBeCaptured()))
        };
    }

    [HarmonyPostfix]
    private static void Postfix(Vector3 clickPos, Pawn pawn, List<FloatMenuOption> opts)
    {
        if (pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
        {
            foreach (LocalTargetInfo dest in GenUI.TargetsAt(clickPos, ForStoring(pawn), true))
            {
                if (!pawn.CanReach(dest, PathEndMode.OnCell, Danger.Deadly))
                {
                    opts.Add(new FloatMenuOption("CannotArrest".Translate() + ": " + "NoPath".Translate().CapitalizeFirst(), null));
                }
                else
                {
                    Pawn pTarg = (Pawn)dest.Thing;
                    Action action = () =>
                    {
                        ThingWithComps storage = WorkGiver_Warden_TakeToStorage.GetStorageForPawn(pTarg, assign: true);
                        if (storage == null)
                        {
                            Messages.Message("CannotArrest".Translate() + ": " + "NoPrisonerStorage".Translate(), (Thing)pTarg,
                                MessageTypeDefOf.RejectInput,
                                false);
                        }
                        else
                        {
                            Job job = JobMaker.MakeJob(PS_DefOf.PS_CaptureInPawnStorage, (LocalTargetInfo)(Thing)pTarg, (LocalTargetInfo)(Thing)storage);
                            job.count = 1;
                            pawn.jobs.TryTakeOrderedJob(job);
                            if (pTarg.Faction == null || (pTarg.Faction == Faction.OfPlayer || pTarg.Faction.Hidden) && !pTarg.IsQuestLodger())
                                return;
                            TutorUtility.DoModalDialogIfNotKnown(ConceptDefOf.ArrestingCreatesEnemies, pTarg.GetAcceptArrestChance(pawn).ToStringPercent());
                        }
                    };
                    opts.Add(FloatMenuUtility.DecoratePrioritizedTask(
                        new FloatMenuOption(
                            "PS_TakeToStorageFloatMenu".Translate((NamedArgument)dest.Thing.LabelCap, (NamedArgument)dest.Thing), action, MenuOptionPriority.High, revalidateClickTarget: dest.Thing), pawn,
                        (LocalTargetInfo)(Thing)pTarg));
                }
            }

            if (pawn.carryTracker?.CarriedThing is not Pawn carriedPawn) return;
            foreach (LocalTargetInfo localTargetInfo in GenUI.TargetsAt(clickPos, ForStoringIn(carriedPawn, pawn), true))
            {
                Thing casket = localTargetInfo.Thing;
                TaggedString label = "PlaceIn".Translate((NamedArgument)(Thing)carriedPawn, (NamedArgument)casket);
                Action action = () =>
                {
                    localTargetInfo.Thing.TryGetComp<CompPawnStorage>()?.TryAssignPawn(carriedPawn);
                    Job job = JobMaker.MakeJob(
                        carriedPawn.IsPrisonerOfColony || carriedPawn.InAggroMentalState || carriedPawn.HostileTo(Faction.OfPlayer)
                            ? PS_DefOf.PS_CaptureCarriedToPawnStorage
                            : PS_DefOf.PS_TakeToPawnStorage, carriedPawn, localTargetInfo);
                    job.count = 1;
                    job.playerForced = true;
                    pawn.jobs.TryTakeOrderedJob(job);
                };
                opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(label, action), pawn, (LocalTargetInfo)casket));
            }
        }
    }
}
