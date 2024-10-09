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
    public static TargetingParameters ForStoring(Pawn arrester)
    {
        return new TargetingParameters()
        {
            canTargetPawns = true,
            canTargetBloodfeeders = true,
            canTargetBuildings = false,
            mapObjectTargetsMustBeAutoAttackable = false,
            validator = targ =>
                targ is { HasThing: true, Thing: Pawn thing } && thing != arrester &&
                (thing.CarriedBy == arrester || thing.IsPrisoner || thing.CanBeCaptured() || (thing.CanBeArrestedBy(arrester) && (!thing.Downed || !thing.guilt.IsGuilty)))
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

    public static TargetingParameters ForEntityOrAnimalCapture()
    {
        return new TargetingParameters()
        {
            canTargetPawns = true,
            canTargetBuildings = false,
            canTargetAnimals = true,
            canTargetMutants = true,
            canTargetHumans = false,
            canTargetMechs = true,
            mapObjectTargetsMustBeAutoAttackable = false,
            validator = targ =>
                targ is { HasThing: true, Thing: Pawn thing }
        };
    }

    public static TargetingParameters ForFarming(bool breeding = false)
    {
        return new TargetingParameters()
        {
            canTargetPawns = true,
            canTargetBuildings = false,
            canTargetAnimals = true,
            onlyTargetFactions =
            [
                Faction.OfPlayer
            ],
            mapObjectTargetsMustBeAutoAttackable = false,
            validator = (targ) =>
                targ is { HasThing: true, Thing: Pawn thing } && WorkGiver_Warden_TakeToStorage.GetStorageForFarmAnimal(thing, assign: false, breeding: breeding) != null
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
                    Pawn pTarg = (Pawn) dest.Thing;
                    bool notArresting = pTarg.Faction == null || (pTarg.Faction == Faction.OfPlayer || pTarg.Faction.Hidden) && !pTarg.IsQuestLodger();

                    bool anyStorage = false;
                    foreach (CompAssignableToPawn_PawnStorage storage in WorkGiver_Warden_TakeToStorage.GetPossibleStorages(pTarg).GroupBy(s => s.parent.def)
                                 .Select(sGroup => sGroup.FirstOrDefault()))
                    {
                        if (storage == null) continue;
                        anyStorage = true;
                        opts.Add(FloatMenuUtility.DecoratePrioritizedTask(
                            new FloatMenuOption(
                                (notArresting ? "PS_TakeToStorageFloatMenu" : "PS_CaptureToStorageFloatMenu").Translate((NamedArgument) pTarg.LabelCap,
                                    (NamedArgument) storage.parent.LabelNoParenthesisCap),
                                () =>
                                {
                                    ThingWithComps building = WorkGiver_Warden_TakeToStorage.GetStorageGeneral(pTarg, assign: true, preferredStorage: storage);
                                    Job job = JobMaker.MakeJob(notArresting ? PS_DefOf.PS_TakeToPawnStorage : PS_DefOf.PS_CaptureInPawnStorage,
                                        (LocalTargetInfo) (Thing) pTarg, (LocalTargetInfo) (Thing) building);
                                    job.count = 1;
                                    pawn.jobs.TryTakeOrderedJob(job);
                                    if (notArresting)
                                        return;
                                    TutorUtility.DoModalDialogIfNotKnown(ConceptDefOf.ArrestingCreatesEnemies, pTarg.GetAcceptArrestChance(pawn).ToStringPercent());
                                },
                                MenuOptionPriority.High,
                                revalidateClickTarget: pTarg), pawn,
                            (LocalTargetInfo) (Thing) pTarg));
                    }

                    foreach (CompPawnStorage comp in pawn.inventory.GetDirectlyHeldThings()
                                 .Select(item => item.TryGetComp<CompPawnStorage>() is { } ps
                                                 && ps.Props.useFromInventory && !ps.IsFull
                                     ? ps
                                     : null)
                                 .Where(ps => ps != null)
                                 .GroupBy(s => s.parent.def)
                                 .Select(sGroup => sGroup.FirstOrDefault()))
                    {
                        if (comp == null) continue;
                        anyStorage = true;
                        opts.Add(FloatMenuUtility.DecoratePrioritizedTask(
                            new FloatMenuOption(
                                "PS_CaptureToStorageFloatMenu".Translate((NamedArgument) pTarg.LabelCap, (NamedArgument) comp.parent.LabelNoParenthesisCap),
                                () =>
                                {
                                    Job job = JobMaker.MakeJob(PS_DefOf.PS_CaptureInPawnStorageItem, (LocalTargetInfo) (Thing) pTarg, (LocalTargetInfo) (Thing) comp.parent);
                                    job.count = 1;
                                    pawn.jobs.TryTakeOrderedJob(job);
                                    if (notArresting)
                                        return;
                                    TutorUtility.DoModalDialogIfNotKnown(ConceptDefOf.ArrestingCreatesEnemies, pTarg.GetAcceptArrestChance(pawn).ToStringPercent());
                                },
                                MenuOptionPriority.High,
                                revalidateClickTarget: pTarg), pawn,
                            (LocalTargetInfo) (Thing) pTarg));
                    }

                    if (!anyStorage)
                    {
                        opts.Add(new FloatMenuOption(
                            "PS_NoPrisonerStorage".Translate((NamedArgument) dest.Thing.Label),
                            null));
                    }
                }
            }

            if (pawn.carryTracker?.CarriedThing is Pawn carriedPawn)
            {
                foreach (LocalTargetInfo localTargetInfo in GenUI.TargetsAt(clickPos, ForStoringIn(carriedPawn, pawn),
                             true))
                {
                    Thing casket = localTargetInfo.Thing;
                    TaggedString label = "PlaceIn".Translate((NamedArgument) (Thing) carriedPawn, (NamedArgument) casket);
                    Action action = () =>
                    {
                        localTargetInfo.Thing.TryGetComp<CompPawnStorage>()?.TryAssignPawn(carriedPawn);
                        Job job = JobMaker.MakeJob(
                            carriedPawn.IsPrisonerOfColony || carriedPawn.InAggroMentalState ||
                            carriedPawn.HostileTo(Faction.OfPlayer)
                                ? PS_DefOf.PS_CaptureCarriedToPawnStorage
                                : PS_DefOf.PS_TakeToPawnStorage, carriedPawn, localTargetInfo);
                        job.count = 1;
                        job.playerForced = true;
                        pawn.jobs.TryTakeOrderedJob(job);
                    };
                    opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(label, action), pawn,
                        (LocalTargetInfo) casket));
                }
            }

            // ForColonistAnimalCapture
            IEnumerable<LocalTargetInfo> targets = GenUI.TargetsAt(clickPos, ForEntityOrAnimalCapture(), true);
            foreach (LocalTargetInfo localTargetInfo in targets)
            {
                if (!pawn.CanReach(localTargetInfo, PathEndMode.OnCell, Danger.Deadly))
                {
                    opts.Add(new FloatMenuOption(
                        "PS_CannotStore".Translate((NamedArgument) localTargetInfo.Thing.Label) + ": " + "NoPath".Translate().CapitalizeFirst(), null));
                }
                else
                {
                    Pawn pTarg = (Pawn) localTargetInfo.Thing;

                    bool anyStorage = false;
                    foreach (CompAssignableToPawn_PawnStorage storage in WorkGiver_Warden_TakeToStorage.GetPossibleStorages(pTarg).GroupBy(s => s.parent.def)
                                 .Select(sGroup => sGroup.FirstOrDefault()))
                    {
                        if (storage == null || (storage.Props?.disallowEntityStoringCommand ?? false)) continue;
                        anyStorage = true;
                        opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(
                            "PS_StoreEntity".Translate((NamedArgument) localTargetInfo.Thing.Label,
                                (NamedArgument) storage.parent.LabelNoParenthesisCap),
                            () =>
                            {
                                ThingWithComps building = WorkGiver_Warden_TakeToStorage.GetStorageGeneral(pTarg, assign: true, preferredStorage: storage);
                                Job job = JobMaker.MakeJob(pTarg.Faction == Faction.OfPlayer ? PS_DefOf.PS_CaptureAnimalInPawnStorage : PS_DefOf.PS_CaptureEntityInPawnStorage,
                                    localTargetInfo, (LocalTargetInfo) (Thing) building);
                                job.count = 1;
                                pawn.jobs.TryTakeOrderedJob(job);
                            }), pawn, localTargetInfo));
                    }


                    foreach (CompPawnStorage comp in pawn.inventory.GetDirectlyHeldThings()
                                 .Select(item => item.TryGetComp<CompPawnStorage>() is { } ps
                                                 && ps.Props.useFromInventory && !ps.IsFull
                                     ? ps
                                     : null)
                                 .Where(ps => ps != null)
                                 .GroupBy(s => s.parent.def)
                                 .Select(sGroup => sGroup.FirstOrDefault()))
                    {
                        if (comp == null) continue;
                        anyStorage = true;
                        opts.Add(FloatMenuUtility.DecoratePrioritizedTask(
                            new FloatMenuOption(
                                "PS_CaptureToStorageFloatMenu".Translate((NamedArgument) pTarg.LabelCap, (NamedArgument) comp.parent.LabelNoParenthesisCap),
                                () =>
                                {
                                    Job job = JobMaker.MakeJob(PS_DefOf.PS_CaptureInPawnStorageItem, (LocalTargetInfo) (Thing) pTarg, (LocalTargetInfo) (Thing) comp.parent);
                                    job.count = 1;
                                    pawn.jobs.TryTakeOrderedJob(job);
                                },
                                MenuOptionPriority.High,
                                revalidateClickTarget: pTarg), pawn,
                            (LocalTargetInfo) (Thing) pTarg));
                    }

                    if (!anyStorage)
                    {
                        opts.Add(new FloatMenuOption(
                            "PS_NoEntityStore".Translate((NamedArgument) localTargetInfo.Thing.Label),
                            null));
                    }
                }
            }

            //Take to the farm
            IEnumerable<LocalTargetInfo> farmableTargets = GenUI.TargetsAt(clickPos, ForFarming(), true);
            foreach (LocalTargetInfo localTargetInfo in farmableTargets)
            {
                if (!pawn.CanReach(localTargetInfo, PathEndMode.OnCell, Danger.Deadly))
                {
                    opts.Add(new FloatMenuOption(
                        "PS_NoFarm".Translate((NamedArgument) localTargetInfo.Thing.Label) + ": " + "NoPath".Translate().CapitalizeFirst(), null));
                }
                else
                {
                    Pawn pTarg = (Pawn) localTargetInfo.Thing;
                    ThingWithComps building = WorkGiver_Warden_TakeToStorage.GetStorageForFarmAnimal(pTarg, assign: false);

                    if (building != null)
                    {
                        opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(
                            "PS_FarmAnimal".Translate((NamedArgument) localTargetInfo.Thing.Label,
                                (NamedArgument) building.LabelCap),
                            () =>
                            {
                                Job job = JobMaker.MakeJob(PS_DefOf.PS_CaptureAnimalToFarm, localTargetInfo, (LocalTargetInfo) (Thing) building);
                                job.count = 1;
                                pawn.jobs.TryTakeOrderedJob(job);
                            }), pawn, localTargetInfo));
                    }
                    else
                    {
                        opts.Add(new FloatMenuOption(
                            "PS_NoFarm".Translate((NamedArgument) localTargetInfo.Thing.Label),
                            null));
                    }
                }
            }


            //Take to breeding domes
            IEnumerable<LocalTargetInfo> breedableTargets = GenUI.TargetsAt(clickPos, ForFarming(true), true);
            foreach (LocalTargetInfo localTargetInfo in breedableTargets)
            {
                if (!pawn.CanReach(localTargetInfo, PathEndMode.OnCell, Danger.Deadly))
                {
                    opts.Add(new FloatMenuOption(
                        "PS_NoBreedingFarm".Translate((NamedArgument) localTargetInfo.Thing.Label) + ": " + "NoPath".Translate().CapitalizeFirst(), null));
                }
                else
                {
                    Pawn pTarg = (Pawn) localTargetInfo.Thing;
                    ThingWithComps building = WorkGiver_Warden_TakeToStorage.GetStorageForFarmAnimal(pTarg, assign: false, true);

                    if (building != null)
                    {
                        opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(
                            "PS_BreedAnimal".Translate((NamedArgument) localTargetInfo.Thing.Label,
                                (NamedArgument) building.LabelCap),
                            () =>
                            {
                                Job job = JobMaker.MakeJob(PS_DefOf.PS_CaptureAnimalToFarm, localTargetInfo, (LocalTargetInfo) (Thing) building);
                                job.count = 1;
                                pawn.jobs.TryTakeOrderedJob(job);
                            }), pawn, localTargetInfo));
                    }
                    else
                    {
                        opts.Add(new FloatMenuOption(
                            "PS_NoBreedingFarm".Translate((NamedArgument) localTargetInfo.Thing.Label),
                            null));
                    }
                }
            }
        }
    }
}

[HarmonyPatch(typeof(FloatMenuMakerMap), "AddMutantOrders")]
public static class MutantOrdersPatch
{
    [HarmonyPostfix]
    private static void Postfix(Vector3 clickPos, Pawn pawn, List<FloatMenuOption> opts)
    {
        if (IntVec3.FromVector3(clickPos).GetFirstThingWithComp<CompPawnStorage>(pawn.Map) is not { } storage) return;
        CompPawnStorage storageComp = storage.TryGetComp<CompPawnStorage>();
        if (storageComp.Props.convertOption && storageComp.CanStore)
            opts.Add(new FloatMenuOption("PS_Enter".Translate(), delegate
            {
                Job job = storageComp.EnterJob(pawn);
                pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
            }));
    }
}
