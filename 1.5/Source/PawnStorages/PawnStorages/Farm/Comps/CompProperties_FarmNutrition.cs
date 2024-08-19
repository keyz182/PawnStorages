using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace PawnStorages.Farm.Comps;

public class CompProperties_FarmNutrition : CompProperties_PawnStorageNutrition
{
    // production
    public int animalTickInterval => PawnTickInterval;

    public bool HasTip;
    public float TipScale;
    public string MainTex;
    public string AltTex;

    public string Tip;
    public string SansTip;

    public static Dictionary<string, Material> MaterialCache = new();

    public Material MainTexture
    {
        get
        {
            string matPath = !HasTip ? MainTex : SansTip;

            if (MaterialCache.TryGetValue(matPath, out Material mat))
                return mat;

            mat = MaterialPool.MatFrom(matPath, ShaderDatabase.Transparent, Color.white);

            MaterialCache[matPath] = mat;

            return mat;

        }
    }

    public Material TipTexture
    {
        get
        {
            if (MaterialCache.TryGetValue(Tip, out Material mat))
                return mat;

            mat = MaterialPool.MatFrom(Tip, ShaderDatabase.Transparent, Color.white);

            MaterialCache[Tip] = mat;

            return mat;
        }
    }

    public CompProperties_FarmNutrition() => compClass = typeof(CompFarmNutrition);
}
