using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OpticalFlow
{

    [RequireComponent (typeof(Camera))]
    public class OpticalFlow : MonoBehaviour {

        protected enum Pass {
            Flow = 0,
            DownSample = 1,
            BlurH = 2,
            BlurV = 3,
            Visualize = 4
        };

        [SerializeField] protected Material flowMaterial;
        [SerializeField, Range(0, 6)] int blurIterations = 1, blurDownSample = 0;
        [SerializeField] protected bool debug;

        protected RenderTexture prev, flowBuffer;

        #region MonoBehaviour functions

        protected void Start () {
        }
        
        protected void Update () {
        }

        protected void OnRenderImage(RenderTexture current, RenderTexture destination)
        {
            if(prev == null) {
                Setup(current.width, current.height);
                Graphics.Blit(current, prev);
            }

            flowMaterial.SetTexture("_PrevTex", prev);
            flowMaterial.SetFloat("_Ratio", 1f * Screen.height / Screen.width);

            Graphics.Blit(current, flowBuffer, flowMaterial, (int)Pass.Flow);
            Graphics.Blit(current, prev);

            // Graphics.Blit(flowBuffer, destination, flowMaterial, (int)Pass.Visualize);

            // Blur and visualize flow
            var downSampled = DownSample(flowBuffer, blurDownSample);
            Blur(downSampled, blurIterations);
            Graphics.Blit(downSampled, destination, flowMaterial, (int)Pass.Visualize);

            RenderTexture.ReleaseTemporary(downSampled);
        }

        protected void OnDestroy ()
        {
            prev.Release();
            flowBuffer.Release();
        }

        protected void OnGUI ()
        {
            if (!debug) return;

            const int offset = 10;
            const int width = 176, height = 144;
            GUI.DrawTexture(new Rect(offset, offset, width, height), prev);
            GUI.DrawTexture(new Rect(offset, offset + height, width, height), flowBuffer);
        }

        #endregion

        protected void Setup(int width, int height)
        {
            prev = new RenderTexture(width, height, 0);
            prev.format = RenderTextureFormat.ARGBFloat;
            prev.wrapMode = TextureWrapMode.Repeat;
            prev.Create();

            flowBuffer = new RenderTexture(width, height, 0);
            flowBuffer.format = RenderTextureFormat.ARGBFloat;
            flowBuffer.wrapMode = TextureWrapMode.Repeat;
            flowBuffer.Create();
        }

        RenderTexture DownSample(RenderTexture source, int lod)
        {
            var dst = RenderTexture.GetTemporary(source.width, source.height, 0, source.format);
            source.filterMode = FilterMode.Bilinear;
            Graphics.Blit(source, dst);

            for (var i = 0; i < lod; i++)
            {
                var tmp = RenderTexture.GetTemporary(dst.width >> 1, dst.height >> 1, 0, dst.format);
                dst.filterMode = FilterMode.Bilinear;
                Graphics.Blit(dst, tmp, flowMaterial, (int)Pass.DownSample);
                RenderTexture.ReleaseTemporary(dst);
                dst = tmp;
            }

            return dst;
        }

        void Blur(RenderTexture source, int iterations)
        {
            var tmp0 = RenderTexture.GetTemporary(source.width, source.height, 0, source.format);
            var tmp1 = RenderTexture.GetTemporary(source.width, source.height, 0, source.format);
            var iters = Mathf.Clamp(iterations, 0, 10);

            Graphics.Blit(source, tmp0);
            for (var i = 0; i < iters; i++)
            {
                for (var pass = 2; pass < 4; pass++)
                {
                    tmp1.DiscardContents();
                    tmp0.filterMode = FilterMode.Bilinear;
                    Graphics.Blit(tmp0, tmp1, flowMaterial, pass);
                    var tmpSwap = tmp0;
                    tmp0 = tmp1;
                    tmp1 = tmpSwap;
                }
            }
            Graphics.Blit(tmp0, source);

            RenderTexture.ReleaseTemporary(tmp0);
            RenderTexture.ReleaseTemporary(tmp1);
        }

        protected RenderTexture CreateBuffer(int width, int height)
        {
            var rt = new RenderTexture(width, height, 0);
            rt.format = RenderTextureFormat.ARGBFloat;
            rt.wrapMode = TextureWrapMode.Repeat;
            rt.Create();
            return rt;
        }

    }

}


