#if DEVKIT_ENABLED
namespace DevKit.Internal
{
    /// <summary>
    /// Installs the built-in modules after the assembly scan.
    /// <para>
    /// There are deliberately only two, and neither knows anything about your game. DevKit ships
    /// no level or economy cheats: what a "level" or a "currency" is differs in every project, and
    /// a generic guess at them is worth less than the two lines of
    /// <see cref="DevActions.Register{T}"/> you would write yourself. Time and Diagnostics survive
    /// because they talk only to the engine, which is the same everywhere.
    /// </para>
    /// </summary>
    internal static class BuiltinModules
    {
        internal static void Install()
        {
            TimeModule.Install();
            DiagnosticsModule.Install();
        }
    }
}
#endif
