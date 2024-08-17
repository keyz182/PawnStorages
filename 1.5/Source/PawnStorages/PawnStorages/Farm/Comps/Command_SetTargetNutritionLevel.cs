using UnityEngine;
using Verse;

namespace PawnStorages.Farm.Comps;

public class Command_SetTargetNutritionLevel : Command
{
    public CompFarmNutrition nutritionComp;

    public override void ProcessInput(Event ev)
    {
        base.ProcessInput(ev);
        int to = (int) nutritionComp.MaxNutrition;
        Dialog_Slider dialogSlider = new(x => "PS_Farm_NutritionLevel".Translate(x), 0, to,
            value =>
            {
                nutritionComp.TargetNutritionLevel = value;
            }, nutritionComp.TargetNutritionLevel <= 0 ? to : (int) nutritionComp.TargetNutritionLevel);
        Find.WindowStack.Add(dialogSlider);
    }
}
