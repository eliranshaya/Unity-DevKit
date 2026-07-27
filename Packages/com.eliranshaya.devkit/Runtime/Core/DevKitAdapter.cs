#if DEVKIT_ENABLED
using UnityEngine;

namespace DevKit.Internal
{
    /// <summary>
    /// Holds the single <see cref="IDevKitGameAdapter"/> the built-in modules talk to.
    /// Resolution order: whatever was handed to <see cref="DevActions.SetAdapter"/>, otherwise the
    /// first implementor found in the loaded scenes.
    /// </summary>
    internal static class DevKitAdapter
    {
        static IDevKitGameAdapter _adapter;
        static bool _searched;

        internal static void Set(IDevKitGameAdapter adapter)
        {
            _adapter = adapter;
            _searched = adapter != null;

            // An adapter handed over after the panel was built still lights up the Level and
            // Economy modules; the panel notices the registry version bump and refreshes.
            if (adapter != null)
            {
                BuiltinModules.InstallAdapterModules();
            }
        }

        internal static void Reset()
        {
            _adapter = null;
            _searched = false;
        }

        /// <summary>
        /// Returns the adapter, searching the scene once on first call. A missing adapter is a
        /// normal state, not an error - callers register nothing instead of throwing.
        /// </summary>
        internal static IDevKitGameAdapter Get()
        {
            // A MonoBehaviour adapter can be destroyed between scenes. ReferenceEquals sees the
            // live C# reference, the == below is Unity's overload that reports the destroyed
            // object as null - the pair together is what detects it.
            MonoBehaviour asBehaviour = _adapter as MonoBehaviour;
            if (!ReferenceEquals(asBehaviour, null) && asBehaviour == null)
            {
                _adapter = null;
                _searched = false;
            }

            if (_adapter != null || _searched)
            {
                return _adapter;
            }

            _searched = true;

            MonoBehaviour[] all = DevKitCompat.FindAll<MonoBehaviour>();
            for (int i = 0; i < all.Length; i++)
            {
                IDevKitGameAdapter candidate = all[i] as IDevKitGameAdapter;
                if (candidate != null)
                {
                    _adapter = candidate;
                    break;
                }
            }

            return _adapter;
        }
    }
}
#endif
