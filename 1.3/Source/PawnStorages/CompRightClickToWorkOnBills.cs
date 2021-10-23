using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace PawnStorages
{
    public class CompRightClickToWorkOnBills : ThingComp
    {
        public IBillGiver BillGiver => this.parent as IBillGiver;
        public CompProperties_RightClickToWorkOnBills Props => base.props as CompProperties_RightClickToWorkOnBills;
        public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
        {
            foreach (var f in base.CompFloatMenuOptions(selPawn))
            {
                yield return f;
            }

            foreach (var recipe in Props.recipeToCallRightClick)
            {
                yield return new FloatMenuOption("PS.WorkOn".Translate(recipe.label), delegate
                {
                    if (!BillGiver.BillStack.Bills.Any(x => x.recipe == recipe))
                    {
                        BillGiver.BillStack.AddBill(recipe.MakeNewBill());
                    }
                    var bill = BillGiver.BillStack.bills.First(x => x.recipe == recipe) as Bill_Production;
                    if (BillGiver.BillStack.bills.First() != bill)
                    {
                        BillGiver.BillStack.bills.Remove(bill);
                        BillGiver.BillStack.bills.Insert(0, bill);
                    }
                    if (bill.targetCount == 0)
                    {
                        bill.targetCount = 1;
                    }
                    var workGiver = new WorkGiver_DoBill();
                    workGiver.def = PS_DefOf.PS_DoBillsDigitalBench;
                    var job = workGiver.JobOnThing(selPawn, parent);
                    if (job != null)
                    {
                        selPawn.jobs.TryTakeOrderedJob(job);
                    }
                });
            }
        }
    }
}
