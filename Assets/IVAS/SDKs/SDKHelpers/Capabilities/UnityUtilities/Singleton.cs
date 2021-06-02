using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using System.Threading;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SensorsSDK.UnityUtilities
{
    // TODO: Consider not using singletons.
    // @dkonik -- consider using more singletons

    /// <summary>
    /// Inherit from this base class to create a singleton.
    /// e.g. public class MyClassName : Singleton<MyClassName> {}
    /// </summary>
    public class Singleton<T> : MonoBehaviour where T : Singleton<T>
    {
        private static bool isShuttingDown = false;

        public static bool IsAvailable => !isShuttingDown && Instance != null;
        public static bool IsShuttingDown => isShuttingDown;

        public static bool IsInitialized = false;

        private static int unityMainThreadId;

        private static T _instance = null;
        public static T Instance
        {
            get
            {
#if UNITY_EDITOR
                // Allow singletons to come up in editor
                if (unityMainThreadId == Thread.CurrentThread.ManagedThreadId && !EditorApplication.isPlaying)
                {
                    isShuttingDown = false;
                }
#endif

                if (isShuttingDown)
                {
                    return null;
                }


                if (_instance == null)
                {
                    _instance = (T)FindObjectOfType(typeof(T));
                    if (_instance != null)
                    {
                        _instance.Initialize();
                        IsInitialized = true;
                    }
                    return _instance;
                }
                else
                {
                    return _instance;
                }
            }
            private set
            {
                _instance = value;
            }
        }

        /// <summary>
        /// If you are reading this because you're like "why can't I override awake?!", 
        /// it's because you're not supposed to. You should override Initialize,
        /// and treat that as your awake. - Adam and Dom #SWAG
        /// NOTE: DO NOT CHANGE THIS FROM PROTECTED. If yu make it private, we lose the warning that comes up when
        /// something improperly overrides it
        /// </summary>
        protected void Awake()
        {
            unityMainThreadId = Thread.CurrentThread.ManagedThreadId;
            isShuttingDown = false;
            if (Instance == null)
            {
                Instance = (T)this;
                Initialize();
            }
        }

        /// <summary>
        /// All inherited types that need to do any initialization should override
        /// this function and do it there
        /// </summary>
        protected virtual void Initialize() { }

        protected virtual void OnApplicationQuit()
        {
            isShuttingDown = true;
            Instance = null;
        }


        protected virtual void OnDestroy()
        {
            Instance = null;
        }
    }
}
