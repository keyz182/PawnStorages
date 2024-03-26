using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace PawnStorages;

[HarmonyPatch(typeof(FloatMenuMakerMap), "AddHumanlikeOrders")]
public static class OrdersPatch
{
    // TODO FloatMenuMakerMap.AddDraftedOrders for capturing direct to a free prisoner slot
    public static TargetingParameters ForStoring(Pawn arrester)
    {
        return new TargetingParameters()
        {
            canTargetPawns = true,
            canTargetBuildings = false,
            mapObjectTargetsMustBeAutoAttackable = false,
            validator = targ =>
                targ is { HasThing: true, Thing: Pawn thing } && thing != arrester &&
                CompAssignableToPawn_PawnStorage.compAssiblables.SelectMany(c => c.AssignedPawns).Contains(thing) &&
                (thing.IsPrisoner || (thing.CanBeArrestedBy(arrester) && (!thing.Downed || !thing.guilt.IsGuilty)))
        };
    }

    [HarmonyPostfix]
    private static void Postfix(Vector3 clickPos, Pawn pawn, List<FloatMenuOption> opts)
    {
        if (pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
        {
            foreach (LocalTargetInfo dest in GenUI.TargetsAt(clickPos, ForStoring(pawn), true))
            {
                bool flag = dest.HasThing && dest.Thing is Pawn && ((Pawn)dest.Thing).IsWildMan();
                if (pawn.Drafted || flag)
                {
                    if (dest.Thing is Pawn && (pawn.InSameExtraFaction((Pawn)dest.Thing, ExtraFactionType.HomeFaction) ||
                                               pawn.InSameExtraFaction((Pawn)dest.Thing, ExtraFactionType.MiniFaction)))
                        opts.Add(new FloatMenuOption("CannotArrest".Translate() + ": " + "SameFaction".Translate((NamedArgument)dest.Thing), null));
                    else if (!pawn.CanReach(dest, PathEndMode.OnCell, Danger.Deadly))
                    {
                        opts.Add(new FloatMenuOption("CannotArrest".Translate() + ": " + "NoPath".Translate().CapitalizeFirst(), null));
                    }
                    else
                    {
                        Pawn pTarg = (Pawn)dest.Thing;
                        Action action = () =>
                        {
                            ThingWithComps targetB = CompAssignableToPawn_PawnStorage.compAssiblables.First(c => c.AssignedPawns.Contains((Pawn)dest.Thing)).parent;
                            if (targetB == null)
                            {
                                Messages.Message("CannotArrest".Translate() + ": " + "NoPrisonerStorage".Translate(), (Thing)pTarg,
                                    MessageTypeDefOf.RejectInput,
                                    false);
                            }
                            else
                            {
                                Job job = JobMaker.MakeJob(PS_DefOf.PS_CaptureInPawnStorage, (LocalTargetInfo)(Thing)pTarg, (LocalTargetInfo)(Thing)targetB);
                                job.count = 1;
                                pawn.jobs.TryTakeOrderedJob(job);
                                if (pTarg.Faction == null || (pTarg.Faction == Faction.OfPlayer || pTarg.Faction.Hidden) && !pTarg.IsQuestLodger())
                                    return;
                                TutorUtility.DoModalDialogIfNotKnown(ConceptDefOf.ArrestingCreatesEnemies, pTarg.GetAcceptArrestChance(pawn).ToStringPercent());
                            }
                        };
                        opts.Add(FloatMenuUtility.DecoratePrioritizedTask(
                            new FloatMenuOption(
                                "TryToArrest".Translate((NamedArgument)dest.Thing.LabelCap, (NamedArgument)dest.Thing,
                                    (NamedArgument)pTarg.GetAcceptArrestChance(pawn).ToStringPercent()), action, MenuOptionPriority.High, revalidateClickTarget: dest.Thing), pawn,
                            (LocalTargetInfo)(Thing)pTarg));
                    }
                }
            }
        }
    }
}
