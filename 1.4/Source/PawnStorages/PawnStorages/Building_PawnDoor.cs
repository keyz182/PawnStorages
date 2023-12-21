using RimWorld;
using UnityEngine;
using Verse;

namespace PawnStorages;

public class Building_PawnDoor : Building_Door
{
    public override void Draw()
    {
        Rotation = DoorRotationAt(Position, Map);
        var d = 0f + 0.45f * OpenPct;
        for (var i = 0; i < 2; i++)
        {
            Vector3 vector;
            Mesh mesh;
            if (i == 0)
            {
                vector = new Vector3(0f, 0f, -1f);
                mesh = MeshPool.plane10;
            }
            else
            {
                vector = new Vector3(0f, 0f, 1f);
                mesh = MeshPool.plane10Flip;
            }

            Rot4 rotation = Rotation;
            rotation.Rotate(RotationDirection.Clockwise);
            vector = rotation.AsQuat * vector;
            Vector3 vector2 = DrawPos;
            vector2.y = def.altitudeLayer.AltitudeFor(); //AltitudeLayer.DoorMoveable.AltitudeFor();
            vector2 += vector * d;
            Graphics.DrawMesh(mesh, vector2, Rotation.AsQuat, Graphic.MatAt(Rotation), 0);
            Graphic_Shadow shadowGraphic = Graphic.ShadowGraphic;
            shadowGraphic?.DrawWorker(vector2, Rotation, def, this, 0f);
        }

        Comps_PostDraw();
    }
}
