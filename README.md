# DevKit

DevKit is a zero-setup developer and cheat panel for Unity. You drop **one empty GameObject** into a scene, call `Open()`, and a full debug panel builds itself at runtime — no prefabs, no sprites, no fonts, no scene wiring. Mark a method with `[DevAction("Economy/Add 1000$")]` and it shows up as a button; give the method an `int` parameter and it shows up as a button with an input field. The whole package is gated behind a single define symbol, so a release build contains none of it: call sites are erased by the compiler, and a bootstrap GameObject left in a shipped scene destroys itself in `Awake`.

DevKit binds no input of its own — you decide what opens it.

## Requirements

- Unity 2021.3 or newer
- uGUI (`com.unity.ugui`) — installed in every project by default
- Either input backend, or both at once

## Installation

Install via Unity's Package Manager using the Git URL.

1. Open your project in Unity.
2. Go to **Window → Package Manager**.
3. Click the **+** button in the top-left and choose **Add package from git URL…**
4. Paste the URL below and click **Add**:

```
https://github.com/eliranshaya/Unity-DevKit.git?path=/Packages/com.eliranshaya.devkit
```

To lock to a specific version, append the tag:

```
https://github.com/eliranshaya/Unity-DevKit.git?path=/Packages/com.eliranshaya.devkit#1.1.0
```

You can also add it manually by editing your project's `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.eliranshaya.devkit": "https://github.com/eliranshaya/Unity-DevKit.git?path=/Packages/com.eliranshaya.devkit#1.1.0"
  }
}
```

## Quick start

1. Open **Project Settings → DevKit** and enable the define for your active build target.
2. Use **GameObject → Dev → Add DevKit Bootstrap** to drop the component into your first scene.
3. Decide what opens it — see below — and enter Play Mode.

That is the whole setup. The panel opens with the built-in `Time` and `Diagnostics` categories already populated.

## Opening the panel

`Open`, `Close` and `Toggle` are public, parameterless methods on `DevKitBootstrap`, so they appear directly in a UI Button's **OnClick** dropdown — drag the DevKit GameObject into the slot and pick `DevKitBootstrap → Open`. No code required.

From your own code, either of these works:

```csharp
// Anywhere, without holding a reference. Erased entirely in release builds.
DevActions.Toggle();

// Or through the component, if you already have it.
_bootstrap.Open();
```

Tick **Open On Start** on the bootstrap to have it open on the first frame while you iterate.

Want a keyboard shortcut? Bind it yourself, with whichever input backend you already use:

```csharp
if (Input.GetKeyDown(KeyCode.F1)) DevActions.Toggle();   // legacy input
if (Keyboard.current.f1Key.wasPressedThisFrame) DevActions.Toggle();   // Input System
```

DevKit deliberately ships no hotkey and no touch gesture of its own: it polls nothing, has no `Update`, and never competes with your game's input.

## Registering actions

### Attributes

```csharp
public class Wallet : MonoBehaviour
{
    [DevAction("Economy/Add 1000$")]
    static void AddThousand() => Instance.Add(1000);

    // Parameters become input fields in the panel.
    [DevAction("Economy/Add Custom")]
    static void AddCustom(int amount) => Instance.Add(amount);

    // Instance methods work too - resolved against the first live instance at invoke time.
    [DevAction("Economy/Reset", confirm: true)]
    void ResetWallet() => _coins = 0;
}
```

- The string is a **path**. Everything before the last `/` becomes the category in the left rail.
- `confirm: true` shows a yes/no prompt before firing. Use it for destructive actions.
- Supported parameter types: `int`, `float`, `string`, `bool` and any `enum`. A method taking anything else is skipped with a warning naming the method and the offending type.
- Instance methods must live on a `Component`, otherwise there is nothing to resolve them against.

### Manual registration

For actions that need closures or runtime-decided paths.

```csharp
DevActions.Register("Level/Win",  () => LevelManager.Win());
DevActions.Register("Level/Lose", () => LevelManager.Lose());
DevActions.Register<int>("Level/Go To Level", i => LevelManager.Load(i));

DevActions.RegisterWatch("Player/Coins", () => Wallet.Coins.ToString());

DevActions.Unregister("Level/Win");   // rarely needed; the registry is cleared each play session
```

`RegisterWatch` adds a read-only live-updating label. Getters are polled at 4 Hz, and only while their category is on screen.

`DevActions.Open()`, `Close()` and `Toggle()` drive the panel from anywhere without a reference, and are erased in release builds.

## Built-in modules

| Module | Actions |
|---|---|
| `Time` | Pause, Resume, Timescale 0.1 / 0.5 / 1 / 2 / 5, Set Timescale, Step One Frame, live scale watch |
| `Diagnostics` | FPS watch, Mono heap watch, screen watch, device info, Collect Garbage, Clear PlayerPrefs (confirmed) |
| `Level` | Win, Lose, Restart, Go To Level, Next, Previous |
| `Economy` | Add 100 / 1000, Add / Remove / Set Currency, Max Out, live balance watch |

`Time` and `Diagnostics` always work. `Level` and `Economy` cannot know your classes, so they talk through a small adapter:

```csharp
public class GameAdapter : MonoBehaviour, IDevKitGameAdapter
{
    void Awake() => DevActions.SetAdapter(this);   // optional; DevKit also finds it on its own

    public void WinLevel()               => LevelManager.Win();
    public void LoseLevel()              => LevelManager.Lose();
    public void LoadLevel(int index)     => LevelManager.Load(index);
    public void AddCurrency(long amount) => Wallet.Add(amount);
    public long GetCurrency()            => Wallet.Coins;
}
```

Without an adapter those two modules register nothing and the panel shows a one-line hint under `Game`. Nothing throws and nothing spams the console.

## Stripping it from release builds

The define symbol is **`DEVKIT_ENABLED`**, managed from **Project Settings → DevKit** (it read-modify-writes the define list per build target group, never clobbering it).

With the symbol undefined:

- every `DevActions.*` call site is erased by the C# compiler, arguments included — `DevActions.Register("X", () => Expensive())` costs literally nothing;
- `DevKitBootstrap.Awake` destroys its own GameObject, so a stale object in a shipped scene is harmless;
- there is no `Update`, no canvas, no reflection scan and no registry.

Your game code keeps compiling either way. That is the point of using `[Conditional]` on the API instead of a define constraint on the assembly.

### IL2CPP and managed stripping

`[DevAction]` methods are only ever reached through reflection, so the linker will remove them. The package ships a `link.xml` that preserves its own assemblies. Methods you annotate live in *your* assembly — add `[UnityEngine.Scripting.Preserve]` to them, or ship your own `link.xml`, when building with stripping enabled.

## How it behaves

- **Lazy, and idle.** The reflection scan and the canvas are built on the first `Open()`, never in `Awake` or `Start`. `DevKitBootstrap` has no `Update` at all, so a player who never opens the panel pays literally nothing per frame.
- **Scan cost.** Assemblies prefixed `System`, `Unity`, `mscorlib`, `netstandard`, `Mono.`, `nunit` and `JetBrains` are skipped. Each remaining assembly is scanned inside its own `try/catch`, so one broken plugin cannot take the panel down. A scan over 50 ms is logged.
- **Zero assets.** Colours come from `Texture2D.whiteTexture` tinted by `Image.color`; text uses the engine's built-in font. Nothing is loaded from disk.
- **Non-invasive.** An existing `EventSystem` is left strictly alone; one is only created if the project has none. `Time.timeScale` is only touched when you opt into `pauseWhenOpen`, and the *previous* value is restored — never assumed to be 1.
- **Survives scene loads, not play sessions.** The panel, the event system and the bootstrap are `DontDestroyOnLoad`, so they live in their own scene — disabling every object in your active scene will not hide the panel, and that is deliberate. Close it with the X in the header, or `DevActions.Close()`. Everything DevKit creates is destroyed on `Application.quitting`, which fires when you leave Play Mode too.
- **Failure is visible.** An action that throws shows a red toast with the message and logs the full stack. The panel stays open.

## Panel layout

Search sits in the header and spans every category, labelling each result with its full path. Rows are 88 units tall at the 1920×1080 reference resolution — the minimum comfortable touch target on a phone.

The panel picks one of two layouts from the canvas width:

| Canvas width | Layout |
|---|---|
| ≥ 1200 units | Category rail down the left, actions to its right |
| < 1200 units | Categories become a horizontally scrolling strip of chips above the actions |

A 1080×2400 phone works out to ~966 units, so it gets the stacked layout and the action pane keeps the full window width instead of losing 420 units to a rail. Desktops and landscape tablets keep the rail. The panel re-checks on rotation and on Game-view resizes, and swaps layouts live.

Every widget carries a *minimum* width as well as a preferred one, because a `HorizontalLayoutGroup` will not shrink a child past its minimum — it overflows and clips instead. Long values are clipped by a `RectMask2D` rather than painted over the neighbouring widget.

Nested paths like `Player/Combat/Weapons` become a single rail entry reading `Player/Combat` — the rail is flat, the paths are not.

## Samples

**Basic Integration** — a wallet, an adapter and both registration styles. Import it from the package's page in the Package Manager.

## License

MIT
