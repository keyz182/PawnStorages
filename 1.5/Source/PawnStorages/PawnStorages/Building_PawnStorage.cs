﻿using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnStorages;

public class Building_PawnStorage : PSBuilding
{
    public CompPawnStorage storageComp;

    public CompAssignableToPawn_PawnStorage compAssignable;

    public override bool ShouldUseAlternative => base.ShouldUseAlternative && !storageComp.GetDirectlyHeldThings().NullOrEmpty();

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);
        storageComp = this.TryGetComp<CompPawnStorage>();
        compAssignable = this.TryGetComp<CompAssignableToPawn_PawnStorage>();

        if (storageComp == null)
            Log.Warning($"{this} has null CompPawnStorage even though of type {nameof(Building_PawnStorage)}");
    }

    /*
    [TweakValue("STATUE ROTATION", 0, 4)]
    public static int ROTATION = 0;

    [TweakValue("STATUE FLIPPING", 0f, 100f)]
    public static bool FLIPBOOL = false;
    */

    public override void Print(SectionLayer layer)
    {
        Pawn pawn = (Pawn)storageComp.GetDirectlyHeldThings().FirstOrDefault();
        if (storageComp.Props.showStoredPawn && pawn != null)
        {
            Vector3 pos = DrawPos;
            pos.y += Altitudes.AltInc;

            // Grab the set rotation from the storage comp so the user can tweak the rotation
            Rot4 rot = storageComp.Rotation;
            // Pass in PawnHealthState.Mobile as an override to ensure the pawn is drawn upright
            RenderTexture texture = PortraitsCache.Get(pawn, new Vector2(175f, 175f), rot, new Vector3(0f, 0f, 0.1f), 1.5f, healthStateOverride: PawnHealthState.Mobile);

            MaterialRequest req2 = default;
            req2.mainTex = texture.GetGreyscale();
            req2.shader = Graphic.data?.shaderType?.Shader;
            if (req2.shader == null) req2.shader = ShaderDatabase.DefaultShader;
            req2.color = DrawColor;
            req2.colorTwo = DrawColorTwo;

            Mesh mesh = Object.Instantiate(Graphic.MeshAt(rot));
            Material mat = MaterialPool.MatFrom(req2);
            Vector3 s = new(1.3f, 1f, 1.3f);

            if (compAssignable.Props.drawAsFrozenInCarbonite)
            {
                s.z *= 0.5f;
                pos += StatueOffset.RotatedBy(Rotation);
            }

            pos += Graphic.DrawOffset(rot);
            pos += StatueOffset;

            //Somehow this magically fixes the flipping issue, just keeping it this way.
            mesh.SetUVs(false);
            Printer_Mesh.PrintMesh(layer, Matrix4x4.TRS(pos, Rotation.AsQuat, s), mesh, mat);
        }

        base.Print(layer);
    }
}
