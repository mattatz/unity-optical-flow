using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;

namespace OpticalFlow
{

    [System.Serializable]
    public class TextureUpdateEvent : UnityEvent<Texture> { }

    public class TextureUpdater : MonoBehaviour {

        [SerializeField] protected TextureUpdateEvent textureUpdateEvent;

    }

}


