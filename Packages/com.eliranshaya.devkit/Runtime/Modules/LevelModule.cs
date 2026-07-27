#if DEVKIT_ENABLED
using System;
using UnityEngine.SceneManagement;

namespace DevKit.Internal
{
    /// <summary>
    /// Win / lose / navigate levels through the game's <see cref="IDevKitGameAdapter"/>.
    /// Registers nothing at all when no adapter exists.
    /// </summary>
    /// <remarks>
    /// DevKit has no idea what a "level" is in a given game, and the adapter deliberately has no
    /// "current level" getter. Next and Previous therefore step a counter that starts at the
    /// active scene's build index and is updated by Go To Level - good enough for a cheat panel,
    /// and honest about what it knows.
    /// </remarks>
    internal static class LevelModule
    {
        static int _current = -1;

        internal static void Install()
        {
            _current = SceneManager.GetActiveScene().buildIndex;

            DevActions.Register("Level/Win", Win);
            DevActions.Register("Level/Lose", Lose);
            DevActions.Register("Level/Restart", Restart, true);
            DevActions.Register<int>("Level/Go To Level", GoTo);
            DevActions.Register("Level/Next", Next);
            DevActions.Register("Level/Previous", Previous);
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

        static void Win()
        {
            Adapter().WinLevel();
        }

        static void Lose()
        {
            Adapter().LoseLevel();
        }

        static void Restart()
        {
            IDevKitGameAdapter adapter = Adapter();
            if (_current >= 0)
            {
                adapter.LoadLevel(_current);
            }
            else
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
        }

        static void GoTo(int index)
        {
            _current = index;
            Adapter().LoadLevel(index);
        }

        static void Next()
        {
            GoTo(_current + 1);
        }

        static void Previous()
        {
            GoTo(_current > 0 ? _current - 1 : 0);
        }
    }
}
#endif
