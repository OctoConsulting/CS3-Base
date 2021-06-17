using UnityEngine;
using SensorsSDK.WmrUtilities;

namespace SensorsSDK.UnityUtilities
{
    /// <summary>
    /// A class that provides the main unity camera so classes don't have to call
    /// Camera.main over and over
    /// </summary>
    public class CameraProvider : Singleton<CameraProvider>
    {
        private Camera mainCamera = null;

        public static Camera MainCamera
        {
            get
            {
                if (Instance == null)
                {
                    return Camera.main;
                }

                if (Instance.mainCamera == null)
                {
                    Instance.mainCamera = Camera.main;
                }

                return Instance.mainCamera;
            }
        }

        protected override void Initialize()
        {
            Instance.mainCamera = Camera.main;
        }
    }
}

