using UnityEngine;
using Verse;

namespace PawnStorages;

public class Settings : ModSettings
{
    public bool AllowNeedsDrop = true;

    public void DoWindowContents(Rect wrect)
    {
        Listing_Standard options = new();
        options.Begin(wrect);

        options.CheckboxLabeled("PS_Settings_AllowNeedsDrop".Translate(), ref AllowNeedsDrop);
        options.Gap();

        options.End();
    }

    public override void ExposeData()
    {
        Scribe_Values.Look(ref AllowNeedsDrop, "AllowNeedsDrop", true);
    }
}
