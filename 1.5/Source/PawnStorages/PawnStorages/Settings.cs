using UnityEngine;
using Verse;

namespace PawnStorages;

public class Settings : ModSettings
{
    public bool AllowNeedsDrop = true;
    public bool SpecialReleaseAll = false;
    public string ForcedPawn = "";

    public void DoWindowContents(Rect wrect)
    {
        Listing_Standard options = new();
        options.Begin(wrect);

        options.CheckboxLabeled("PS_Settings_AllowNeedsDrop".Translate(), ref AllowNeedsDrop);
        if (ModsConfig.anomalyActive) options.CheckboxLabeled("PS_Settings_SpecialReleaseAll".Translate(), ref SpecialReleaseAll);
        options.Gap();
        options.Label("PS_Settings_Advanced".Translate());
        ForcedPawn = options.TextEntryLabeled("PS_Settings_ForceNextPawnStatue".Translate(), ForcedPawn);
        options.Gap();

        options.End();
    }

    public void AllReleased()
    {
        SpecialReleaseAll = false;
        Write();
    }

    public override void ExposeData()
    {
        Scribe_Values.Look(ref AllowNeedsDrop, "AllowNeedsDrop", true);
        Scribe_Values.Look(ref SpecialReleaseAll, "SpecialReleaseAll", false);
    }
}
