using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace PawnStorages.Farm.Comps
{
    public class CompProperties_FarmNutrition : CompProperties
    {
        // production
        public float maxNutrition => PawnStoragesMod.settings.MaxFarmStoredNutrition;
        public int ticksToAbsorbNutrients => PawnStoragesMod.settings.TicksToAbsorbNutrients;
        public int animalTickInterval => PawnStoragesMod.settings.AnimalTickInterval;

        public bool HasTip;
        public float TipScale;
        public string MainTex;
        public string AltTex;

        public string Tip;
        public string SansTip;
        public string AltTip;
        public string AltSansTip;

        public static Dictionary<string, Material> MaterialCache = new Dictionary<string, Material>();

        public Material MainTexture
        {
            get
            {
                string matPath = "";
                if (!HasTip)
                {
                    matPath = PawnStoragesMod.settings.RusticFarms ? AltTex : MainTex;
                }

                else
                {
                    matPath = PawnStoragesMod.settings.RusticFarms ? AltSansTip : SansTip;
                }

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
                string matPath = PawnStoragesMod.settings.RusticFarms ? AltTip : Tip;

                if (MaterialCache.TryGetValue(matPath, out Material mat))
                    return mat;

                mat = MaterialPool.MatFrom(matPath, ShaderDatabase.Transparent, Color.white);

                MaterialCache[matPath] = mat;

                return mat;
            }
        }

        public CompProperties_FarmNutrition()
        {
            compClass = typeof(CompFarmNutrition);
        }
    }
}
