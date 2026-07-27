using System;
using System.Collections.Generic;
using System.IO;
using DevKit;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Example DevKit wiring. Put this on the same GameObject as <c>DevKitBootstrap</c>, press Play,
/// then open the panel by calling <c>DevKitBootstrap.Open()</c> - from a UI Button, or by ticking
/// "Open On Start" on the bootstrap.
/// <para>
/// It carries a little fake game state - health, coins, speed, god mode, difficulty - purely so the
/// panel has something to move. Every action below is a one-liner you would replace with a call
/// into your own systems.
/// </para>
/// <para>
/// It also implements <see cref="IDevKitGameAdapter"/>, which is what turns the built-in Level and
/// Economy categories on.
/// </para>
/// </summary>
public class DevKitExampleActions : MonoBehaviour, IDevKitGameAdapter
{
    /// <summary>Any enum works as an action parameter. The panel gives it a button that cycles.</summary>
    public enum Difficulty
    {
        Easy,
        Normal,
        Hard,
        Nightmare,
    }

    [Header("Fake game state, so the panel has something to change")]
    [SerializeField] int _health = 100;
    [SerializeField] long _coins = 250;
    [SerializeField] float _moveSpeed = 5f;
    [SerializeField] bool _godMode;
    [SerializeField] Difficulty _difficulty = Difficulty.Normal;

    readonly List<GameObject> _spawned = new List<GameObject>();

    // A real game exposes its state to the rest of the game. These also keep the fields above
    // "used" when DEVKIT_ENABLED is off and every Register call below is erased by the compiler -
    // otherwise you would ship a handful of CS0414 warnings.
    public int Health { get { return _health; } }
    public long Coins { get { return _coins; } }
    public float MoveSpeed { get { return _moveSpeed; } }
    public bool IsGodMode { get { return _godMode; } }
    public Difficulty CurrentDifficulty { get { return _difficulty; } }

    void Awake()
    {
        // Hands the built-in Level and Economy modules something to talk to. Without this they
        // register nothing and the panel shows a hint under "Game" instead.
        DevActions.SetAdapter(this);

        RegisterPlayerActions();
        RegisterWorldActions();
        RegisterDeviceActions();
        RegisterStatWatches();
    }

    // ------------------------------------------------------------------ player

    void RegisterPlayerActions()
    {
        // No parameters: the whole row becomes one big button.
        DevActions.Register("Player/Heal Full", () => _health = 100);

        // confirm: true puts a yes/no prompt in front of it. Use it for anything destructive.
        DevActions.Register("Player/Kill", () => _health = 0, confirm: true);

        // int parameter: a number field appears, with a Run button next to it.
        DevActions.Register<int>("Player/Damage", amount => _health = Mathf.Max(0, _health - amount));

        // float parameter.
        DevActions.Register<float>("Player/Set Move Speed", speed => _moveSpeed = Mathf.Max(0f, speed));

        // bool parameter: a button that flips ON/OFF, then Run applies it.
        DevActions.Register<bool>("Player/God Mode", on => _godMode = on);

        // enum parameter: a button that cycles through your enum's values.
        DevActions.Register<Difficulty>("Player/Set Difficulty", value => _difficulty = value);

        // A watch sits in the same category as the actions that change it, so you can see the
        // number move as you tap. Polled 4x a second, only while this category is on screen.
        DevActions.RegisterWatch("Player/= Health", () => _health.ToString());
        DevActions.RegisterWatch("Player/= Speed", () => _moveSpeed.ToString("0.0"));
        DevActions.RegisterWatch("Player/= God Mode", () => _godMode ? "ON" : "OFF");
        DevActions.RegisterWatch("Player/= Difficulty", () => _difficulty.ToString());
    }

    // ------------------------------------------------------------------ world

    void RegisterWorldActions()
    {
        DevActions.Register("World/Reload Scene", ReloadScene, confirm: true);

        // PrimitiveType is one of Unity's own enums - nothing special is needed for it.
        DevActions.Register<PrimitiveType>("World/Spawn Primitive", SpawnPrimitive);

        DevActions.Register("World/Spawn 10 Cubes", () =>
        {
            for (int i = 0; i < 10; i++)
            {
                SpawnPrimitive(PrimitiveType.Cube);
            }
        });

        DevActions.Register("World/Clear Spawned", ClearSpawned, confirm: true);
    }

    // ------------------------------------------------------------------ device

    void RegisterDeviceActions()
    {
        DevActions.Register("Device/Screenshot", TakeScreenshot);

        DevActions.Register<int>("Device/Target FPS", fps => Application.targetFrameRate = fps);

        // Always give yourself a way back out of a setting you changed. -1 means "platform default".
        DevActions.Register("Device/Restore Target FPS", () => Application.targetFrameRate = -1);

        DevActions.Register<bool>("Device/VSync", on => QualitySettings.vSyncCount = on ? 1 : 0);
        DevActions.Register<int>("Device/Quality Level", SetQualityLevel);

        // string parameter.
        DevActions.Register<string>("Device/Delete PlayerPref", DeletePlayerPref);

        // What a failing action looks like: red toast in the panel, full stack in the console,
        // and the panel stays open. Nothing you register can take the panel down.
        DevActions.Register("Device/Throw Test Error",
            () => throw new InvalidOperationException("This is what a failed action looks like."));
    }

    // ------------------------------------------------------------------ watches

    void RegisterStatWatches()
    {
        DevActions.RegisterWatch("Stats/Scene", () => SceneManager.GetActiveScene().name);
        DevActions.RegisterWatch("Stats/Uptime", () => Time.realtimeSinceStartup.ToString("0") + " s");
        DevActions.RegisterWatch("Stats/Spawned", () => CountSpawned().ToString());
        DevActions.RegisterWatch("Stats/Quality", () => QualitySettings.names[QualitySettings.GetQualityLevel()]);
        DevActions.RegisterWatch("Stats/VSync", () => QualitySettings.vSyncCount > 0 ? "ON" : "OFF");
        DevActions.RegisterWatch("Stats/Target FPS", () =>
            Application.targetFrameRate < 0 ? "platform default" : Application.targetFrameRate.ToString());
    }

    // ------------------------------------------------------------------ the other style
    //
    // Everything above ran through DevActions.Register in Awake. The [DevAction] attribute does the
    // same job with no Awake code at all - DevKit finds these by reflection the first time the
    // panel opens. Use Register when you need a closure or a runtime-decided path; use the
    // attribute for everything else.

    /// <summary>Static: invoked directly, there is no instance to resolve.</summary>
    [DevAction("Cheats/Unlock Everything")]
    static void UnlockEverything()
    {
        PlayerPrefs.SetInt("example_unlocked_all", 1);
        PlayerPrefs.Save();
        Debug.Log("[Example] Everything unlocked.");
    }

    /// <summary>Instance: DevKit resolves the first live instance when you tap the row.</summary>
    [DevAction("Cheats/Give 1000 Coins")]
    void GiveThousandCoins()
    {
        _coins += 1000;
    }

    /// <summary>Parameters and confirm work on attributes exactly as they do on Register.</summary>
    [DevAction("Cheats/Set Coins", confirm: true)]
    void SetCoins(int amount)
    {
        _coins = Math.Max(0, amount);
    }

    /// <summary>order sorts a row inside its category. Lower comes first; the default is 0.</summary>
    [DevAction("Cheats/Wipe Save", confirm: true, order: -10)]
    static void WipeSave()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("[Example] Save wiped.");
    }

    // ------------------------------------------------------------------ adapter
    //
    // Implementing this is what makes the built-in Level and Economy categories appear.

    public void WinLevel()
    {
        Debug.Log("[Example] Level won. Call your own LevelManager here.");
    }

    public void LoseLevel()
    {
        Debug.Log("[Example] Level lost. Call your own LevelManager here.");
    }

    public void LoadLevel(int index)
    {
        if (index < 0 || index >= SceneManager.sceneCountInBuildSettings)
        {
            // Throwing is the right move: the panel turns it into a red toast naming the problem
            // instead of failing silently.
            throw new ArgumentOutOfRangeException(
                "index", "No scene at build index " + index + ". Check File > Build Settings.");
        }

        SceneManager.LoadScene(index);
    }

    public void AddCurrency(long amount)
    {
        _coins = Math.Max(0L, _coins + amount);
    }

    public long GetCurrency()
    {
        return _coins;
    }

    // ------------------------------------------------------------------ plumbing

    void SpawnPrimitive(PrimitiveType type)
    {
        GameObject spawned = GameObject.CreatePrimitive(type);
        spawned.name = "DevKit Spawn (" + type + ")";
        spawned.transform.position = UnityEngine.Random.insideUnitSphere * 3f + Vector3.up * 2f;
        _spawned.Add(spawned);
    }

    void ClearSpawned()
    {
        for (int i = 0; i < _spawned.Count; i++)
        {
            if (_spawned[i] != null)
            {
                Destroy(_spawned[i]);
            }
        }

        _spawned.Clear();
    }

    int CountSpawned()
    {
        int alive = 0;
        for (int i = 0; i < _spawned.Count; i++)
        {
            if (_spawned[i] != null)
            {
                alive++;
            }
        }

        return alive;
    }

    void ReloadScene()
    {
        // Spawned objects belong to the scene and die with it.
        _spawned.Clear();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void TakeScreenshot()
    {
        string file = "devkit-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".png";
        string path = Path.Combine(Application.persistentDataPath, file);

        ScreenCapture.CaptureScreenshot(path);
        Debug.Log("[Example] Screenshot queued: " + path);
    }

    void SetQualityLevel(int level)
    {
        string[] names = QualitySettings.names;
        if (names.Length == 0)
        {
            throw new InvalidOperationException("This project has no quality levels defined.");
        }

        QualitySettings.SetQualityLevel(Mathf.Clamp(level, 0, names.Length - 1), true);
    }

    void DeletePlayerPref(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            throw new ArgumentException("Type a key name into the field first.");
        }

        if (!PlayerPrefs.HasKey(key))
        {
            throw new ArgumentException("No PlayerPrefs key named '" + key + "'.");
        }

        PlayerPrefs.DeleteKey(key);
        PlayerPrefs.Save();
        Debug.Log("[Example] Deleted PlayerPrefs key '" + key + "'.");
    }
}
