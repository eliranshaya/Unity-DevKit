using System;
using System.Diagnostics;

#if DEVKIT_ENABLED
using DevKit.Internal;
#endif

namespace DevKit
{
    /// <summary>
    /// The public registration API. Everything here is
    /// <see cref="ConditionalAttribute">[Conditional("DEVKIT_ENABLED")]</see>, which means the C#
    /// compiler erases the whole call site - arguments included - when the symbol is undefined.
    /// <c>DevActions.Register("X", () =&gt; Expensive())</c> therefore costs literally nothing in a
    /// release build, and game code that calls it still compiles.
    /// </summary>
    public static class DevActions
    {
        /// <summary>Registers a parameterless action.</summary>
        /// <param name="path">Slash separated path, for example <c>"Level/Win"</c>.</param>
        /// <param name="action">Invoked when the row is tapped. Exceptions surface as a toast.</param>
        /// <param name="confirm">Ask for confirmation first. Use for destructive actions.</param>
        [Conditional("DEVKIT_ENABLED")]
        public static void Register(string path, Action action, bool confirm = false)
        {
#if DEVKIT_ENABLED
            DevActionRegistry.AddAction(path, action, confirm);
#endif
        }

        /// <summary>
        /// Registers an action taking a single argument. The panel renders an input field for it.
        /// </summary>
        /// <typeparam name="T">
        /// One of <c>int</c>, <c>float</c>, <c>string</c>, <c>bool</c> or an <c>enum</c>.
        /// Anything else is rejected with a warning and nothing is registered.
        /// </typeparam>
        [Conditional("DEVKIT_ENABLED")]
        public static void Register<T>(string path, Action<T> action, bool confirm = false)
        {
#if DEVKIT_ENABLED
            DevActionRegistry.AddAction(path, action, confirm);
#endif
        }

        /// <summary>
        /// Adds a read-only live-updating label. The getter is polled at roughly 4 Hz and only
        /// while its category is on screen, so it is safe to make it slightly expensive.
        /// </summary>
        [Conditional("DEVKIT_ENABLED")]
        public static void RegisterWatch(string path, Func<string> getter)
        {
#if DEVKIT_ENABLED
            DevActionRegistry.AddWatch(path, getter);
#endif
        }

        /// <summary>
        /// Removes a previously registered entry. Rarely needed - the registry is cleared before
        /// every play session.
        /// </summary>
        [Conditional("DEVKIT_ENABLED")]
        public static void Unregister(string path)
        {
#if DEVKIT_ENABLED
            DevActionRegistry.Remove(path);
#endif
        }

        /// <summary>
        /// Hands the built-in Level and Economy modules an adapter explicitly. Without this call
        /// DevKit looks for an <see cref="IDevKitGameAdapter"/> component in the loaded scenes the
        /// first time the panel opens.
        /// </summary>
        [Conditional("DEVKIT_ENABLED")]
        public static void SetAdapter(IDevKitGameAdapter adapter)
        {
#if DEVKIT_ENABLED
            DevKitAdapter.Set(adapter);
#endif
        }

        /// <summary>Opens the panel, building it on first use.</summary>
        [Conditional("DEVKIT_ENABLED")]
        public static void Open()
        {
#if DEVKIT_ENABLED
            DevKitBootstrap.RequestOpen();
#endif
        }

        /// <summary>Closes the panel if it is open.</summary>
        [Conditional("DEVKIT_ENABLED")]
        public static void Close()
        {
#if DEVKIT_ENABLED
            DevKitBootstrap.RequestClose();
#endif
        }

        /// <summary>Opens the panel if it is closed, closes it if it is open.</summary>
        [Conditional("DEVKIT_ENABLED")]
        public static void Toggle()
        {
#if DEVKIT_ENABLED
            DevKitBootstrap.RequestToggle();
#endif
        }
    }
}
