using UnityEngine;

#if DEVKIT_ENABLED
using DevKit.Internal;
#endif

namespace DevKit
{
    /// <summary>
    /// The one component a project needs. Drop it on an empty GameObject, press the hotkey, and
    /// the panel builds itself.
    /// <para>
    /// This type is always compiled so that a stale GameObject left in a shipped scene does not
    /// become a missing script reference. With <c>DEVKIT_ENABLED</c> undefined it destroys itself
    /// in <c>Awake</c> and has no <c>Update</c> at all.
    /// </para>
    /// </summary>
    [AddComponentMenu("DevKit/DevKit Bootstrap")]
    [DisallowMultipleComponent]
    public sealed class DevKitBootstrap : MonoBehaviour
    {
#if !DEVKIT_ENABLED
        // The fields stay compiled so a scene keeps its serialized values across a release build,
        // but with the symbol off nothing reads them. Silence CS0414 rather than ship warnings.
#pragma warning disable 0414
#endif

        [Header("Toggle")]
        [SerializeField]
        [Tooltip("Keyboard key that opens and closes the panel.")]
        DevKey _toggleKey = DevKey.F1;

        [SerializeField]
        [Tooltip("Open the panel by holding several fingers on the screen. For phones and tablets.")]
        bool _mobileGesture = true;

        [SerializeField, Range(2, 5)]
        [Tooltip("How many fingers the gesture needs.")]
        int _gestureFingers = 3;

        [SerializeField, Range(0.1f, 2f)]
        [Tooltip("How long those fingers must stay down, in unscaled seconds.")]
        float _gestureHold = 0.5f;

        [Header("Behaviour")]
        [SerializeField]
        [Tooltip("Survive scene loads. Leave on unless something else already owns a persistent root.")]
        bool _dontDestroyOnLoad = true;

        [SerializeField]
        [Tooltip("Set Time.timeScale to 0 while the panel is open. The previous value is restored on close.")]
        bool _pauseWhenOpen = false;

        [SerializeField]
        [Tooltip("Open the panel immediately on the first frame. Handy while iterating on the panel itself.")]
        bool _openOnStart = false;

#if !DEVKIT_ENABLED
#pragma warning restore 0414
#endif

#if DEVKIT_ENABLED
        static DevKitBootstrap _instance;

        DevPanel _panel;
        float _gestureTimer;
        bool _gestureFired;

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;

            if (_dontDestroyOnLoad)
            {
                if (transform.parent != null)
                {
                    transform.SetParent(null, true);
                }

                DontDestroyOnLoad(gameObject);
            }

            // Deliberately nothing else here. No reflection scan, no UI construction - both wait
            // for the first toggle.
        }

        void Start()
        {
            if (_openOnStart)
            {
                Open();
            }
        }

        void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        void Update()
        {
            if (DevKitInput.GetKeyDown(_toggleKey))
            {
                Toggle();
                return;
            }

            if (_panel != null && _panel.IsVisible && _toggleKey != DevKey.Escape && DevKitInput.GetKeyDown(DevKey.Escape))
            {
                Close();
                return;
            }

            if (_mobileGesture)
            {
                UpdateGesture();
            }
        }

        void UpdateGesture()
        {
            if (DevKitInput.TouchCount >= _gestureFingers)
            {
                if (_gestureFired)
                {
                    return;
                }

                _gestureTimer += Time.unscaledDeltaTime;
                if (_gestureTimer >= _gestureHold)
                {
                    _gestureFired = true;
                    Toggle();
                }
            }
            else
            {
                _gestureTimer = 0f;
                _gestureFired = false;
            }
        }

        void Toggle()
        {
            if (_panel != null && _panel.IsVisible)
            {
                Close();
            }
            else
            {
                Open();
            }
        }

        void Open()
        {
            if (_panel == null)
            {
                // First open pays for everything: the scan, then the canvas.
                DevActionRegistry.ScanAssemblies();
                _panel = DevPanel.Build(_pauseWhenOpen);
            }

            _panel.SetVisible(true);
        }

        void Close()
        {
            if (_panel != null)
            {
                _panel.SetVisible(false);
            }
        }

        internal static void RequestOpen()
        {
            if (_instance != null) _instance.Open();
            else WarnNoBootstrap("open");
        }

        internal static void RequestClose()
        {
            if (_instance != null) _instance.Close();
        }

        internal static void RequestToggle()
        {
            if (_instance != null) _instance.Toggle();
            else WarnNoBootstrap("toggle");
        }

        static void WarnNoBootstrap(string what)
        {
            DevKitLog.Warning(string.Format("Cannot {0} the panel: no DevKitBootstrap in the scene. " + "Add one through GameObject > Dev > Add DevKit Bootstrap.", what));
        }
#else
        void Awake()
        {
            // DEVKIT_ENABLED is undefined: leave no trace, not even an idle GameObject.
            Destroy(gameObject);
        }
#endif
    }
}