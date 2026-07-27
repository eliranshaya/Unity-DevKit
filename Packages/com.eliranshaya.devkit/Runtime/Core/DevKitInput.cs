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
        bool TryAttachUIModule(GameObject eventSystemHost);
    }

    /// <summary>
    /// Picks the right UI input module for the active backend.
    /// <para>
    /// DevKit polls no keys and reads no touches - the panel is opened by calling
    /// <see cref="DevKitBootstrap.Open"/>. All this type still does is make sure an EventSystem
    /// DevKit created can actually deliver clicks, which differs between the two backends.
    /// </para>
    /// </summary>
    public static class DevKitInput
    {
        static IDevKitInputProvider _provider;

        /// <summary>Called by the optional Input System assembly during subsystem registration.</summary>
        public static void SetProvider(IDevKitInputProvider provider)
        {
            _provider = provider;
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
    }
}
#endif
