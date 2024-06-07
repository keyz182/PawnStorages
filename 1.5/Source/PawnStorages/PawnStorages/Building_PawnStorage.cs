﻿using System.Linq;
using Mono.Unix.Native;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Noise;

namespace PawnStorages;

public class Building_PawnStorage : PSBuilding
{
    public CompPawnStorage storageComp;

    public CompAssignableToPawn_PawnStorage compAssignable;

    public override bool ShouldUseAlternative => base.ShouldUseAlternative && !(storageComp?.StoredPawns.NullOrEmpty() ?? true);

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);
        storageComp = this.TryGetComp<CompPawnStorage>();
        compAssignable = this.TryGetComp<CompAssignableToPawn_PawnStorage>();
        // Set the default rotation
        storageComp.Rotation = Rotation;
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
        Pawn pawn = storageComp.StoredPawns.FirstOrDefault();
        if (storageComp.Props.showStoredPawn && pawn != null)
        {
            if (!compAssignable.Props.drawAsFrozenInCarbonite)
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

                pos += Graphic.DrawOffset(rot);
                pos += StatueOffset;

                //Somehow this magically fixes the flipping issue, just keeping it this way.
                mesh.SetUVs(false);
                Printer_Mesh.PrintMesh(layer, Matrix4x4.TRS(pos, Graphic.QuatFromRot(rot), s), mesh, mat);
            }
        }

        base.Print(layer);
    }

    public float statueOffsetZ = -0.25f;
    public float scale = 1.15f;
    
    public override void DrawAt(Vector3 drawLoc, bool flip = false)
    {
        base.DrawAt(drawLoc, flip);
        if (compAssignable.Props.drawAsFrozenInCarbonite && storageComp.StoredPawns.Count > 0)
        {
            Pawn pawn = storageComp.StoredPawns.First();
            RenderTexture texture = PortraitsCache.Get(pawn, new Vector2(175f, 175f), storageComp.Rotation.Rotated(RotationDirection.Opposite), new Vector3(0f, 0f, 0.1f), 1.5f, healthStateOverride: PawnHealthState.Mobile);
            
            var pos = DrawPos;
            pos.z += statueOffsetZ;
            pos.y = AltitudeLayer.BuildingOnTop.AltitudeFor();

            MaterialRequest req2 = default;
            req2.mainTex = texture.GetGreyscale();
            req2.shader = Graphic.data?.shaderType?.Shader;
            if (req2.shader == null) req2.shader = ShaderDatabase.DefaultShader;
            req2.color = DrawColor;
            req2.colorTwo = DrawColorTwo;

            Material mat = MaterialPool.MatFrom(req2);


            Matrix4x4 matrix = Matrix4x4.TRS(pos, Quaternion.Euler(0.0f, 0f, 0.0f), new Vector3(scale, 1f, scale));
            Graphics.DrawMesh(MeshPool.plane10, matrix, mat, 0);
        }
    }
}
