﻿using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnStorages;

public class Tradeable_StoredPawn : Tradeable
{
    public override Window NewInfoDialog => new Dialog_InfoCard(AnyPawn);

    public override string Label
    {
        get
        {
            string str = base.Label;
            if (AnyPawn == null)
                return str;
            if (AnyPawn.Name is { Numerical: false } && !AnyPawn.RaceProps.Humanlike)
                str = str + ", " + AnyPawn.def.label;
            return str
                + " ("
                + AnyPawn.GetGenderLabel()
                + ", "
                + Mathf.FloorToInt(AnyPawn.ageTracker.AgeBiologicalYearsFloat)
                + ")";
        }
    }

    public override string TipDescription =>
        AnyPawn == null
            ? base.TipDescription
            : !HasAnyThing
                ? ""
                : $"{AnyThing.def.label}-{AnyPawn.MainDesc(true)}\n\n{AnyPawn.DescriptionDetailed}";

    private Pawn AnyPawn
    {
        get
        {
            if (GetCompFrom(AnyThing) is { } storageComp)
            {
                return storageComp.StoredPawns.FirstOrDefault();
            }

            return null;
        }
    }

    public CompPawnStorage GetCompFrom(Thing thing)
    {
        return AnyThing?.GetInnerIfMinified()?.TryGetComp<CompPawnStorage>();
    }

    public override void ResolveTrade()
    {
        switch (ActionToDo)
        {
            case TradeAction.PlayerSells:
                List<ThingWithComps> listSold = thingsColony
                    .Take(CountToTransferToDestination)
                    .Cast<ThingWithComps>()
                    .ToList();
                foreach (ThingWithComps t in listSold)
                {
                    List<Pawn> slaves = GetCompFrom(t)?.StoredPawns ?? [];
                    int num = slaves
                        .Select(slave =>
                            GuestUtility.IsSellingToSlavery(slave, TradeSession.trader.Faction)
                                ? 1
                                : 0
                        )
                        .Sum();
                    foreach (Pawn slavePawn in slaves)
                    {
                        TradeSession.trader.GiveSoldThingToTrader(
                            slavePawn,
                            1,
                            TradeSession.playerNegotiator
                        );
                    }

                    TradeSession.trader.GiveSoldThingToTrader(t, 1, TradeSession.playerNegotiator);

                    if (num != 0)
                        Find.HistoryEventsManager.RecordEvent(
                            new HistoryEvent(
                                HistoryEventDefOf.SoldSlave,
                                TradeSession.playerNegotiator.Named(HistoryEventArgsNames.Doer)
                            )
                        );
                }

                break;
            case TradeAction.PlayerBuys:
                List<ThingWithComps> listPurchased = thingsTrader
                    .Take(CountToTransferToSource)
                    .Cast<ThingWithComps>()
                    .ToList();
                foreach (ThingWithComps thingPurchased in listPurchased)
                {
                    List<Pawn> slaves = GetCompFrom(thingPurchased)?.StoredPawns ?? [];
                    foreach (Pawn slavePawn in slaves)
                    {
                        TradeSession.trader.GiveSoldThingToPlayer(
                            slavePawn,
                            1,
                            TradeSession.playerNegotiator
                        );
                    }

                    TradeSession.trader.GiveSoldThingToPlayer(thingPurchased, 1, TradeSession.playerNegotiator);
                    TradeSession.trader.GiveSoldThingToPlayer(thingPurchased, 1, TradeSession.playerNegotiator);
                }

                break;
            case TradeAction.None:
            default:
                return;
        }
    }
}
