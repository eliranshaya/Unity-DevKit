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
/// </summary>
public class DevKitExampleActions : MonoBehaviour
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

    /// <summary>The rows of the example table: a few enemy types, each with its own tunable stats.</summary>
    readonly Enemy[] _enemies =
    {
        new Enemy("Slime", 1.5f, 5, false, Difficulty.Easy),
        new Enemy("Goblin", 3.2f, 12, false, Difficulty.Normal),
        new Enemy("Skeleton Archer", 2.4f, 18, false, Difficulty.Normal),
        new Enemy("Ogre", 1.1f, 40, true, Difficulty.Hard),
        new Enemy("Wraith", 4.5f, 25, true, Difficulty.Nightmare),
    };

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
        RegisterPlayerActions();
        RegisterLevelActions();
        RegisterWorldActions();
        RegisterDeviceActions();
        RegisterStatWatches();
        RegisterTables();
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

    // ------------------------------------------------------------------ tables
    //
    // Many things with the same properties - every enemy type, here - are one table instead of a
    // Select row plus a Set row per property. Every cell is live: a typed number is written when you
    // commit it, a dropdown or a toggle on the tap. There is no Run button anywhere.

    void RegisterTables()
    {
        DevActions.RegisterTable("Enemies/Stats", new DevTable<Enemy>(_enemies)
            // Tapping a name is the row's main action. Highlighted rows are the ones you changed.
            .Label("Enemy", e => e.Name, onClick: e => Debug.Log("[Example] Spawned a " + e.Name + "."),
                highlight: e => e.IsTuned)
            .Float("Speed", e => e.Speed, (e, v) => e.Speed = Mathf.Max(0f, v))
            .Int("Damage", e => e.Damage, (e, v) => e.Damage = Mathf.Max(0, v))
            .Toggle("Elite", e => e.Elite, (e, v) => e.Elite = v)
            // An enum becomes a dropdown of its values.
            .Choice("Tier", e => e.Tier, (e, v) => e.Tier = v)
            .Button("", "Reset", e => e.Reset(), enabled: e => e.IsTuned)
            .Action("Reset All", ResetEnemies, confirm: true));
    }

    void ResetEnemies()
    {
        for (int i = 0; i < _enemies.Length; i++)
        {
            _enemies[i].Reset();
        }
    }

    /// <summary>One enemy type: the stats it ships with, and the ones the table is editing.</summary>
    sealed class Enemy
    {
        public readonly string Name;

        public float Speed;
        public int Damage;
        public bool Elite;
        public Difficulty Tier;

        readonly float _baseSpeed;
        readonly int _baseDamage;
        readonly bool _baseElite;
        readonly Difficulty _baseTier;

        public Enemy(string name, float speed, int damage, bool elite, Difficulty tier)
        {
            Name = name;
            _baseSpeed = speed;
            _baseDamage = damage;
            _baseElite = elite;
            _baseTier = tier;
            Reset();
        }

        public bool IsTuned
        {
            get { return !Mathf.Approximately(Speed, _baseSpeed) || Damage != _baseDamage || Elite != _baseElite || Tier != _baseTier; }
        }

        public void Reset()
        {
            Speed = _baseSpeed;
            Damage = _baseDamage;
            Elite = _baseElite;
            Tier = _baseTier;
        }
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
        AddCoins(1000);
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

    // ------------------------------------------------------------------ your own "level" cheats
    //
    // DevKit ships none of these. What a level is differs in every project, so you register the
    // handful you actually want - which is all the Level module ever did anyway.

    void RegisterLevelActions()
    {
        DevActions.Register("Level/Win", () => Debug.Log("[Example] Level won."));
        DevActions.Register("Level/Lose", () => Debug.Log("[Example] Level lost."));
        DevActions.Register<int>("Level/Go To Build Index", LoadLevel);
        DevActions.RegisterWatch("Level/= Build Index",
            () => SceneManager.GetActiveScene().buildIndex.ToString());
    }

    static void LoadLevel(int index)
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

    void AddCoins(long amount)
    {
        _coins = Math.Max(0L, _coins + amount);
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
