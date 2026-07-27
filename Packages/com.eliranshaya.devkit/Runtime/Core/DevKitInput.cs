#if DEVKIT_ENABLED
using UnityEngine;
using UnityEngine.EventSystems;

namespace DevKit.Internal
{
    /// <summary>
    /// Implemented by the optional <c>Core.DevKit.InputSystem</c> assembly, which only compiles
    /// when <c>com.unity.inputsystem</c> is installed. Core never references the Input System
    /// directly, so a project without the package still builds.
    /// </summary>
    public interface IDevKitInputProvider
    {
        bool GetKeyDown(DevKey key);
        int TouchCount { get; }
        bool TryAttachUIModule(GameObject eventSystemHost);
    }

    /// <summary>
    /// Backend neutral input. Both backends can be enabled at once, so the legacy result and the
    /// provider result are OR'd rather than picked between.
    /// </summary>
    public static class DevKitInput
    {
        static IDevKitInputProvider _provider;

        /// <summary>Called by the optional Input System assembly during subsystem registration.</summary>
        public static void SetProvider(IDevKitInputProvider provider)
        {
            _provider = provider;
        }

        internal static bool GetKeyDown(DevKey key)
        {
            if (key == DevKey.None)
            {
                return false;
            }

#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(ToKeyCode(key)))
            {
                return true;
            }
#endif
            return _provider != null && _provider.GetKeyDown(key);
        }

        /// <summary>Number of fingers currently down, across whichever backend reports them.</summary>
        internal static int TouchCount
        {
            get
            {
#if ENABLE_LEGACY_INPUT_MANAGER
                int legacy = Input.touchCount;
                if (legacy > 0)
                {
                    return legacy;
                }
#endif
                return _provider != null ? _provider.TouchCount : 0;
            }
        }

        /// <summary>
        /// Adds an input module to an EventSystem <em>we created</em>. The Input System module is
        /// preferred because it also works when both backends are active; the legacy module throws
        /// at runtime in Input-System-only projects.
        /// </summary>
        internal static void AttachUIModule(GameObject eventSystemHost)
        {
            if (_provider != null && _provider.TryAttachUIModule(eventSystemHost))
            {
                return;
            }

#if ENABLE_LEGACY_INPUT_MANAGER
            if (eventSystemHost.GetComponent<StandaloneInputModule>() == null)
            {
                eventSystemHost.AddComponent<StandaloneInputModule>();
            }
#else
            DevKitLog.Warning(
                "No usable UI input module. Install com.unity.inputsystem or enable the legacy " +
                "input manager, otherwise the panel will render but not respond to clicks.");
#endif
        }

#if ENABLE_LEGACY_INPUT_MANAGER
        static KeyCode ToKeyCode(DevKey key)
        {
            switch (key)
            {
                case DevKey.F1: return KeyCode.F1;
                case DevKey.F2: return KeyCode.F2;
                case DevKey.F3: return KeyCode.F3;
                case DevKey.F4: return KeyCode.F4;
                case DevKey.F5: return KeyCode.F5;
                case DevKey.F6: return KeyCode.F6;
                case DevKey.F7: return KeyCode.F7;
                case DevKey.F8: return KeyCode.F8;
                case DevKey.F9: return KeyCode.F9;
                case DevKey.F10: return KeyCode.F10;
                case DevKey.F11: return KeyCode.F11;
                case DevKey.F12: return KeyCode.F12;
                case DevKey.BackQuote: return KeyCode.BackQuote;
                case DevKey.Tab: return KeyCode.Tab;
                case DevKey.Escape: return KeyCode.Escape;
                case DevKey.Insert: return KeyCode.Insert;
                case DevKey.Home: return KeyCode.Home;
                case DevKey.End: return KeyCode.End;
                case DevKey.Pause: return KeyCode.Pause;
                default: return KeyCode.None;
            }
        }
#endif
    }
}
#endif
