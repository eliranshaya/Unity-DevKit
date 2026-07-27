namespace DevKit
{
    /// <summary>
    /// Keys DevKit can bind its toggle to. Deliberately small: this is a backend neutral subset
    /// that maps cleanly onto both <c>KeyCode</c> and the Input System's <c>Key</c>.
    /// </summary>
    /// <remarks>
    /// Values are explicit because this enum is serialized on <see cref="DevKitBootstrap"/>.
    /// Never renumber an existing entry, only append.
    /// </remarks>
    public enum DevKey
    {
        None = 0,

        F1 = 1,
        F2 = 2,
        F3 = 3,
        F4 = 4,
        F5 = 5,
        F6 = 6,
        F7 = 7,
        F8 = 8,
        F9 = 9,
        F10 = 10,
        F11 = 11,
        F12 = 12,

        BackQuote = 20,
        Tab = 21,
        Escape = 22,
        Insert = 23,
        Home = 24,
        End = 25,
        Pause = 26,
    }
}
