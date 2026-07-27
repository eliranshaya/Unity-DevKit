#if DEVKIT_ENABLED
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DevKit.Internal
{
    /// <summary>
    /// Every persistent GameObject DevKit creates is born here, so there is exactly one place that
    /// decides how they are flagged and exactly one place that tears them down.
    /// </summary>
    internal static class DevKitScene
    {
        /// <summary>
        /// Prefix on every GameObject DevKit creates. The editor's cleanup menu matches on it -
        /// keep the two in step.
        /// </summary>
        internal const string NamePrefix = "[DevKit] ";

        static readonly List<GameObject> Owned = new List<GameObject>(4);

        /// <summary>
        /// Creates a scene-persistent DevKit root.
        /// </summary>
        internal static GameObject NewRoot(string name)
        {
            GameObject root = new GameObject(NamePrefix + name);

            // Deliberately NOT HideFlags.DontSave.
            //
            // DontSave means two things, not one: "never serialise me" AND "survive a scene
            // unload". Leaving Play Mode in the Editor is a scene unload, so a DontSave object
            // outlives the play session and turns up in the Edit Mode hierarchy - once per run,
            // accumulating. A GameObject created at runtime is never written to a scene asset in
            // the first place, so the serialisation half buys nothing and the survival half is a
            // pure liability.
            root.hideFlags = HideFlags.None;

            Object.DontDestroyOnLoad(root);

            Owned.Add(root);

            // Idempotent: -= on a handler that is not subscribed is a no-op, and this guards
            // against a stale subscription when Enter Play Mode runs without a domain reload.
            Application.quitting -= DestroyAll;
            Application.quitting += DestroyAll;

            return root;
        }

        /// <summary>
        /// Runs on quit, and in the Editor when Play Mode ends. With <see cref="HideFlags.None"/>
        /// the engine would collect these anyway; doing it explicitly means a leftover can never
        /// accumulate across sessions even if something re-flags an object later.
        /// </summary>
        internal static void DestroyAll()
        {
            for (int i = 0; i < Owned.Count; i++)
            {
                if (Owned[i] != null)
                {
                    Object.Destroy(Owned[i]);
                }
            }

            Owned.Clear();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnEnterPlayMode()
        {
            // Statics survive Enter Play Mode when domain reload is disabled. Anything still in
            // this list belongs to the previous session and is already destroyed.
            Owned.Clear();
            Application.quitting -= DestroyAll;
        }
    }
}
#endif
