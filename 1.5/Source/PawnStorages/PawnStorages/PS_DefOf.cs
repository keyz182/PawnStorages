using RimWorld;
using Verse;

namespace PawnStorages;

[DefOf]
public static class PS_DefOf
{
    //Jobs
    public static JobDef PS_Enter;
    public static JobDef PS_Release;
    public static JobDef PS_CaptureInPawnStorage;
    public static JobDef PS_CaptureEntityInPawnStorage;
    public static JobDef PS_CaptureAnimalInPawnStorage;
    public static JobDef PS_CaptureAnimalToFarm;
    public static JobDef PS_TakeToPawnStorage;
    public static JobDef PS_CaptureCarriedToPawnStorage;

    //Things
    public static ThingDef PS_DigitizerFabricator;
    public static ThingDef PS_PawnStatue;
    public static ThingDef PS_BatteryFarm;

    //Misc
    public static RecipeDef PS_MakePawnStorageDisc;
    public static WorkGiverDef PS_DoBillsDigitalBench;

    public static TimeAssignmentDef PS_Home;
}
