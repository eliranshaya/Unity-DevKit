using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DevKit.EditorTools
{
    /// <summary>
    /// <b>Project Settings &gt; DevKit</b>. One switch per build target group, because the whole
    /// package is gated behind a single define.
    /// </summary>
    internal static class DevKitSettingsProvider
    {
        internal const string SettingsPath = "Project/DevKit";

        const string Explanation =
            "DevKit compiles into your project only when DEVKIT_ENABLED is defined. With the " +
            "symbol off, every DevActions call site is erased by the compiler, the bootstrap " +
            "destroys itself in Awake, and the package contributes nothing to the build.\n\n" +
            "Turn it on for the targets you develop against. Leave it off for the target you ship.";

        [SettingsProvider]
        public static SettingsProvider Create()
        {
            SettingsProvider provider = new SettingsProvider(SettingsPath, SettingsScope.Project)
            {
                label = "DevKit",
                guiHandler = OnGUI,
                keywords = new HashSet<string>(new[]
                {
                    "devkit", "debug", "cheat", "developer", "panel", "DEVKIT_ENABLED"
                }),
            };
            return provider;
        }

        static void OnGUI(string searchContext)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Scripting Define", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(Explanation, MessageType.None);
            EditorGUILayout.Space();

            BuildTargetGroup active = EditorUserBuildSettings.selectedBuildTargetGroup;

            if (DevKitDefines.IsSupported(active) && !DevKitDefines.IsEnabled(active))
            {
                EditorGUILayout.HelpBox(
                    "DevKit is disabled for the active build target (" + active + "). " +
                    "The panel will not appear in Play Mode.",
                    MessageType.Warning);
                if (GUILayout.Button("Enable for " + active, GUILayout.Height(28f)))
                {
                    DevKitDefines.SetEnabled(active, true);
                }
                EditorGUILayout.Space();
            }

            List<BuildTargetGroup> groups = new List<BuildTargetGroup>(DevKitDefines.CommonGroups);
            if (DevKitDefines.IsSupported(active) && !groups.Contains(active))
            {
                groups.Insert(0, active);
            }

            for (int i = 0; i < groups.Count; i++)
            {
                BuildTargetGroup group = groups[i];
                bool enabled = DevKitDefines.IsEnabled(group);

                EditorGUI.BeginChangeCheck();
                string label = group == active ? group + "  (active)" : group.ToString();
                bool next = EditorGUILayout.ToggleLeft(label, enabled);
                if (EditorGUI.EndChangeCheck())
                {
                    DevKitDefines.SetEnabled(group, next);
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Scene Setup", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Add an empty GameObject with the DevKitBootstrap component to your first scene. " +
                "That is the entire setup - the panel builds itself the first time you press the hotkey.",
                MessageType.None);

            if (GUILayout.Button("Add DevKit Bootstrap to Open Scene", GUILayout.Height(28f)))
            {
                DevKitMenu.AddBootstrap();
            }
        }
    }
}
