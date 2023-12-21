using UnityEngine;
using Verse;

namespace PawnStorages;

public class Settings : ModSettings
{
    public bool AllowNeedsDrop = true;
    public string ForcedPawn = "";

    public void DoWindowContents(Rect wrect)
    {
        Listing_Standard options = new();
        options.Begin(wrect);

        options.CheckboxLabeled("PS_Settings_AllowNeedsDrop".Translate(), ref AllowNeedsDrop);
        options.Gap();
        options.Label("PS_Settings_Advanced".Translate());
        ForcedPawn = options.TextEntryLabeled("PS_Settings_ForceNextPawnStatue".Translate(), ForcedPawn);
        options.Gap();

        options.End();
    }

    public override void ExposeData()
    {
        Scribe_Values.Look(ref AllowNeedsDrop, "AllowNeedsDrop", true);
    }
}
