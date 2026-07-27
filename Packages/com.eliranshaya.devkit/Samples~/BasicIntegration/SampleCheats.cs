using DevKit;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DevKitSamples
{
    /// <summary>
    /// Manual registration: for actions that need a closure, or a path decided at runtime.
    /// <para>
    /// This is where a project's own cheats live. DevKit ships none of its own beyond Time and
    /// Diagnostics - what "win the level" or "add currency" means is yours to define, and it is
    /// two lines each.
    /// </para>
    /// </summary>
    public class SampleCheats : MonoBehaviour
    {
        void Awake()
        {
            // No parameters: the whole row is one button.
            DevActions.Register("Sample/Level/Win", () => Debug.Log("[Sample] Level won."));
            DevActions.Register("Sample/Level/Lose", () => Debug.Log("[Sample] Level lost."));

            // int parameter: a number field appears next to a Run button, already parsed.
            DevActions.Register<int>("Sample/Level/Go To", index =>
                Debug.Log("[Sample] Loading level " + index + "."));

            // confirm: true puts a yes/no prompt in front of anything destructive.
            DevActions.Register("Sample/Level/Restart", Restart, confirm: true);

            // A read-only label, polled about 4x a second while its category is on screen.
            DevActions.RegisterWatch("Sample/Level/= Scene", () => SceneManager.GetActiveScene().name);
        }

        static void Restart()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
