using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using Random = UnityEngine.Random;

namespace OpticalFlow.Demo
{

    public class GPUParticleSystem : MonoBehaviour {

        protected enum ComputePass
        {
            PositionUpdate = 0,
            VelocityUpdate = 1,
            PositionInit = 2,
            VelocityInit = 3
        };

        [SerializeField] protected OpticalFlow flow;
        [SerializeField] protected int width = 128, height = 128;
        [SerializeField] protected Material compute, render;
        [SerializeField] protected bool debug;

        protected Mesh mesh;

        FBO positionFBO, velocityFBO;

        protected const string kPositionKey = "_Position", kVelocityKey = "_Velocity";
        protected const string kFlowKey = "_Flow";

        #region Monobehaviour functions

        protected void Start () {
            positionFBO = new FBO(width, height);
            velocityFBO = new FBO(width, height);
            mesh = Build(width, height);

            Init();
        }
        
        protected void Update () {
            if (flow.Flow == null) return;

            Compute(Time.deltaTime);

            render.SetTexture(kPositionKey, positionFBO.ReadBuffer);
            render.SetTexture(kVelocityKey, velocityFBO.ReadBuffer);
            // Graphics.DrawMesh(mesh, transform.position, transform.rotation, render, 0);
            Graphics.DrawMesh(mesh, transform.localToWorldMatrix, render, 0, null, 0);
        }

        protected void OnDestroy ()
        {
            positionFBO.Dispose();
            velocityFBO.Dispose();
        }

        protected void OnGUI ()
        {
            if (!debug) return;

            const int offset = 10;
            const int size = 128;
            GUI.DrawTexture(new Rect(offset, offset, size, size), positionFBO.ReadBuffer);
            GUI.DrawTexture(new Rect(offset, offset + size, size, size), velocityFBO.ReadBuffer);
            GUI.DrawTexture(new Rect(offset, offset + size * 2, size, size), flow.Flow);
        }

        #endregion

        protected Mesh Build(int width, int height)
        {
            var mesh = new Mesh();

            var vertices = new List<Vector3>();
            var texcoords = new List<Vector2>();
            var texcoords2 = new List<Vector2>();
            var indices = new List<int>();

            var invH = 1f / height;
            var invW = 1f / width;

            Vector3 p0 = new Vector3(-0.5f * invW, 0.5f * invH, 0), p1 = new Vector3(0.5f * invW, 0.5f * invH, 0), p2 = new Vector3(0.5f * invW, -0.5f * invH, 0), p3 = new Vector3(-0.5f * invW, -0.5f * invH, 0);
            Vector2 uv0 = new Vector2(0f, 0f), uv1 = new Vector2(1f, 0f), uv2 = new Vector2(1f, 1f), uv3 = new Vector2(0f, 1f);

            for(int y = 0; y < height; y++)
            {
                var v = y * invH;
                for(int x = 0; x < width; x++)
                {
                    var u = x * invW;

                    var uv = new Vector2(u, v);
                    int i0 = vertices.Count, i1 = i0 + 1, i2 = i1 + 1, i3 = i2 + 1;

                    vertices.Add(p0); vertices.Add(p1); vertices.Add(p2); vertices.Add(p3);
                    texcoords.Add(uv); texcoords.Add(uv); texcoords.Add(uv); texcoords.Add(uv);
                    texcoords2.Add(uv0); texcoords2.Add(uv1); texcoords2.Add(uv2); texcoords2.Add(uv3);

                    indices.Add(i0); indices.Add(i2); indices.Add(i1);
                    indices.Add(i2); indices.Add(i0); indices.Add(i3);
                }
            }

            mesh.vertices = vertices.ToArray();
            mesh.uv = texcoords.ToArray();
            mesh.uv2 = texcoords2.ToArray();
            mesh.SetIndices(indices.ToArray(), MeshTopology.Triangles, 0);
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 1000f);
            return mesh;
        }

        protected void Init()
        {
            Graphics.Blit(null, velocityFBO.ReadBuffer, compute, (int)ComputePass.VelocityInit);
            Graphics.Blit(null, positionFBO.ReadBuffer, compute, (int)ComputePass.PositionInit);
        }

        protected void Compute(float dt)
        {
            compute.SetTexture(kPositionKey, positionFBO.ReadBuffer);
            compute.SetTexture(kVelocityKey, velocityFBO.ReadBuffer);
            compute.SetTexture(kFlowKey, flow.Flow);

            Graphics.Blit(velocityFBO.ReadBuffer, velocityFBO.WriteBuffer, compute, (int)ComputePass.VelocityUpdate);
            Graphics.Blit(positionFBO.ReadBuffer, positionFBO.WriteBuffer, compute, (int)ComputePass.PositionUpdate);

            positionFBO.Swap();
            velocityFBO.Swap();
        }

        public void Capture(Texture texture)
        {
            render.SetTexture("_MainTex", texture);
        }

    }

}


