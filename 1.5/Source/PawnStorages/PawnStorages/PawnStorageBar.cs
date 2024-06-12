using System;
using System.Collections.Generic;
using System.Linq;
using PawnStorages.Farm.Comps;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnStorages;

public class PawnStorageBar
{
    private Dictionary<string, string> pawnLabelsCache = new Dictionary<string, string>();
    
    public float MaxPawnHeight = 48f;
    public float MinPawnHeight = 12f;
    public float MaxPawnWidth = 48f;
    public float MinPawnWidth = 12f;

    public float PawnSpacing = 20f;

    public float MaxPawnBarHeight = UI.screenHeight - 500f;

    public float PawnBarOriginX = 50f;
    public float PawnBarOriginY = 50f;

    public int selectedPawnIdx = -1;

    public bool PawnsDirty = true;
    public bool RectsDirty = true;
    protected List<Pawn> _StoredPawns = new List<Pawn>();
    
    // Only faction pawns that aren't in farms
    public List<Pawn> StoredPawns {
        get
        {
            if (PawnsDirty)
            {
                _StoredPawns = PawnStorages_GameComponent.CompPawnStorage.Where(cps => cps is not CompFarmStorage)
                    .SelectMany(cps => cps.GetDirectlyHeldThings()).Where(p => p.Faction == Faction.OfPlayer)
                    .Select(p => p as Pawn).ToList();
                PawnsDirty = false;
                RectsDirty = true;
            }

            return _StoredPawns;
        }
    }

    public float Scale = 1f;
    protected List<Rect> _StoredPawnRects = new List<Rect>();
    public List<Rect> StoredPawnRects {
        get
        {
            if (RectsDirty)
            {
                _StoredPawnRects = CalculateOptimalPawnRect(StoredPawns);
                RectsDirty = false;
            }

            return _StoredPawnRects;
        }
    }
    
    public Rect GetPawnTextureRect(Vector2 pos)
    {
        float x = pos.x;
        float y = pos.y;
        Vector2 vector2 = ColonistBarColonistDrawer.PawnTextureSize * Scale;
        return new Rect(x + 1f, (float) ((double) y - ((double) vector2.y - (double)(MaxPawnWidth*Scale)) - 1.0), vector2.x, vector2.y).ContractedBy(1f);
    }
    
    public void PawnStorageBarOnGui()
    {
        var selected = false;
        if (Event.current.type != EventType.Layout)
        {
            for (var i = 0; i < StoredPawns.Count; i++)
            {
                var pawn = StoredPawns[i];
                var rect = StoredPawnRects[i];
                
                GUI.DrawTexture(rect, ColonistBar.BGTex);
                MoodThreshold m = MoodThresholdExtensions.CurrentMoodThresholdFor(pawn);

                float transparency = m < MoodThreshold.Major ? 0.1f : 0.15f;
                Widgets.DrawBoxSolid(rect, m.GetColor().ToTransparent(transparency));
                
                if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && Mouse.IsOver(rect))
                {
                    selected = true;
                    if (Event.current.clickCount == 2)
                    {
                        //double click
                    }
                    Event.current.Use();
                    Log.Message($"Pawn at index {i} -> {pawn.LabelShort}");
                    selectedPawnIdx = i;
                }

                if (i == selectedPawnIdx)
                {
                    Rect rect1 = rect.ContractedBy(-2f * Scale);
                    SelectionDrawerUtility.DrawSelectionOverlayOnGUI((object) pawn, rect, 0.4f * Scale, 20f * Scale);
                }

                var tex = PortraitsCache.Get(pawn, ColonistBarColonistDrawer.PawnTextureSize, Rot4.South,
                    ColonistBarColonistDrawer.PawnTextureCameraOffset, 1.28205f);
                
                GUI.DrawTexture(this.GetPawnTextureRect(rect.position), (Texture) tex);

                float num3 = 4f * Scale;
                Vector2 pos = new Vector2(rect.center.x, rect.yMax - num3);
                GenMapUI.DrawPawnLabel(pawn, pos, 1f, (float) (double) rect.width, this.pawnLabelsCache);
                Text.Font = GameFont.Small;
                GUI.color = Color.white;
            }
            
        }

        if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && !selected) selectedPawnIdx = -1;
        
        
        if (Event.current.type != EventType.MouseDown || Event.current.button != 1 || !selected)
            return;
        Event.current.Use();
    }

    public List<Rect> CalculateOptimalPawnRect(List<Pawn> pawns)
    {
        var rects = new List<Rect>();
        
        var height = Mathf.Clamp((MaxPawnBarHeight / pawns.Count)-PawnSpacing, MinPawnHeight, MaxPawnHeight);

        Scale = height / MaxPawnHeight;

        var width = Mathf.Clamp((MaxPawnWidth / MaxPawnHeight) * height, MinPawnWidth, MaxPawnWidth);

        var currentY = PawnBarOriginY;
        foreach (var pawn in pawns)
        {
            rects.Add(new Rect(PawnBarOriginX, currentY, width, height));
            currentY += height;
            currentY += PawnSpacing;
        }
        return rects;
    }
}