#if DEVKIT_ENABLED
using DevKit.Internal;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.UI;

namespace DevKit.InputSystemSupport
{
    /// <summary>
    /// The new Input System half of <see cref="DevKitInput"/>.
    /// <para>
    /// This lives in its own assembly whose <c>defineConstraints</c> are only satisfied when
    /// <c>com.unity.inputsystem</c> is installed. A project without the package simply never
    /// compiles this file, which is why the core assembly can stay free of any Input System
    /// reference and still build everywhere.
    /// </para>
    /// </summary>
    internal sealed class DevKitInputSystemProvider : IDevKitInputProvider
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Install()
        {
            DevKitInput.SetProvider(new DevKitInputSystemProvider());
        }

        public bool GetKeyDown(DevKey key)
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return false;
            }

            Key mapped = ToKey(key);
            if (mapped == Key.None)
            {
                return false;
            }

            KeyControl control = keyboard[mapped];
            return control != null && control.wasPressedThisFrame;
        }

        public int TouchCount
        {
            get
            {
                Touchscreen screen = Touchscreen.current;
                if (screen == null)
                {
                    return 0;
                }

                int count = 0;
                var touches = screen.touches;
                for (int i = 0; i < touches.Count; i++)
                {
                    if (touches[i].press.isPressed)
                    {
                        count++;
                    }
                }
                return count;
            }
        }

        public bool TryAttachUIModule(GameObject eventSystemHost)
        {
            if (eventSystemHost.GetComponent<InputSystemUIInputModule>() == null)
            {
                eventSystemHost.AddComponent<InputSystemUIInputModule>();
            }
            return true;
        }

        static Key ToKey(DevKey key)
        {
            switch (key)
            {
                case DevKey.F1: return Key.F1;
                case DevKey.F2: return Key.F2;
                case DevKey.F3: return Key.F3;
                case DevKey.F4: return Key.F4;
                case DevKey.F5: return Key.F5;
                case DevKey.F6: return Key.F6;
                case DevKey.F7: return Key.F7;
                case DevKey.F8: return Key.F8;
                case DevKey.F9: return Key.F9;
                case DevKey.F10: return Key.F10;
                case DevKey.F11: return Key.F11;
                case DevKey.F12: return Key.F12;
                case DevKey.BackQuote: return Key.Backquote;
                case DevKey.Tab: return Key.Tab;
                case DevKey.Escape: return Key.Escape;
                case DevKey.Insert: return Key.Insert;
                case DevKey.Home: return Key.Home;
                case DevKey.End: return Key.End;
                case DevKey.Pause: return Key.Pause;
                default: return Key.None;
            }
        }
    }
}
#endif
