namespace DevKit
{
    /// <summary>
    /// The seam between DevKit and a game. The built-in Level and Economy modules cannot know
    /// your classes, so they talk through this interface instead.
    /// <para>
    /// Implement it on any <see cref="UnityEngine.MonoBehaviour"/> in the scene - DevKit finds it
    /// on the first panel open - or hand an instance over explicitly with
    /// <see cref="DevActions.SetAdapter"/>.
    /// </para>
    /// <para>
    /// If no adapter is found, the Level and Economy modules register nothing and the panel shows
    /// a one line hint instead. Nothing throws, nothing is logged repeatedly.
    /// </para>
    /// </summary>
    /// <example>
    /// <code>
    /// public class GameAdapter : MonoBehaviour, IDevKitGameAdapter
    /// {
    ///     void Awake() =&gt; DevActions.SetAdapter(this);
    ///
    ///     public void WinLevel()               =&gt; LevelManager.Win();
    ///     public void LoseLevel()              =&gt; LevelManager.Lose();
    ///     public void LoadLevel(int index)     =&gt; LevelManager.Load(index);
    ///     public void AddCurrency(long amount) =&gt; Wallet.Add(amount);
    ///     public long GetCurrency()            =&gt; Wallet.Coins;
    /// }
    /// </code>
    /// </example>
    public interface IDevKitGameAdapter
    {
        /// <summary>Force a win on the current level.</summary>
        void WinLevel();

        /// <summary>Force a loss on the current level.</summary>
        void LoseLevel();

        /// <summary>Load the level with the given index. Implementations decide what "index" means.</summary>
        void LoadLevel(int index);

        /// <summary>Add to the player's currency. Negative amounts remove.</summary>
        void AddCurrency(long amount);

        /// <summary>Current currency, used by the live watch row and by "Set Currency".</summary>
        long GetCurrency();
    }
}
