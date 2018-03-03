using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OpticalFlow.Demo
{

    [RequireComponent (typeof(Camera))]
    public class WebCam : MonoBehaviour {

        public int Width { get { return webCamTexture.width;  } }
        public int Height { get { return webCamTexture.height;  } }

        [SerializeField] protected WebCamTexture webCamTexture;
        [SerializeField] protected int width = 512, height = 512;

        void Start () {
            WebCamDevice userCameraDevice = WebCamTexture.devices[0];
            webCamTexture = new WebCamTexture(userCameraDevice.name, width, height);
            webCamTexture.Play();
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


