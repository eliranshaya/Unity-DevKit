#if DEVKIT_ENABLED
using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DevKit.Internal
{
    /// <summary>
    /// One place for every API that changed shape between 2021.3 and Unity 6. Keeping the
    /// <c>#if</c> soup here means the rest of the package reads like ordinary code.
    /// </summary>
    internal static class DevKitCompat
    {
        /// <summary>First live instance of <typeparamref name="T"/>, inactive objects included.</summary>
        internal static T FindFirst<T>() where T : Object
        {
#if UNITY_2023_1_OR_NEWER
            return Object.FindAnyObjectByType<T>(FindObjectsInactive.Include);
#else
            return Object.FindObjectOfType<T>(true);
#endif
        }

        /// <summary>Every live instance of <typeparamref name="T"/>, inactive objects included.</summary>
        internal static T[] FindAll<T>() where T : Object
        {
#if UNITY_2023_1_OR_NEWER
            return Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
            return Object.FindObjectsOfType<T>(true);
#endif
        }

        /// <summary>
        /// Runtime typed variant, used to resolve the target of an instance <c>[DevAction]</c>
        /// method. Returns null when nothing matching is loaded.
        /// </summary>
        internal static Object FindFirst(Type type)
        {
#if UNITY_2023_1_OR_NEWER
            Object[] found = Object.FindObjectsByType(type, FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
            Object[] found = Object.FindObjectsOfType(type, true);
#endif
            return found != null && found.Length > 0 ? found[0] : null;
        }

        /// <summary>
        /// The engine's built-in font. 2022.1 renamed it, and a null here would leave the panel
        /// full of empty boxes, so fall back twice before giving up.
        /// </summary>
        internal static Font LoadBuiltinFont()
        {
            Font font = null;

#if UNITY_2022_1_OR_NEWER
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#else
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif
            if (font == null)
            {
                font = Font.CreateDynamicFontFromOSFont("Arial", DevPanelTheme.FontSizeBody);
            }
            if (font == null)
            {
                DevKitLog.Warning("No usable font found. Panel labels will be blank.");
            }
            return font;
        }
    }
}
#endif
