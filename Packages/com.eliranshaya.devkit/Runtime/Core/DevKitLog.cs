#if DEVKIT_ENABLED
using System;
using System.Diagnostics;
using UnityEngine;

namespace DevKit.Internal
{
    /// <summary>
    /// Every DevKit log goes through here so the prefix is consistent and so the whole logging
    /// surface disappears with the define symbol.
    /// </summary>
    internal static class DevKitLog
    {
        const string Prefix = "[DevKit] ";

        [Conditional("DEVKIT_ENABLED")]
        internal static void Info(string message)
        {
            UnityEngine.Debug.Log(Prefix + message);
        }

        [Conditional("DEVKIT_ENABLED")]
        internal static void Warning(string message)
        {
            UnityEngine.Debug.LogWarning(Prefix + message);
        }

        [Conditional("DEVKIT_ENABLED")]
        internal static void Error(string message)
        {
            UnityEngine.Debug.LogError(Prefix + message);
        }

        /// <summary>
        /// Logs a failure with its context and the full stack. Never swallow an exception without
        /// naming the action path that produced it.
        /// </summary>
        [Conditional("DEVKIT_ENABLED")]
        internal static void Exception(string context, Exception exception)
        {
            UnityEngine.Debug.LogError(Prefix + context);
            if (exception != null)
            {
                UnityEngine.Debug.LogException(exception);
            }
        }
    }
}
#endif
