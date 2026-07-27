#if DEVKIT_ENABLED
using System.Collections;
using System.Globalization;
using UnityEngine;

namespace DevKit.Internal
{
    /// <summary>
    /// Time scale controls. This is the one module that changes engine state on purpose, so it
    /// owns that state openly rather than snapshotting and restoring behind the user's back.
    /// </summary>
    /// <remarks>
    /// If the bootstrap's <c>pauseWhenOpen</c> is on, the panel restores the time scale it
    /// captured when it opened - so a scale set from here is undone on close. Turn
    /// <c>pauseWhenOpen</c> off if you want the setting to stick.
    /// </remarks>
    internal static class TimeModule
    {
        internal static void Install()
        {
            DevActions.RegisterWatch("Time/Current Scale", ReadScale);
        }

        static string ReadScale()
        {
            return Time.timeScale.ToString("0.###", CultureInfo.InvariantCulture);
        }

        [DevAction("Time/Pause", order: -20)]
        static void Pause()
        {
            Time.timeScale = 0f;
        }

        [DevAction("Time/Resume", order: -19)]
        static void Resume()
        {
            Time.timeScale = 1f;
        }

        [DevAction("Time/Timescale 0.1", order: -10)]
        static void Scale01()
        {
            Time.timeScale = 0.1f;
        }

        [DevAction("Time/Timescale 0.5", order: -9)]
        static void Scale05()
        {
            Time.timeScale = 0.5f;
        }

        [DevAction("Time/Timescale 1", order: -8)]
        static void Scale1()
        {
            Time.timeScale = 1f;
        }

        [DevAction("Time/Timescale 2", order: -7)]
        static void Scale2()
        {
            Time.timeScale = 2f;
        }

        [DevAction("Time/Timescale 5", order: -6)]
        static void Scale5()
        {
            Time.timeScale = 5f;
        }

        [DevAction("Time/Set Timescale", order: -5)]
        static void SetScale(float value)
        {
            Time.timeScale = Mathf.Max(0f, value);
        }

        /// <summary>Advances exactly one frame, then pauses again.</summary>
        [DevAction("Time/Step One Frame")]
        static void StepOneFrame()
        {
            DevKitRunner.Run(StepRoutine());
        }

        static IEnumerator StepRoutine()
        {
            Time.timeScale = 1f;
            // WaitForEndOfFrame still ticks at timeScale 0, which is what makes stepping possible.
            yield return new WaitForEndOfFrame();
            Time.timeScale = 0f;
        }
    }
}
#endif
