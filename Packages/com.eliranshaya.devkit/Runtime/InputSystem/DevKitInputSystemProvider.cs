#if DEVKIT_ENABLED
using DevKit.Internal;
using UnityEngine;
using UnityEngine.InputSystem.UI;

namespace DevKit.InputSystemSupport
{
    /// <summary>
    /// The new Input System half of <see cref="DevKitInput"/>: it supplies the UI input module,
    /// which is the only thing the panel needs from an input backend.
    /// <para>
    /// This lives in its own assembly whose <c>defineConstraints</c> are only satisfied when
    /// <c>com.unity.inputsystem</c> is installed. A project without the package never compiles
    /// this file, which is why the core assembly can stay free of any Input System reference and
    /// still build everywhere.
    /// </para>
    /// </summary>
    internal sealed class DevKitInputSystemProvider : IDevKitInputProvider
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Install()
        {
            DevKitInput.SetProvider(new DevKitInputSystemProvider());
        }

        public bool TryAttachUIModule(GameObject eventSystemHost)
        {
            if (eventSystemHost.GetComponent<InputSystemUIInputModule>() == null)
            {
                eventSystemHost.AddComponent<InputSystemUIInputModule>();
            }

            return true;
        }
    }
}
#endif
