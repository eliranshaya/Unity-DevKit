#if DEVKIT_ENABLED
using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DevKit.Internal
{
    /// <summary>
    /// Gives an EventSystem that DevKit created a UI input module, so the panel actually receives
    /// clicks. That is the only thing DevKit still wants from an input backend.
    /// <para>
    /// An EventSystem the project already owns is never touched — this runs only when DevKit had
    /// to create one because the scene had none.
    /// </para>
    /// </summary>
    internal static class DevKitInput
    {
        /// <summary>
        /// Resolved by reflection rather than by an assembly reference. <c>Core.DevKit</c> must
        /// never reference <c>com.unity.inputsystem</c>: an asmdef reference to a package that is
        /// not installed stops the assembly compiling, which would break every project without it.
        /// Reflection costs one type lookup, once, and only when DevKit creates an EventSystem.
        /// </summary>
        const string InputSystemUIModule =
            "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem";

        internal static void AttachUIModule(GameObject eventSystemHost)
        {
#if ENABLE_INPUT_SYSTEM
            // Guarded on ENABLE_INPUT_SYSTEM, not merely on the package being installed. A project
            // can have the package present while active input handling is still set to the legacy
            // manager, and in that case the Input System module receives nothing.
            if (TryAttachInputSystemModule(eventSystemHost))
            {
                return;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            if (eventSystemHost.GetComponent<StandaloneInputModule>() == null)
            {
                eventSystemHost.AddComponent<StandaloneInputModule>();
            }
#else
            // Legacy input is off and the Input System assembly could not be found. Adding
            // StandaloneInputModule here would throw the moment it ran, so adding nothing is the
            // lesser evil - but the panel would then be silently unclickable, which is worse than
            // a loud message.
            DevKitLog.Error(
                "The panel has no UI input module and will not respond to clicks. This scene has " +
                "no EventSystem of its own, active input handling is set to Input System Package " +
                "(New), and UnityEngine.InputSystem could not be resolved. Add an EventSystem to " +
                "the scene, or enable the legacy input manager in Player Settings.");
#endif
        }

#if ENABLE_INPUT_SYSTEM
        static bool TryAttachInputSystemModule(GameObject host)
        {
            Type moduleType = Type.GetType(InputSystemUIModule, false);
            if (moduleType == null)
            {
                return false;
            }

            if (host.GetComponent(moduleType) == null)
            {
                host.AddComponent(moduleType);
            }

            return true;
        }
#endif
    }
}
#endif
