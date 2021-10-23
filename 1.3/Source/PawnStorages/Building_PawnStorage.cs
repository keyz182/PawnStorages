using RimWorld;
using System.Linq;
using UnityEngine;
using Verse;

namespace PawnStorages
{
    public class Building_PawnStorage : Building
    {
        CompPawnStorage comp;
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            comp = this.TryGetComp<CompPawnStorage>();
        }

        public override void Draw()
        {
            if (comp.Props.showStoredPawn && comp.StoredPawns.Any())
            {
                var pos = this.DrawPos;
                pos.y += 0.1f;
                var pawn = comp.StoredPawns.First();
                var rot = this.Rotation;
                var texture = PortraitsCache.Get(pawn, new Vector2(100f, 100f), rot, new Vector3(0f, 0f, 0.1f), 1.5f);
                MaterialRequest req2 = default(MaterialRequest);
                req2.mainTex = MakeReadableTextureInstance(texture);
                req2.shader = this.Graphic.data.shaderType.Shader;
                req2.color = this.DrawColor;
                req2.colorTwo = this.DrawColorTwo;
                var mat = MaterialPool.MatFrom(req2);
                Mesh mesh = this.Graphic.MeshAt(rot);
                Quaternion quat = this.Graphic.QuatFromRot(rot);
                pos += this.Graphic.DrawOffset(rot);
                Matrix4x4 matrix = default(Matrix4x4);
                Vector3 s = new Vector3(1.3f, 1f, 1.3f);
                matrix.SetTRS(pos, quat, s);
                Graphics.DrawMesh(mesh, matrix, mat, 0);
            }
            else
            {
                base.Draw();
            }
        }
        public static Texture2D MakeReadableTextureInstance(RenderTexture source)
        {
            RenderTexture temporary = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
            temporary.name = "MakeReadableTexture_Temp";
            Graphics.Blit(source, temporary);
            RenderTexture active = RenderTexture.active;
            RenderTexture.active = temporary;
            Texture2D texture2D = new Texture2D(source.width, source.height);
            texture2D.ReadPixels(new Rect(0f, 0f, temporary.width, temporary.height), 0, 0);
            texture2D.Apply();
            RenderTexture.active = active;
            RenderTexture.ReleaseTemporary(temporary);
            return texture2D;
        }
    }
}
