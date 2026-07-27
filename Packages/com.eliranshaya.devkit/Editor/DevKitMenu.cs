using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DevKit.EditorTools
{
    /// <summary>Menu entries for the one-off setup step.</summary>
    internal static class DevKitMenu
    {
        [MenuItem("GameObject/Dev/Add DevKit Bootstrap", false, 10)]
        internal static void AddBootstrap()
        {
            DevKitBootstrap existing = FindBootstrap();
            if (existing != null)
            {
                Selection.activeGameObject = existing.gameObject;
                EditorGUIUtility.PingObject(existing.gameObject);
                Debug.Log("[DevKit] This scene already has a DevKitBootstrap.", existing.gameObject);
                return;
            }

            GameObject host = new GameObject("DevKit");
            host.AddComponent<DevKitBootstrap>();

            Undo.RegisterCreatedObjectUndo(host, "Add DevKit Bootstrap");
            Selection.activeGameObject = host;
            EditorGUIUtility.PingObject(host);
            EditorSceneManager.MarkSceneDirty(host.scene);

            OfferToEnableDefine();
        }

        [MenuItem("Tools/DevKit/Project Settings", false, 100)]
        static void OpenSettings()
        {
            SettingsService.OpenProjectSettings(DevKitSettingsProvider.SettingsPath);
        }

        /// <summary>
        /// Deletes DevKit's runtime GameObjects from the open scenes.
        /// <para>
        /// Nothing should ever survive Play Mode now, but versions up to 1.0.0 flagged these
        /// objects <c>HideFlags.DontSave</c>, which also made them survive the scene unload that
        /// ends a play session. This clears anything left over from those runs.
        /// </para>
        /// </summary>
        [MenuItem("Tools/DevKit/Clean Up Leftover Objects", false, 101)]
        static void CleanUpLeftovers()
        {
            // Mirrors DevKitScene.NamePrefix, which is internal to the runtime assembly.
            const string prefix = "[DevKit] ";

            // FindObjectsOfTypeAll rather than FindObjectsByType: it is the only one that returns
            // objects carrying hide flags, which is exactly what we are hunting.
            GameObject[] all = Resources.FindObjectsOfTypeAll<GameObject>();
            int removed = 0;

            for (int i = 0; i < all.Length; i++)
            {
                GameObject candidate = all[i];
                if (candidate == null || !candidate.name.StartsWith(prefix, System.StringComparison.Ordinal))
                {
                    continue;
                }

                // Scene-bound roots only. Skips prefab assets and anything in a preview scene.
                if (!candidate.scene.IsValid() || candidate.transform.parent != null)
                {
                    continue;
                }

                // Clear the flags first or DestroyImmediate refuses to touch a NotEditable object.
                candidate.hideFlags = HideFlags.None;
                Object.DestroyImmediate(candidate);
                removed++;
            }

            Debug.Log(removed == 0
                ? "[DevKit] No leftover objects found."
                : "[DevKit] Removed " + removed + " leftover object(s).");
        }

        /// <summary>
        /// Adding the component is useless while the define is off, and silently editing a
        /// project's scripting defines is not something a package should do unasked - so ask.
        /// </summary>
        static void OfferToEnableDefine()
        {
            BuildTargetGroup active = EditorUserBuildSettings.selectedBuildTargetGroup;
            if (!DevKitDefines.IsSupported(active) || DevKitDefines.IsEnabled(active))
            {
                return;
            }

            bool enable = EditorUtility.DisplayDialog(
                "Enable DevKit?",
                "DEVKIT_ENABLED is not defined for " + active + ", so the panel will not appear.\n\n" +
                "Add the define now? You can change it later under Project Settings > DevKit.",
                "Add define",
                "Not now");

            if (enable)
            {
                DevKitDefines.SetEnabled(active, true);
            }
        }

        static DevKitBootstrap FindBootstrap()
        {
#if UNITY_2023_1_OR_NEWER
            return Object.FindAnyObjectByType<DevKitBootstrap>(FindObjectsInactive.Include);
#else
            return Object.FindObjectOfType<DevKitBootstrap>(true);
#endif
        }
    }
}
