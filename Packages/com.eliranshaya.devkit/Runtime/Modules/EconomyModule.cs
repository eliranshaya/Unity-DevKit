#if DEVKIT_ENABLED
using System;
using System.Globalization;

namespace DevKit.Internal
{
    /// <summary>
    /// Currency cheats through the game's <see cref="IDevKitGameAdapter"/>.
    /// Registers nothing at all when no adapter exists.
    /// </summary>
    internal static class EconomyModule
    {
        /// <summary>What "Max Out" grants. Not long.MaxValue - overflowing a game's wallet on
        /// purpose is a worse bug report than a large number is a cheat.</summary>
        const long MaxOutAmount = 999999999L;

        internal static void Install()
        {
            DevActions.RegisterWatch("Economy/Balance", ReadBalance);

            DevActions.Register("Economy/Add 100", Add100);
            DevActions.Register("Economy/Add 1000", Add1000);
            DevActions.Register<int>("Economy/Add Currency", Add);
            DevActions.Register<int>("Economy/Remove Currency", Remove);
            DevActions.Register<int>("Economy/Set Currency", Set);
            DevActions.Register("Economy/Max Out", MaxOut, true);
        }

        static IDevKitGameAdapter Adapter()
        {
            IDevKitGameAdapter adapter = DevKitAdapter.Get();
            if (adapter == null)
            {
                throw new InvalidOperationException("The game adapter is gone. Was its GameObject destroyed?");
            }
            return adapter;
        }

        static string ReadBalance()
        {
            IDevKitGameAdapter adapter = DevKitAdapter.Get();
            return adapter == null
                ? "-"
                : adapter.GetCurrency().ToString("N0", CultureInfo.InvariantCulture);
        }

        static void Add100()
        {
            Adapter().AddCurrency(100L);
        }

        static void Add1000()
        {
            Adapter().AddCurrency(1000L);
        }

        static void Add(int amount)
        {
            Adapter().AddCurrency(amount);
        }

        static void Remove(int amount)
        {
            Adapter().AddCurrency(-amount);
        }

        static void Set(int amount)
        {
            IDevKitGameAdapter adapter = Adapter();
            adapter.AddCurrency(amount - adapter.GetCurrency());
        }

        static void MaxOut()
        {
            IDevKitGameAdapter adapter = Adapter();
            adapter.AddCurrency(MaxOutAmount - adapter.GetCurrency());
        }
    }
}
#endif
