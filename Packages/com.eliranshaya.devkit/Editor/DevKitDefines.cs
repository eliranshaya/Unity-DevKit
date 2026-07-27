using System.Collections.Generic;
using UnityEditor;

#if UNITY_2021_2_OR_NEWER
using UnityEditor.Build;
#endif

namespace DevKit.EditorTools
{
    /// <summary>
    /// Reads and writes the <c>DEVKIT_ENABLED</c> scripting define.
    /// <para>
    /// Always read the existing list, change only our own symbol, and write it back. Clobbering
    /// the whole define list is how packages break other packages.
    /// </para>
    /// </summary>
    internal static class DevKitDefines
    {
        internal const string Symbol = "DEVKIT_ENABLED";

        /// <summary>The groups the settings page offers, on top of whatever is currently selected.</summary>
        internal static readonly BuildTargetGroup[] CommonGroups =
        {
            BuildTargetGroup.Standalone,
            BuildTargetGroup.Android,
            BuildTargetGroup.iOS,
            BuildTargetGroup.WebGL,
        };

        internal static bool IsEnabled(BuildTargetGroup group)
        {
            List<string> symbols = Read(group);
            return symbols != null && symbols.Contains(Symbol);
        }

        internal static void SetEnabled(BuildTargetGroup group, bool enabled)
        {
            List<string> symbols = Read(group);
            if (symbols == null)
            {
                return;
            }

            bool present = symbols.Contains(Symbol);
            if (present == enabled)
            {
                return;
            }

            if (enabled)
            {
                symbols.Add(Symbol);
            }
            else
            {
                symbols.Remove(Symbol);
            }

            Write(group, symbols);
        }

        internal static bool IsSupported(BuildTargetGroup group)
        {
            return group != BuildTargetGroup.Unknown;
        }

        static List<string> Read(BuildTargetGroup group)
        {
            if (!IsSupported(group))
            {
                return null;
            }

            string raw;
#if UNITY_2021_2_OR_NEWER
            raw = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(group));
#else
            raw = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
#endif
            List<string> symbols = new List<string>();
            if (string.IsNullOrEmpty(raw))
            {
                return symbols;
            }

            string[] parts = raw.Split(';');
            for (int i = 0; i < parts.Length; i++)
            {
                string trimmed = parts[i].Trim();
                if (trimmed.Length > 0 && !symbols.Contains(trimmed))
                {
                    symbols.Add(trimmed);
                }
            }
            return symbols;
        }

        static void Write(BuildTargetGroup group, List<string> symbols)
        {
            string joined = string.Join(";", symbols.ToArray());
#if UNITY_2021_2_OR_NEWER
            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(group), joined);
#else
            PlayerSettings.SetScriptingDefineSymbolsForGroup(group, joined);
#endif
        }
    }
}
