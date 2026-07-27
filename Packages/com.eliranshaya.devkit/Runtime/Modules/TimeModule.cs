#if DEVKIT_ENABLED
using System.Globalization;
using UnityEngine;

namespace DevKit.Internal
{
    /// <summary>
    /// One field for the time scale, and a live readout of it.
    /// <para>
    /// Deliberately not a row of presets. A typed field covers 0, 0.5, 1, 5 and everything
    /// between with one row instead of six, and six rows of guesses at the values someone might
    /// want is exactly the kind of clutter that makes a debug panel slow to use.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This is the one module that changes engine state on purpose, so it owns that state openly
    /// rather than snapshotting and restoring behind the user's back. Note that the bootstrap's
    /// <c>pauseWhenOpen</c> restores the scale it captured when the panel opened, so a value set
    /// here is undone on close - turn that option off if you want the setting to stick.
    /// </remarks>
    internal static class TimeModule
    {
        internal static void Install()
        {
            DevActions.Register<float>("Time/Set Timescale", SetScale);
            DevActions.RegisterWatch("Time/= Current", ReadScale);
        }

        static void SetScale(float value)
        {
            // Unity rejects a negative time scale. Clamping beats throwing a toast at someone for
            // a stray minus sign.
            Time.timeScale = Mathf.Max(0f, value);
        }

        static string ReadScale()
        {
            return Time.timeScale.ToString("0.###", CultureInfo.InvariantCulture);
        }
    }
}
#endif
