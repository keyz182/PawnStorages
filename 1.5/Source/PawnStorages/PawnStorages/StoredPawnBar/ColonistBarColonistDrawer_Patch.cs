using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace PawnStorages.StoredPawnBar;

//      GUI.DrawTexture(this.GetPawnTextureRect(rect.position), (Texture) PortraitsCache.Get(colonist, ColonistBarColonistDrawer.PawnTextureSize, Rot4.South, ColonistBarColonistDrawer.PawnTextureCameraOffset, 1.28205f));


[HarmonyPatch(typeof(ColonistBarColonistDrawer))]
public static class ColonistBarColonistDrawer_Patch
{
    public static Texture2D tex;
    
    [HarmonyPostfix]
    [HarmonyPatch(nameof(ColonistBarColonistDrawer.DrawColonist))]
    public static void DrawColonist_Patch(ColonistBarColonistDrawer __instance,
      Rect rect,
      Pawn colonist,
      Map pawnMap,
      bool highlight,
      bool reordering)
    {
        if (CompPawnStorage.IsPawnStored(colonist))
        {
          var pawnTexRect = __instance.GetPawnTextureRect(rect.position);
          var newRect = new Rect(pawnTexRect.xMax - 20f, pawnTexRect.yMax - 20f, 20f, 20f);

          if (ColonistBarColonistDrawer_Patch.tex == null)
          {
              var texture = new Texture2D(Mathf.CeilToInt(newRect.size.x), Mathf.CeilToInt(newRect.size.y));
              Color[] colors = texture.GetPixels();

              for (int i = 0; i < colors.Length; i++)
              {
                  colors[i] = new Color(1f, 0.0f, 0.0f, 1);
              }

              texture.SetPixels(colors);
              texture.Apply();

              tex = texture;
          }

          GUI.DrawTexture(newRect, tex);
        }
    }
    
    [HarmonyPrefix]
    [HarmonyPatch(nameof(ColonistBarColonistDrawer.ColonistBar), MethodType.Getter)]
    public static bool Prefix(ref ColonistBar __result, ColonistBarColonistDrawer __instance)
    {
        if (__instance is StoredPawnBarColonistDrawer drawer)
        {
            __result = drawer.ColonistBarOverride;
            return false;
        }

        return true;
    }
}