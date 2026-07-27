#if DEVKIT_ENABLED
namespace DevKit.Internal
{
    /// <summary>
    /// Installs the built-in modules after the assembly scan.
    /// <para>
    /// Time and Diagnostics declare their actions with <see cref="DevActionAttribute"/> and are
    /// picked up by the scan like any user code; this class only adds their live watches. Level
    /// and Economy cannot do that - they are meaningless without an
    /// <see cref="IDevKitGameAdapter"/> - so they register manually, and only once one exists.
    /// </para>
    /// </summary>
    internal static class BuiltinModules
    {
        internal const string AdapterHintPath = "Game/Adapter missing";

        const string AdapterHintText =
            "The Level and Economy modules need an IDevKitGameAdapter. Implement it on any " +
            "MonoBehaviour in the scene, or call DevActions.SetAdapter(this) from its Awake.";

        static bool _adapterModulesInstalled;

        internal static void Reset()
        {
            _adapterModulesInstalled = false;
        }

        internal static void Install()
        {
            TimeModule.Install();
            DiagnosticsModule.Install();
            InstallAdapterModules();
        }

        /// <summary>
        /// Idempotent. Called after the scan and again whenever an adapter is handed over through
        /// <see cref="DevActions.SetAdapter"/>, so an adapter that shows up late still lights the
        /// modules up.
        /// </summary>
        internal static void InstallAdapterModules()
        {
            if (_adapterModulesInstalled)
            {
                return;
            }

            if (DevKitAdapter.Get() == null)
            {
                // No adapter is a normal state. Say so once, in the panel, and move on.
                DevActionRegistry.AddInfo(AdapterHintPath, AdapterHintText);
                return;
            }

            _adapterModulesInstalled = true;
            DevActionRegistry.Remove(AdapterHintPath);

            LevelModule.Install();
            EconomyModule.Install();
        }
    }
}
#endif
