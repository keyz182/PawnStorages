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
    public static JobDef PS_TakeToPawnStorage;
    public static JobDef PS_CaptureCarriedToPawnStorage;

    //Things
    public static ThingDef PS_DigitizerFabricator;
    public static ThingDef PS_PawnStatue;

    //Misc
    public static RecipeDef PS_MakePawnStorageDisc;
    public static WorkGiverDef PS_DoBillsDigitalBench;

    public static TimeAssignmentDef PS_Home;
}
