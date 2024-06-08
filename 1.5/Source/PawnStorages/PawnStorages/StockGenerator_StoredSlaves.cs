using System.Collections.Generic;
using RimWorld;
using Verse;

namespace PawnStorages;

public class StockGenerator_StoredSlaves : StockGenerator_Slaves
{
    public ThingDef storeInDef;

    public override IEnumerable<Thing> GenerateThings(int forTile, Faction faction = null)
    {
        ThingWithComps storageItem = ThingMaker.MakeThing(storeInDef, GenStuff.RandomStuffByCommonalityFor(storeInDef)) as ThingWithComps;
        storageItem?.InitializeComps();
        CompPawnStorage storageComp = storageItem.GetInnerIfMinified().TryGetComp<CompPawnStorage>();
        IEnumerable<Thing> thingsGenerated = base.GenerateThings(forTile, faction);
        if (storageComp == null)
        {
            foreach (Thing thing in thingsGenerated)
            {
                yield return thing;
            }
        }
        else
        {
            foreach (Thing thing in thingsGenerated)
            {
                if (thing is not Pawn)
                {
                    yield return thing;
                }

                if (!storageComp?.CanStore ?? false)
                {
                    yield return storageItem;
                    storageItem = ThingMaker.MakeThing(storeInDef, GenStuff.RandomStuffByCommonalityFor(storeInDef)) as ThingWithComps;
                    storageItem?.InitializeComps();
                    storageComp = storageItem.GetInnerIfMinified()?.TryGetComp<CompPawnStorage>();
                }

                storageComp?.StoredPawns.Add(thing as Pawn);
                storageComp?.SetLabelDirty();
                storageComp?.SetBarDirty();
            }

            yield return storageItem;
        }
    }

    public override bool HandlesThingDef(ThingDef thingDef)
    {
        return thingDef.HasComp(typeof(CompPawnStorage));
    }
}
