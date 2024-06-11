using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnStorages;

public class Building_PawnStorage : PSBuilding, IThingHolder
{
    public CompPawnStorage storageComp;

    public CompAssignableToPawn_PawnStorage compAssignable;

    public ThingOwner innerContainer;
    public Building_PawnStorage()
    {
        this.innerContainer = new ThingOwner<Pawn>((IThingHolder) this);
    }
    public override bool ShouldUseAlternative => base.ShouldUseAlternative && !innerContainer.NullOrEmpty();

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
        Pawn pawn = (Pawn)innerContainer.FirstOrDefault();
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
        if (compAssignable.Props.drawAsFrozenInCarbonite && innerContainer.Count > 0)
        {
            Pawn pawn = (Pawn)innerContainer.First();
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
    public void GetChildHolders(List<IThingHolder> outChildren)
    {
        ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, (IList<Thing>) this.GetDirectlyHeldThings());
    }

    public ThingOwner GetDirectlyHeldThings() => this.innerContainer;

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (var gizmo in base.GetGizmos())
        {
            yield return gizmo;
        }
        foreach (Thing thing in (IEnumerable<Thing>) innerContainer)
        {
            Gizmo gizmo;
            if ((gizmo = Building.SelectContainedItemGizmo(thing, thing)) != null)
                yield return gizmo;
        }
        
    }
    

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Deep.Look<ThingOwner>(ref this.innerContainer, "innerContainer", (object) this);
    }
}
