#if DEVKIT_ENABLED
using System;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace DevKit.Internal
{
    /// <summary>
    /// Frame rate, memory and device information. Everything here is derived on demand from values
    /// the engine already tracks, so no per-frame hook is installed just to feed these rows.
    /// </summary>
    internal static class DiagnosticsModule
    {
        static readonly StringBuilder Buffer = new StringBuilder(48);

        static int _lastFrame;
        static float _lastSampleTime;
        static float _fps;

        internal static void Install()
        {
            DevActions.RegisterWatch("Diagnostics/FPS", ReadFps);
            DevActions.RegisterWatch("Diagnostics/Mono Heap", ReadManagedMemory);
            DevActions.RegisterWatch("Diagnostics/Screen", ReadScreen);

            DevActionRegistry.AddInfo("Diagnostics/Device", DescribeDevice());
        }

        /// <summary>
        /// Frames elapsed over wall clock elapsed. Sampling between two watch polls averages over
        /// the interval, which is both steadier than a single delta time and free - no Update
        /// anywhere has to accumulate it.
        /// </summary>
        static string ReadFps()
        {
            float now = Time.realtimeSinceStartup;
            int frame = Time.frameCount;

            float elapsed = now - _lastSampleTime;
            int frames = frame - _lastFrame;

            if (elapsed > 0.05f && frames > 0)
            {
                _fps = frames / elapsed;
                _lastSampleTime = now;
                _lastFrame = frame;
            }

            Buffer.Length = 0;
            Buffer.Append(_fps.ToString("0.0", CultureInfo.InvariantCulture)).Append(" fps");
            return Buffer.ToString();
        }

        static string ReadManagedMemory()
        {
            return FormatBytes(GC.GetTotalMemory(false));
        }

        static string ReadScreen()
        {
            Buffer.Length = 0;
            Buffer.Append(Screen.width).Append(" x ").Append(Screen.height)
                  .Append(" @ ").Append(Mathf.RoundToInt((float)Screen.dpi)).Append(" dpi");
            return Buffer.ToString();
        }

        static string DescribeDevice()
        {
            return string.Format(
                "{0} / {1}\n{2}, {3} MB RAM, {4} cores\nGraphics: {5}",
                SystemInfo.deviceModel,
                SystemInfo.operatingSystem,
                SystemInfo.processorType,
                SystemInfo.systemMemorySize,
                SystemInfo.processorCount,
                SystemInfo.graphicsDeviceName);
        }

        static string FormatBytes(long bytes)
        {
            const float Mega = 1024f * 1024f;
            Buffer.Length = 0;
            Buffer.Append((bytes / Mega).ToString("0.0", CultureInfo.InvariantCulture)).Append(" MB");
            return Buffer.ToString();
        }

        [DevAction("Diagnostics/Log Device Info")]
        static void LogDeviceInfo()
        {
            DevKitLog.Info(DescribeDevice());
        }

        [DevAction("Diagnostics/Collect Garbage")]
        static void CollectGarbage()
        {
            long before = GC.GetTotalMemory(false);
            GC.Collect();
            long after = GC.GetTotalMemory(true);
            DevKitLog.Info(string.Format("GC freed {0}", FormatBytes(before - after)));
        }

        [DevAction("Diagnostics/Clear PlayerPrefs", confirm: true)]
        static void ClearPlayerPrefs()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            DevKitLog.Info("PlayerPrefs cleared.");
        }
    }
}
#endif
