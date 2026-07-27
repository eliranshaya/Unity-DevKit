using DevKit;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DevKitSamples
{
    /// <summary>
    /// Wires the built-in Level and Economy modules to this sample's systems. Without an adapter
    /// those two modules register nothing and the panel shows a hint instead.
    /// </summary>
    public class SampleGameAdapter : MonoBehaviour, IDevKitGameAdapter
    {
        void Awake()
        {
            // Optional. DevKit finds the adapter on its own when the panel first opens; this just
            // skips the search and works even if the component is on an inactive object.
            DevActions.SetAdapter(this);
        }

        void Start()
        {
            // Manual registration is for things an attribute cannot express: closures, and paths
            // decided at runtime.
            DevActions.Register("Sample/Level/Skip Cutscene", () => Debug.Log("[Sample] Cutscene skipped."));
            DevActions.RegisterWatch("Sample/Level/Scene", () => SceneManager.GetActiveScene().name);
        }

        public void WinLevel()
        {
            Debug.Log("[Sample] Level won.");
        }

        public void LoseLevel()
        {
            Debug.Log("[Sample] Level lost.");
        }

        public void LoadLevel(int index)
        {
            Debug.Log("[Sample] Loading level " + index + ".");
        }

        public void AddCurrency(long amount)
        {
            if (SampleWallet.Instance != null)
            {
                SampleWallet.Instance.Add(amount);
            }
        }

        public long GetCurrency()
        {
            return SampleWallet.Instance != null ? SampleWallet.Instance.Coins : 0L;
        }
    }
}
