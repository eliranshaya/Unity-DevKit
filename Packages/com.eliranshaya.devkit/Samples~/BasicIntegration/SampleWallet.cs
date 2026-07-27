using DevKit;
using UnityEngine;

namespace DevKitSamples
{
    /// <summary>
    /// A toy wallet showing the two registration styles side by side: attribute based for fixed
    /// actions, and a parameter for the ones you want to type a number into.
    /// </summary>
    public class SampleWallet : MonoBehaviour
    {
        [SerializeField] long _coins = 100;

        public static SampleWallet Instance { get; private set; }

        public long Coins { get { return _coins; } }

        void Awake()
        {
            Instance = this;
        }

        public void Add(long amount)
        {
            _coins = System.Math.Max(0, _coins + amount);
            Debug.Log("[Sample] Coins: " + _coins);
        }

        // A static action. Nothing to resolve, so this is the cheapest kind.
        [DevAction("Sample/Wallet/Add 1000")]
        static void AddThousand()
        {
            if (Instance != null)
            {
                Instance.Add(1000);
            }
        }

        // The int parameter turns into an input field next to the Run button.
        [DevAction("Sample/Wallet/Add Custom")]
        static void AddCustom(int amount)
        {
            if (Instance != null)
            {
                Instance.Add(amount);
            }
        }

        // An instance method. DevKit resolves the first live SampleWallet at invoke time.
        // confirm: true puts a yes/no prompt in front of it.
        [DevAction("Sample/Wallet/Reset", confirm: true)]
        void ResetWallet()
        {
            _coins = 0;
            Debug.Log("[Sample] Wallet reset.");
        }
    }
}
