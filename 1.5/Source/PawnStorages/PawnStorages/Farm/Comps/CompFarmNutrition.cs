using UnityEngine;
using Verse;

namespace PawnStorages.Farm.Comps;

[StaticConstructorOnStartup]
public class CompFarmNutrition : CompPawnStorageNutrition
{
    public bool doesBreeding => parent.GetComp<CompFarmBreeder>() != null;

    public new CompProperties_FarmNutrition Props => props as CompProperties_FarmNutrition;

    public override void PostDraw()
    {
        ((Graphic_Single) parent.Graphic).mat = Props.MainTexture;

        base.PostDraw();

        if (!doesBreeding || !PawnStoragesMod.settings.SuggestiveSilo)
            return;

        float filled = (storedNutrition / Props.MaxNutrition) * 0.6f;

        Vector3 pos = parent.DrawPos;
        pos.z += filled;
        pos.y = AltitudeLayer.BuildingOnTop.AltitudeFor();

        Matrix4x4 matrix = Matrix4x4.TRS(pos, Quaternion.Euler(0.0f, 0f, 0.0f), new Vector3(Props.TipScale, 1f, Props.TipScale));
        Graphics.DrawMesh(MeshPool.plane10, matrix, Props.TipTexture, 0);
    }
}
