using UnityEngine;
using Verse;

namespace PawnStorages;

public class Settings : ModSettings
{
    public bool AllowNeedsDrop = true;
    public bool SpecialReleaseAll = false;
    public string ForcedPawn = "";
    public bool SuggestiveSilo = false;
    public float ProductionScale = 0.5f;
    public float BreedingScale = 2f;
    public int MaxPawnsInFarm = 16;
    public bool ShowStatueBar = true;

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
        options.CheckboxLabeled("PS_Settings_SuggestiveSilo".Translate(), ref SuggestiveSilo);
        options.Gap();
        options.CheckboxLabeled("PS_Settings_StatueBar".Translate(), ref ShowStatueBar);
        options.Gap();
        options.Label("PS_Settings_Production_Scale".Translate(ProductionScale.ToString("0.00")));
        ProductionScale = options.Slider(ProductionScale, 0f, 10f);
        options.Gap();
        options.Label("PS_Settings_Breeding_Scale".Translate(BreedingScale.ToString("0.00")));
        BreedingScale = options.Slider(BreedingScale, 0f, 10f);
        options.Gap();
        options.Label("PS_Settings_Max_Farm".Translate(MaxPawnsInFarm));
        options.IntAdjuster(ref MaxPawnsInFarm, 1, 1);

        options.Gap();
        if (options.ButtonText("PS_Reset".Translate()))
        {
            AllowNeedsDrop = true;
            SpecialReleaseAll = false;
            ForcedPawn = "";
            SuggestiveSilo = false;
            ProductionScale = 0.5f;
            BreedingScale = 2f;
            MaxPawnsInFarm = 16;
        }
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
        Scribe_Values.Look(ref SuggestiveSilo, "SuggestiveSilo", false);
        Scribe_Values.Look(ref ProductionScale, "ProductionScale", 0.5f);
        Scribe_Values.Look(ref BreedingScale, "BreedingScale", 2);
        Scribe_Values.Look(ref MaxPawnsInFarm, "MaxPawnsInFarm", 16);
        Scribe_Values.Look(ref ShowStatueBar, "ShowStatueBar", false);
    }
}
