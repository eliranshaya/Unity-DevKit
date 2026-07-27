using UnityEngine;

#if DEVKIT_ENABLED
using DevKit.Internal;
#endif

namespace DevKit
{
    /// <summary>
    /// The one component a project needs. Drop it on an empty GameObject and open the panel by
    /// calling <see cref="Open"/> - from a UI Button's OnClick, or from your own code.
    /// <para>
    /// DevKit binds no input of its own. <see cref="Open"/>, <see cref="Close"/> and
    /// <see cref="Toggle"/> are public and parameterless, so they show up directly in a Button's
    /// OnClick dropdown. If you want a keyboard shortcut, call
    /// <c>DevActions.Toggle()</c> from wherever you already read input.
    /// </para>
    /// <para>
    /// This type is always compiled so that a stale GameObject left in a shipped scene does not
    /// become a missing script reference. With <c>DEVKIT_ENABLED</c> undefined it destroys itself
    /// in <c>Awake</c>, and <see cref="Open"/> does nothing.
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

        /// <summary>True while the panel is on screen.</summary>
        public bool IsOpen { get { return _panel != null && _panel.IsVisible; } }

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
            // for the first Open. There is no Update either: nothing is polled.
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

        /// <summary>
        /// Opens the panel, building it on first use. Hook this straight to a UI Button's OnClick.
        /// </summary>
        public void Open()
        {
            if (_panel == null)
            {
                // First open pays for everything: the scan, then the canvas.
                DevActionRegistry.ScanAssemblies();
                _panel = DevPanel.Build(_pauseWhenOpen);
            }

            _panel.SetVisible(true);
        }

        /// <summary>Closes the panel if it is open. Safe to call when it was never built.</summary>
        public void Close()
        {
            if (_panel != null)
            {
                _panel.SetVisible(false);
            }
        }

        /// <summary>Opens the panel if it is closed, closes it if it is open.</summary>
        public void Toggle()
        {
            if (IsOpen)
            {
                Close();
            }
            else
            {
                Open();
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
            DevKitLog.Warning(string.Format(
                "Cannot {0} the panel: no DevKitBootstrap in the scene. " +
                "Add one through GameObject > Dev > Add DevKit Bootstrap.", what));
        }
#else
        /// <summary>
        /// No-op when <c>DEVKIT_ENABLED</c> is undefined, so a Button still wired to this method in
        /// a shipped scene does nothing instead of throwing a missing-method error.
        /// </summary>
        public void Open()
        {
        }

        /// <inheritdoc cref="Open"/>
        public void Close()
        {
        }

        /// <inheritdoc cref="Open"/>
        public void Toggle()
        {
        }

        void Awake()
        {
            // DEVKIT_ENABLED is undefined: leave no trace, not even an idle GameObject.
            Destroy(gameObject);
        }
#endif
    }
}
