using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnStorages.StoredPawnBar;

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

            if (tex == null)
            {
                var texture = new Texture2D(Mathf.CeilToInt(newRect.size.x), Mathf.CeilToInt(newRect.size.y));
                var colors = texture.GetPixels();

                for (var i = 0; i < colors.Length; i++) colors[i] = new Color(1f, 0.0f, 0.0f, 1);

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
        if (__instance is not StoredPawnBarColonistDrawer drawer) return true;
        __result = drawer.ColonistBarOverride;
        return false;

    }
}