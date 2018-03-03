using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OpticalFlow
{

    public class OpticalFlow : MonoBehaviour {

        protected enum Pass {
            Flow = 0,
            DownSample = 1,
            BlurH = 2,
            BlurV = 3,
            Visualize = 4
        };

        public RenderTexture Flow { get { return resultBuffer; } }

        [SerializeField] protected Material flowMaterial;
        [SerializeField, Range(0, 6)] int blurIterations = 0, blurDownSample = 0;
        [SerializeField] protected bool debug;

        protected RenderTexture prevFrame, flowBuffer, resultBuffer;

        #region MonoBehaviour functions

        protected void Start () {
        }

        protected void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            Graphics.Blit(resultBuffer, destination, flowMaterial, (int)Pass.Visualize);
        }

        protected void OnDestroy ()
        {
            if(prevFrame != null)
            {
                prevFrame.Release();
                prevFrame = null;

                flowBuffer.Release();
                flowBuffer = null;

                resultBuffer.Release();
                resultBuffer = null;
            }
        }

        protected void OnGUI ()
        {
            if (!debug || prevFrame == null || flowBuffer == null) return;

            const int offset = 10;
            const int width = 176, height = 144;
            GUI.DrawTexture(new Rect(offset, offset, width, height), prevFrame);
            GUI.DrawTexture(new Rect(offset, offset + height, width, height), flowBuffer);
        }

        #endregion

        protected void Setup(int width, int height)
        {
            prevFrame = new RenderTexture(width, height, 0);
            prevFrame.format = RenderTextureFormat.ARGBFloat;
            prevFrame.wrapMode = TextureWrapMode.Repeat;
            prevFrame.Create();

            flowBuffer = new RenderTexture(width, height, 0);
            flowBuffer.format = RenderTextureFormat.ARGBFloat;
            flowBuffer.wrapMode = TextureWrapMode.Repeat;
            flowBuffer.Create();

            resultBuffer = new RenderTexture(width >> blurDownSample, height >> blurDownSample, 0);
            resultBuffer.format = RenderTextureFormat.ARGBFloat;
            resultBuffer.wrapMode = TextureWrapMode.Repeat;
            resultBuffer.Create();
        }

        public void Calculate(Texture current)
        {
            if(prevFrame == null) {
                Setup(current.width, current.height);
                Graphics.Blit(current, prevFrame);
            }

            flowMaterial.SetTexture("_PrevTex", prevFrame);
            flowMaterial.SetFloat("_Ratio", 1f * Screen.height / Screen.width);

            Graphics.Blit(current, flowBuffer, flowMaterial, (int)Pass.Flow);
            Graphics.Blit(current, prevFrame);

            // Graphics.Blit(flowBuffer, destination, flowMaterial, (int)Pass.Visualize);

            // Blur and visualize flow
            var downSampled = DownSample(flowBuffer, blurDownSample);
            Blur(downSampled, blurIterations);
            // Graphics.Blit(downSampled, destination, flowMaterial, (int)Pass.Visualize);
            Graphics.Blit(downSampled, resultBuffer);

            RenderTexture.ReleaseTemporary(downSampled);
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


