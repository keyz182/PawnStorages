using RimWorld;
using UnityEngine;
using Verse;

namespace PawnStorages;

public class Building_PawnDoor : Building_Door
{
    public override void DrawAt(Vector3 drawLoc, bool flip = false)
    {
        DoorPreDraw();
        float offsetDist = (float) (0.0 + 0.45 * OpenPct);
        DrawMovers(drawLoc, offsetDist, Graphic, def.altitudeLayer.AltitudeFor(), Vector3.one, Graphic.ShadowGraphic);
    }
}
