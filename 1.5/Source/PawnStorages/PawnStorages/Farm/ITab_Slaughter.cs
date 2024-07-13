using System.Collections.Generic;
using System.Linq;
using PawnStorages.Farm.Comps;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using Color = UnityEngine.Color;

namespace PawnStorages.Farm;

public class ITab_Slaughter : ITab
{
    private static readonly Vector2 WinSize = new(800f, 480f);
    public readonly ThingFilterUI.UIState ThingFilterState = new();
    public const float LineHeight = 24f;
    private Vector2 scrollPos;
    private Rect viewRect;
    private List<Rect> tmpMouseoverHighlightRects = [];
    private List<Rect> tmpGroupRects = [];

    public CompFarmStorage compFarmStorage => SelThing.TryGetComp<CompFarmStorage>();
    public CompFarmBreeder compFarmBreeder => SelThing.TryGetComp<CompFarmBreeder>();

    public Dictionary<PawnKindDef,AutoSlaughterConfig> AutoSlaughterSettings => compFarmBreeder.AutoSlaughterSettings;

    public override void OnOpen()
    {
        compFarmBreeder.AutoSlaughterSettings = compFarmBreeder.AutoSlaughterSettings
            .OrderByDescending(c => compFarmStorage.CountForType(c.Value.animal).total)
            .ThenBy(c => c.Value.animal.label)
            .ToDictionary(c=> c.Key, d=> d.Value);
    }

    public ITab_Slaughter()
    {
        size = WinSize;
        labelKey = "PS_SlaughterTab";
    }

    public void DoMaxColumn(WidgetRow row, ref int val, ref string buffer, int current)
    {
        if (val == -1)
        {
            float width = (float) ((60.0 - 68.0) / 2.0);
            row.Gap(width);
            if (row.ButtonIconWithBG(TexButton.Infinity, 48f, "AutoSlaughterTooltipSetLimit".Translate()))
            {
                SoundDefOf.Click.PlayOneShotOnCamera();
                val = current;
            }

            row.Gap(width);
        }
        else
        {
            row.CellGap = 0.0f;
            row.Gap(-4f);
            row.TextFieldNumeric<int>(ref val, ref buffer, 40f);
            val = Mathf.Max(0, val);
            if (row.ButtonIcon(TexButton.CloseXSmall, mouseoverColor: Color.white, overrideSize: 16f))
            {
                SoundDefOf.Click.PlayOneShotOnCamera();
                val = -1;
                buffer = null;
            }

            row.CellGap = 4f;
            row.Gap(4f);
        }
    }

    public void DrawLine(Rect rowRect, AutoSlaughterConfig config, int index)
    {
        if (index % 2 == 1)
            Widgets.DrawLightHighlight(rowRect);
        Color color = GUI.color;
        Dialog_AutoSlaughter.AnimalCountRecord animalCount = compFarmStorage.CountForType(config.animal);

        float labelWidth = 60f;
        Widgets.BeginGroup(rowRect);
        WidgetRow row = new WidgetRow(0.0f, 0.0f);
        row.DefIcon(config.animal);
        row.Gap(4f);
        GUI.color = animalCount.total == 0 ? Color.gray : color;
        row.Label( config.animal.LabelCap.Truncate(labelWidth), labelWidth, GetTipForAnimal());
        GUI.color = color;
        DrawCurrentCol(animalCount.total, config.maxTotal);
        DoMaxColumn(row, ref config.maxTotal, ref config.uiMaxTotalBuffer, animalCount.total);
        DrawCurrentCol(animalCount.male, config.maxMales);
        DoMaxColumn(row, ref config.maxMales, ref config.uiMaxMalesBuffer, animalCount.male);
        DrawCurrentCol(animalCount.maleYoung, config.maxMalesYoung);
        DoMaxColumn(row, ref config.maxMalesYoung, ref config.uiMaxMalesYoungBuffer, animalCount.maleYoung);
        DrawCurrentCol(animalCount.female, config.maxFemales);
        DoMaxColumn(row, ref config.maxFemales, ref config.uiMaxFemalesBuffer, animalCount.female);
        DrawCurrentCol(animalCount.femaleYoung, config.maxFemalesYoung);
        DoMaxColumn(row, ref config.maxFemalesYoung, ref config.uiMaxFemalesYoungBuffer, animalCount.femaleYoung);

        Widgets.EndGroup();
        return;

        void DrawCurrentCol(int val, int? limit)
        {
            Color colColour = Color.white;

            if (val == 0)
                colColour = (Color.gray);
            else if (val > limit)
            {
                colColour = ColorLibrary.RedReadable;
            }

            Color startColor = GUI.color;
            int anchor = (int) Text.Anchor;
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = colColour;
            row.Label(val.ToString(), 60f);
            Text.Anchor = (TextAnchor) anchor;
            GUI.color = startColor;
        }

        string DevTipPartForPawn(Pawn pawn)
        {
            string label = pawn.LabelShortCap + " " + pawn.gender.GetLabel() + " (" + pawn.ageTracker.AgeBiologicalYears + "y)";
            Hediff firstHediffOfDef = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Pregnant);
            if (firstHediffOfDef != null)
                label = label + ", pregnant (" + firstHediffOfDef.Severity.ToStringPercent() + ")";
            return label;
        }

        string GetTipForAnimal()
        {
            TaggedString labelCap = config.animal.LabelCap;
            if (Prefs.DevMode)
                labelCap += "\n\nDEV: Animals to slaughter:\n" + compFarmBreeder.Parent.Map.autoSlaughterManager.AnimalsToSlaughter
                    .Where(x => x.def == config.animal)
                    .Select(DevTipPartForPawn).ToLineList("  - ");
            return labelCap;
        }
    }

    private void DoAnimalHeader(Rect rect1, Rect rect2)
    {
        float labelWidth = 60f;
        Widgets.BeginGroup(new Rect(rect1.x, rect1.y, rect1.width, (float) (rect1.height + (double) rect2.height + 1.0)));
        int num = 0;
        foreach (Rect tmpGroupRect in tmpGroupRects)
        {
            if (num % 2 == 1)
            {
                Widgets.DrawLightHighlight(tmpGroupRect);
                Widgets.DrawLightHighlight(tmpGroupRect);
            }
            else
                Widgets.DrawLightHighlight(tmpGroupRect);

            GUI.color = Color.gray;
            if (num > 0)
                Widgets.DrawLineVertical(tmpGroupRect.xMin, 0.0f, (float) ( rect1.height + (double) rect2.height + 1.0));
            if (num < tmpGroupRects.Count - 1)
                Widgets.DrawLineVertical(tmpGroupRect.xMax, 0.0f, (float) ( rect1.height + (double) rect2.height + 1.0));
            GUI.color = Color.white;
            ++num;
        }

        foreach (Rect mouseoverHighlightRect in tmpMouseoverHighlightRects)
            Widgets.DrawHighlightIfMouseover(mouseoverHighlightRect);
        Widgets.EndGroup();
        tmpMouseoverHighlightRects.Clear();
        tmpGroupRects.Clear();
        Widgets.BeginGroup(rect1);
        WidgetRow row = new WidgetRow(0.0f, 0.0f);
        int anchor1 = (int) Text.Anchor;
        Text.Anchor = TextAnchor.MiddleCenter;
        row.Label(string.Empty, 24f);
        float startX = row.FinalX;
        row.Label(string.Empty, labelWidth,  "AutoSlaugtherHeaderTooltipLabel".Translate());
        Rect rect = new Rect(startX, rect1.height, row.FinalX - startX, rect2.height);
        tmpMouseoverHighlightRects.Add(rect);
        tmpGroupRects.Add(rect);
        AddCurrentAndMaxEntries("AutoSlaugtherHeaderColTotal", 0.0f, 0.0f);
        AddCurrentAndMaxEntries("AnimalMaleAdult", 0.0f, 0.0f);
        AddCurrentAndMaxEntries("AnimalMaleYoung", 0.0f, 0.0f);
        AddCurrentAndMaxEntries("AnimalFemaleAdult", 0.0f, 0.0f);
        AddCurrentAndMaxEntries("AnimalFemaleYoung", 0.0f, 0.0f);
        Text.Anchor = (TextAnchor) anchor1;
        Widgets.EndGroup();
        Widgets.BeginGroup(rect2);
        WidgetRow widgetRow = new (0.0f, 0.0f);
        TextAnchor anchor2 = Text.Anchor;
        Text.Anchor = TextAnchor.MiddleCenter;
        widgetRow.Label(string.Empty, 24f);
        widgetRow.Label( "AutoSlaugtherHeaderColLabel".Translate(), labelWidth,  "AutoSlaugtherHeaderTooltipLabel".Translate());
        widgetRow.Label( "AutoSlaugtherHeaderColCurrent".Translate(), 60f,  "AutoSlaugtherHeaderTooltipCurrentTotal".Translate());
        widgetRow.Label( "AutoSlaugtherHeaderColMax".Translate(), 56f,  "AutoSlaugtherHeaderTooltipMaxTotal".Translate());
        widgetRow.Label( "AutoSlaugtherHeaderColCurrent".Translate(), 60f,  "AutoSlaugtherHeaderTooltipCurrentMales".Translate());
        widgetRow.Label( "AutoSlaugtherHeaderColMax".Translate(), 56f,  "AutoSlaugtherHeaderTooltipMaxMales".Translate());
        widgetRow.Label( "AutoSlaugtherHeaderColCurrent".Translate(), 60f,  "AutoSlaughterHeaderTooltipCurrentMalesYoung".Translate());
        widgetRow.Label( "AutoSlaugtherHeaderColMax".Translate(), 56f,  "AutoSlaughterHeaderTooltipMaxMalesYoung".Translate());
        widgetRow.Label( "AutoSlaugtherHeaderColCurrent".Translate(), 60f,  "AutoSlaugtherHeaderTooltipCurrentFemales".Translate());
        widgetRow.Label( "AutoSlaugtherHeaderColMax".Translate(), 56f,  "AutoSlaugtherHeaderTooltipMaxFemales".Translate());
        widgetRow.Label( "AutoSlaugtherHeaderColCurrent".Translate(), 60f,  "AutoSlaugtherHeaderTooltipCurrentFemalesYoung".Translate());
        widgetRow.Label( "AutoSlaugtherHeaderColMax".Translate(), 56f,  "AutoSlaughterHeaderTooltipMaxFemalesYoung".Translate());
        Text.Anchor = anchor2;
        Widgets.EndGroup();
        GUI.color = Color.gray;
        Widgets.DrawLineHorizontal(rect2.x, (float) ( rect2.y + (double) rect2.height + 1.0), rect2.width);
        GUI.color = Color.white;
        return;

        void AddCurrentAndMaxEntries(string headerKey, float extraWidthFirst, float extraWidthSecond)
        {
            startX = row.FinalX;
            row.Label(string.Empty, 60f + extraWidthFirst);
            tmpMouseoverHighlightRects.Add(new Rect(startX, rect1.height, row.FinalX - startX, rect2.height));
            float finalX = row.FinalX;
            row.Label(string.Empty, 56f + extraWidthSecond);
            tmpMouseoverHighlightRects.Add(new Rect(finalX, rect1.height, row.FinalX - finalX, rect2.height));
            Rect lblRect = new (startX, 0.0f, row.FinalX - startX, rect2.height);
            Widgets.Label(lblRect, headerKey.Translate());
            tmpGroupRects.Add(lblRect);
        }
    }

    public override void FillTab()
    {
        if (compFarmBreeder == null) return;

        Widgets.Label(new Rect(5.0f, 0.0f, WinSize.x, 30f), "Breeding Animals");

        Rect tabRect = new Rect(0.0f, 30.0f, WinSize.x, WinSize.y - 30f).ContractedBy(10f);

        Rect insetRect = new (tabRect);
        insetRect.yMax -= Window.CloseButSize.y;
        insetRect.yMin += 8f;
        Listing_Standard listingStandard = new (insetRect,  () => scrollPos) { ColumnWidth = (float) ( tabRect.width - 16.0 - 4.0) };
        viewRect = new Rect(0.0f, 0.0f, insetRect.width - 16f, 30f * (AutoSlaughterSettings.Count + 1));
        Rect other = insetRect with { x = scrollPos.x, y = scrollPos.y };
        Widgets.BeginScrollView(insetRect, ref scrollPos, viewRect);
        listingStandard.Begin(viewRect);
        DoAnimalHeader(listingStandard.GetRect(24f), listingStandard.GetRect(24f));
        listingStandard.Gap(6f);

        int row = 0;
        foreach (KeyValuePair<PawnKindDef, AutoSlaughterConfig> item in AutoSlaughterSettings)
        {
            Rect rowRect = listingStandard.GetRect(24f);
            if (rowRect.Overlaps(other))
                DrawLine(rowRect, item.Value, row++);
            listingStandard.Gap(6f);
        }
        listingStandard.End();

        Widgets.EndScrollView();
    }
}
