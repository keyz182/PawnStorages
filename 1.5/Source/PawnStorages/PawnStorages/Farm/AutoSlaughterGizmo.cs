using System;
using System.Collections.Generic;
using System.Linq;
using LudeonTK;
using PawnStorages.Farm.Comps;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace PawnStorages.Farm;

public class AutoSlaughterGizmo : Gizmo
{

    private CompFarmBreeder breeder;
    private float lastTargetValue;
    private float targetValue;

    public static int increments = 16;

    public static IEnumerable<float> BandPercentages => from num in Enumerable.Range(0, increments) select 1/(num * 1f);

    public int TargetValue
    {
        get
        {
            return Mathf.CeilToInt(targetValue*increments);
        }
        set
        {
            targetValue = value / (increments * 1.0f);
        }
    }

    private Texture2D LimitedTex;
    private Texture2D UnlimitedTex;
    private const string LimitedIconPath = "UI/Icons/EntropyLimit/Limited";
    private const string UnlimitedIconPath = "UI/Icons/EntropyLimit/Unlimited";
    public const float CostPreviewFadeIn = 0.1f;
    public const float CostPreviewSolid = 0.15f;
    public const float CostPreviewFadeInSolid = 0.25f;
    public const float CostPreviewFadeOut = 0.6f;
    private static readonly Color PainBoostColor = new Color(0.2f, 0.65f, 0.35f);
    private static readonly Texture2D EntropyBarTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.46f, 0.34f, 0.35f));
    private static readonly Texture2D EntropyBarTexAdd = SolidColorMaterials.NewSolidColorTexture(new Color(0.78f, 0.72f, 0.66f));
    private static readonly Texture2D OverLimitBarTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.75f, 0.2f, 0.15f));
    private static readonly Texture2D PsyfocusBarTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.34f, 0.42f, 0.43f));
    private static readonly Texture2D PsyfocusBarTexReduce = SolidColorMaterials.NewSolidColorTexture(new Color(0.65f, 0.83f, 0.83f));
    private static readonly Texture2D PsyfocusBarHighlightTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.43f, 0.54f, 0.55f));
    private static readonly Texture2D EmptyBarTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.03f, 0.035f, 0.05f));
    private static readonly Texture2D PsyfocusTargetTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.74f, 0.97f, 0.8f));

    public AutoSlaughterGizmo(CompFarmBreeder breeder)
    {
      this.breeder = breeder;
      this.targetValue = breeder.AutoSlaughterTarget;
      this.LimitedTex = ContentFinder<Texture2D>.Get("UI/Icons/EntropyLimit/Limited");
      this.UnlimitedTex = ContentFinder<Texture2D>.Get("UI/Icons/EntropyLimit/Unlimited");
    }

    public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
    {
      Rect outerRect = new Rect(topLeft.x, topLeft.y, this.GetWidth(maxWidth), 75f);
      Rect insetRect = outerRect.ContractedBy(6f);

      Widgets.DrawWindowBackground(outerRect);
      Text.Font = GameFont.Small;

      Rect autoSlaughterLabel = insetRect;
      autoSlaughterLabel.y += 38f;
      autoSlaughterLabel.height = Text.LineHeight;
      Widgets.Label(autoSlaughterLabel, "PS_AutoSlaughterLevel".Translate());

      Rect autoSlaughterBarRect = insetRect;
      autoSlaughterBarRect.x += 63f;
      autoSlaughterBarRect.y += 38f;
      autoSlaughterBarRect.width = 100f;
      autoSlaughterBarRect.height = 22f;
      this.lastTargetValue = this.targetValue;


      Widgets.DraggableBar(
          autoSlaughterBarRect,
          PsychicEntropyGizmo.PsyfocusBarTex,
          PsychicEntropyGizmo.PsyfocusBarHighlightTex,
          PsychicEntropyGizmo.EmptyBarTex,
          PsychicEntropyGizmo.PsyfocusTargetTex,
          ref PsychicEntropyGizmo.draggingBar,
          targetValue,
          ref targetValue,
          BandPercentages,
          increments);
      if (Mathf.CeilToInt(this.lastTargetValue) != this.TargetValue)
      {
          breeder.AutoSlaughterTarget = TargetValue;
      }

      // Rect tooltipRect = insetRect;
      // tooltipRect.y += 38f;
      // tooltipRect.width = 175f;
      // tooltipRect.height = 38f;
      // TooltipHandler.TipRegion(tooltipRect, (Func<string>) (() => this.tracker.PsyfocusTipString(this.selectedPsyfocusTarget)), Gen.HashCombineInt(this.tracker.GetHashCode(), 133873));

      return new GizmoResult(GizmoState.Clear);
    }

    public override float GetWidth(float maxWidth) => 212f;
}
