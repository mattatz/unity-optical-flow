using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OpticalFlow.Demo
{

    public class WebCamUpdater : TextureUpdater {

        public int Width { get { return webCamTexture.width;  } }
        public int Height { get { return webCamTexture.height;  } }

        [SerializeField] protected WebCamTexture webCamTexture;
        [SerializeField] protected int width = 512, height = 512;

        void Start () {
            WebCamDevice userCameraDevice = WebCamTexture.devices[0];
            webCamTexture = new WebCamTexture(userCameraDevice.name, width, height);
            webCamTexture.Play();
        }

        void Update ()
        {
            if(webCamTexture != null && webCamTexture.didUpdateThisFrame)
            {
                textureUpdateEvent.Invoke(webCamTexture);
            }
        }

        void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            Graphics.Blit(webCamTexture, destination);
        }

        protected void OnDestroy()
        {
        }

    }

}


