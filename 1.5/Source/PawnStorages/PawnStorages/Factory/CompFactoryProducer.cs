using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace PawnStorages.Factory;

public class CompFactoryProducer : CompPawnStorageProducer, IBillGiver
{
    public BillStack billStack;
    public float storedWork = 0f;
    public Bill currentBill;
    private List<IntVec3> cachedAdjCellsCardinal;

    public bool CurrentlyUsableForBills() => false;
    public bool UsableForBillsAfterFueling() => false;

    public void Notify_BillDeleted(Bill bill)
    {
        if (currentBill == bill) currentBill = null;
    }

    public Map Map => parent.Map;
    public BillStack BillStack => billStack;
    public Bill CurrentBill => currentBill;
    public IEnumerable<IntVec3> IngredientStackCells => [];
    public string LabelShort => parent.LabelShort;

    public CompFactoryProducer() => billStack = new BillStack(this);

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref storedWork, "storedWork");
        Scribe_Deep.Look(ref billStack, "billStack", this);
    }

    public List<IntVec3> AdjCellsCardinalInBounds =>
        cachedAdjCellsCardinal ??= GenAdj.CellsAdjacentCardinal(parent)
            .Where(c => c.InBounds(parent.Map))
            .ToList();

    public override void CompTick()
    {
        base.CompTick();

        if (!PawnStoragesMod.settings.AllowNeedsDrop) return;

        if (parent.IsHashIntervalTick(Parent.TickInterval))
        {
            storedWork += Parent.ProducingPawns.Count * Parent.TickInterval;
            if (currentBill == null) TryPickNextBill();
            if (currentBill != null && storedWork > currentBill?.GetWorkAmount())
            {
                List<Thing> chosenIngredients = SelectedIngredientsFor(currentBill.recipe)?.Select(pair => pair.Key.SplitOff(pair.Value)).ToList() ?? [];
                if (chosenIngredients.Count == 0)
                {
                    if (TryPickNextBill() != null) chosenIngredients = SelectedIngredientsFor(currentBill.recipe)?.Select(pair => pair.Key.SplitOff(pair.Value)).ToList() ?? [];
                }

                if (chosenIngredients.Count == 0)
                {
                    DaysProduce.AddRange(GenRecipe.MakeRecipeProducts(currentBill.recipe, Parent.ProducingPawns.RandomElement(), chosenIngredients,
                        CalculateDominantIngredient(chosenIngredients, currentBill.recipe), this));
                    ConsumeIngredients(chosenIngredients, currentBill.recipe, parent.Map);
                }
            }
        }

        if (!ProduceNow && (!parent.IsHashIntervalTick(60000 / Math.Max(PawnStoragesMod.settings.ProductionsPerDay, 1)) || DaysProduce.Count <= 0 || !Parent.IsActive)) return;
        List<Thing> failedToPlace = [];
        failedToPlace.AddRange(DaysProduce.Where(thing => !GenPlace.TryPlaceThing(thing, parent.Position, parent.Map, ThingPlaceMode.Near)));
        DaysProduce.Clear();
        DaysProduce.AddRange(failedToPlace);
    }

    public Dictionary<Thing, int> SelectedIngredientsFor(RecipeDef recipeDef)
    {
        List<IngredientCount> ingredientList = [];
        currentBill.MakeIngredientsListInProcessingOrder(ingredientList);
        Dictionary<Thing, int> reserved = [];
        Dictionary<IngredientCount, int> countSoFar = [];
        Dictionary<IngredientCount, bool> done = [];
        foreach (IngredientCount ingredientCount in ingredientList) done.Add(ingredientCount, false);

        foreach (IntVec3 cellsCardinalInBound in AdjCellsCardinalInBounds)
        {
            Map map = parent.Map;
            List<Thing> potentialInputItemThings = cellsCardinalInBound.GetThingList(map);
            foreach (Thing potentialInputItemThing in potentialInputItemThings)
            {
                foreach (IngredientCount ingredientCount in ingredientList)
                {
                    if (done[ingredientCount] || !ingredientCount.filter.Allows(potentialInputItemThing)) continue;
                    int countSoFarForIngredient = countSoFar.GetWithFallback(ingredientCount, 0);
                    int required = ingredientCount.CountRequiredOfFor(potentialInputItemThing.def, recipeDef);
                    required -= countSoFarForIngredient;
                    if (required > 0)
                    {
                        int reservedSoFar = reserved.GetWithFallback(potentialInputItemThing, 0);
                        int reservable = potentialInputItemThing.stackCount - reservedSoFar;
                        int toReserve = Math.Min(required, reservable);
                        if (toReserve >= required) done[ingredientCount] = true;
                        reserved.SetOrAdd(potentialInputItemThing, toReserve + reservedSoFar);
                    }
                }
            }
        }

        return done.Any(pair => !pair.Value) ? [] : reserved;
    }

    private static Thing CalculateDominantIngredient(List<Thing> ingredients, RecipeDef recipeDef)
    {
        if (recipeDef.productHasIngredientStuff)
            return ingredients[0];
        return recipeDef.products.Any(x => x.thingDef.MadeFromStuff) || recipeDef.unfinishedThingDef is { MadeFromStuff: true }
            ? ingredients.Where(x => x.def.IsStuff).RandomElementByWeight(x => x.stackCount)
            : ingredients.RandomElementByWeight(x => x.stackCount);
    }

    private static void ConsumeIngredients(List<Thing> ingredients, RecipeDef recipe, Map map)
    {
        foreach (Thing t in ingredients)
            if (!t.Destroyed)
                recipe.Worker.ConsumeIngredient(t, recipe, map);
    }

    private Bill TryPickNextBill()
    {
        currentBill = BillStack.bills.FirstOrDefault(b => b.ShouldDoNow() && SelectedIngredientsFor(b.recipe).Any());
        return currentBill;
    }
}
