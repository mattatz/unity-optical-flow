using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OpticalFlow.Demo
{

    public class FBO : System.IDisposable {

        public RenderTexture ReadBuffer { get { return buffers[read]; } }
        public RenderTexture WriteBuffer { get { return buffers[write]; } }

        protected RenderTexture[] buffers;
        protected int read = 0, write = 1;

        public FBO(int width, int height, RenderTextureFormat format =  RenderTextureFormat.ARGBFloat, FilterMode filterMode = FilterMode.Point)
        {
            buffers = new RenderTexture[2] {
                CreateBuffer(width, height, format, filterMode),
                CreateBuffer(width, height, format, filterMode)
            };
        }

        public void Swap()
        {
            int tmp = read;
            read = write;
            write = tmp;
        }

        protected RenderTexture CreateBuffer(int width, int height, RenderTextureFormat format, FilterMode filterMode)
        {
            var rt = new RenderTexture(width, height, 0);
            rt.format = format;
            rt.filterMode = filterMode;
            rt.wrapMode = TextureWrapMode.Repeat;
            rt.Create();
            return rt;
        }

        public void Dispose()
        {
            buffers[0].Release();
            buffers[1].Release();
        }

    }

}


