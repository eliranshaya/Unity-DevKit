#if DEVKIT_ENABLED
using System.Collections;
using UnityEngine;

namespace DevKit.Internal
{
    /// <summary>
    /// A hidden, scene-persistent MonoBehaviour that exists purely so static modules have
    /// somewhere to run a coroutine. Created on demand - a project that never steps a frame never
    /// gets one.
    /// </summary>
    internal sealed class DevKitRunner : MonoBehaviour
    {
        static DevKitRunner _instance;

        internal static DevKitRunner Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject host = DevKitScene.NewRoot("Runner");
                    _instance = host.AddComponent<DevKitRunner>();
                }
                return _instance;
            }
        }

        internal static void Run(IEnumerator routine)
        {
            Instance.StartCoroutine(routine);
        }

        void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}
#endif
