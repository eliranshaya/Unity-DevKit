# Unity-DevKit

A zero-setup, runtime-generated developer/cheat panel for Unity games.

The entire package is stripped from release builds via a define symbol. In dev builds,
the user drops **one empty GameObject** into a scene, presses a hotkey, and a full
debug panel builds itself at runtime — no prefabs, no scene wiring, no assets.

---

## 1. Non-negotiable principles

These are the rules that define the package. Never violate them without asking.

1. **Zero assets.** The package must work with *no* prefabs, sprites, fonts, or
   ScriptableObjects shipped alongside it. Everything is constructed in C# at runtime.
   A user must be able to `git clone` into `Packages/` and it just works.
2. **Zero scene setup.** One empty GameObject + one component. Nothing else.
3. **Zero cost in release.** With `DEVKIT_ENABLED` undefined, the package must contribute
   nothing: no allocations, no `Update()` calls, no GameObjects, ideally no IL.
4. **Never break the user's build.** Calling DevKit APIs from game code must still
   *compile* when the symbol is off. See §6.
5. **The game never depends on DevKit.** DevKit reads and pokes the game; the game must
   never `using DevKit;` in production code paths.

---

## 2. Repo structure

The repo is a working Unity project **and** the installable package, the same shape as
`Unity-ActionFlow` and `Unity-SoundBalance`: the package lives under `Packages/`, so users
install it with

```
https://github.com/eliranshaya/Unity-DevKit.git?path=/Packages/com.eliranshaya.devkit
```

Assemblies follow the sibling repos' `Core.<Name>` convention. `.meta` files are committed —
consumers must not get fresh GUIDs on every clone.

```
Unity-DevKit/
├── README.md
├── CLAUDE.md
├── Assets/                                   # scratch test scene, never shipped
└── Packages/
    └── com.eliranshaya.devkit/               # the package users install
        ├── package.json                      # com.eliranshaya.devkit
        ├── CHANGELOG.md
        ├── LICENSE.md
        ├── Runtime/
        │   ├── Core.DevKit.asmdef
        │   ├── link.xml                      # preserves DevKit under managed stripping
        │   ├── Core/
        │   │   ├── DevKitBootstrap.cs        # the component the user drops in the scene
        │   │   ├── DevActions.cs             # public registration API
        │   │   ├── DevActionAttribute.cs     # the [DevAction] attribute
        │   │   ├── DevActionEntry.cs         # internal record of one registered entry
        │   │   ├── DevActionRegistry.cs      # storage + reflection scan
        │   │   ├── DevKitAdapter.cs          # resolves IDevKitGameAdapter
        │   │   ├── DevKitCompat.cs           # every 2021.3-vs-Unity-6 API difference
        │   │   ├── DevKitInput.cs            # hotkey abstraction (old + new input system)
        │   │   ├── DevKitLog.cs
        │   │   ├── DevKitRunner.cs           # hidden coroutine host for static modules
        │   │   ├── DevKey.cs                 # backend-neutral key enum
        │   │   └── IDevKitGameAdapter.cs
        │   ├── UI/
        │   │   ├── DevPanel.cs               # builds + owns the runtime canvas
        │   │   ├── DevPanelBuilder.cs        # low-level uGUI construction helpers
        │   │   ├── DevPanelTheme.cs          # colors, sizes, spacing — all code constants
        │   │   └── Widgets/
        │   │       ├── DevActionRow.cs       # button row, with or without param fields
        │   │       ├── DevParamField.cs      # int / float / string / bool / enum field
        │   │       ├── DevWatchRow.cs        # live-updating read-only label
        │   │       ├── DevInfoRow.cs         # static hint text
        │   │       ├── DevToast.cs
        │   │       └── DevConfirmDialog.cs
        │   ├── Modules/
        │   │   ├── BuiltinModules.cs         # installs the four below
        │   │   ├── LevelModule.cs            # Win / Lose / Load Level
        │   │   ├── EconomyModule.cs          # Add / Remove currency
        │   │   ├── TimeModule.cs             # timescale, pause, step
        │   │   └── DiagnosticsModule.cs      # FPS, memory, screen info
        │   └── InputSystem/                  # optional assembly — see §6
        │       ├── Core.DevKit.InputSystem.asmdef
        │       └── DevKitInputSystemProvider.cs
        ├── Editor/
        │   ├── Core.DevKit.Editor.asmdef
        │   ├── DevKitDefines.cs              # read-modify-write of the define list
        │   ├── DevKitSettingsProvider.cs     # Project Settings > DevKit (toggles the symbol)
        │   └── DevKitMenu.cs                 # GameObject > Dev > Add DevKit Bootstrap
        └── Samples~/
            └── BasicIntegration/
```

---

## 3. Public API

This is the surface users touch. Keep it small and stable.

### 3.1 Attribute registration (preferred)

```csharp
public class Wallet : MonoBehaviour
{
    [DevAction("Economy/Add 1000$")]
    static void AddThousand() => Instance.Add(1000);

    // Parameters become input fields in the panel.
    [DevAction("Economy/Add Custom")]
    static void AddCustom(int amount) => Instance.Add(amount);

    // Instance methods work too — resolved via FindObjectOfType at invoke time.
    [DevAction("Economy/Reset", confirm: true)]
    void ResetWallet() => _coins = 0;
}
```

- The string is a **path**. `/` creates nested categories in the panel.
- `confirm: true` shows a yes/no prompt before firing. Use for destructive actions.
- Supported parameter types: `int`, `float`, `string`, `bool`, and `enum`.
  Anything else → skip the method and log a clear warning naming the method and type.

### 3.2 Manual registration

For actions that need closures or runtime-dynamic paths.

```csharp
DevActions.Register("Level/Win", () => LevelManager.Win());
DevActions.Register("Level/Lose", () => LevelManager.Lose());
DevActions.Register<int>("Level/Go To Level", i => LevelManager.Load(i));
DevActions.RegisterWatch("Player/Coins", () => Wallet.Coins.ToString());

DevActions.Unregister("Level/Win");   // rarely needed; registry is cleared on domain reload
```

`RegisterWatch` adds a read-only live-updating label. Poll it at ~4 Hz, never per-frame.

---

## 4. Bootstrap flow

`DevKitBootstrap` is a `MonoBehaviour` with inspector fields for hotkey, mobile gesture,
and `dontDestroyOnLoad` (default true).

```
Awake()
 └─ if (!DEVKIT_ENABLED) { Destroy(gameObject); return; }
 └─ enforce singleton (destroy self if another instance exists)
 └─ DontDestroyOnLoad
 └─ DO NOT build UI, DO NOT run the reflection scan

Update()
 └─ poll DevKitInput for the toggle (default F1; mobile: 3-finger tap held 0.5s)
 └─ on first toggle:
      ├─ DevActionRegistry.ScanAssemblies()   // one time, cached
      └─ DevPanel.Build()                     // one time, cached, then SetActive
 └─ on subsequent toggles: just SetActive(!active)
```

**The reflection scan and UI construction must be lazy.** A user who never opens the
panel should pay nothing but one `if` per frame.

### Scan rules

- Iterate `AppDomain.CurrentDomain.GetAssemblies()`, but **skip** assemblies whose name
  starts with `System`, `Unity`, `mscorlib`, `netstandard`, `Mono.`, `nunit`, `JetBrains`.
- Wrap the whole scan in `try/catch` per-assembly; a single `ReflectionTypeLoadException`
  must not kill the panel.
- If the scan exceeds ~50 ms, log the duration at `Debug.Log` level so users can see it.

---

## 5. UI generation rules

Use **uGUI built in code**, not UI Toolkit. Rationale: UI Toolkit needs a `PanelSettings`
asset and a theme stylesheet, which violates the zero-assets rule.

### Construction

- Build one root `GameObject("[DevKit] Panel")` with `Canvas` (`RenderMode.ScreenSpaceOverlay`,
  `sortingOrder = short.MaxValue`), `CanvasScaler` (`ScaleWithScreenSize`, 1920×1080,
  `matchWidthOrHeight = 0.5f`), and `GraphicRaycaster`.
- Create every persistent GameObject through `DevKitScene.NewRoot`, never `new GameObject` +
  `DontDestroyOnLoad` directly. It is the single place that owns flags and teardown.
- **Never use `HideFlags.DontSave` on a runtime object.** `DontSave` means *both* "never serialise
  me" and "survive a scene unload" — and leaving Play Mode is a scene unload, so the object lands
  in the Edit Mode hierarchy and accumulates one per run. A runtime-created GameObject is never
  written to a scene asset anyway, so the flag buys nothing. Use `HideFlags.None`.
- If no `EventSystem.current` exists, create one. If one exists, **do not touch it.**
  Add `StandaloneInputModule` or `InputSystemUIInputModule` depending on the active input backend.
- Fonts: `Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")`. Fall back to
  `"Arial.ttf"` for Unity < 2022. Never require TMP; if TMP is present it's still fine to
  use legacy `Text` — consistency beats prettiness here.
- Images: `Texture2D.whiteTexture` + `Image.color`. Never load a sprite from disk.

### Layout

- Left rail listing categories → right pane listing actions in the selected category.
- **Responsive.** Below `DevPanelTheme.NarrowWidthThreshold` canvas units of width the rail is
  swapped for a horizontal strip of category chips above the pane, so the pane keeps the full
  window width. A 1080×2400 phone is only ~966 units wide — a 420-unit rail there costs half the
  window and forces rows to clip. `DevPanel.ApplyLayout` re-evaluates on rotation and resize.
- **Every widget declares a minimum width as well as a preferred one.** A `HorizontalLayoutGroup`
  refuses to shrink a child below its minimum and overflows instead, so a row whose minimums do
  not fit the narrowest supported pane *will* clip. Use `DevPanelBuilder.SetLayout`, not
  `SetSize`, for anything inside a row. Clip user-supplied text with `DevPanelBuilder.Clip`.
- Every list is inside a `ScrollRect` with a `VerticalLayoutGroup` +
  `ContentSizeFitter`. Assume 100+ registered actions.
- Minimum touch target: 88×88 px at the reference resolution. This gets used on phones.
- All colors, paddings, and font sizes live as `const`/`static readonly` in
  `DevPanelTheme`. Never inline a magic number in a widget.

### Behavior

- While the panel is open, set `Time.timeScale = 0` **only if** the user enabled
  `pauseWhenOpen`. Default: false. Restore the *previous* value on close, never assume 1.
- Invoking an action must be wrapped in try/catch. An exception shows a red toast in the
  panel with the message and logs the full stack — it must never close the panel.

---

## 6. Conditional compilation strategy

The define symbol is **`DEVKIT_ENABLED`**. Not `DEVELOPER_MODE` — that's too generic and
will collide with other packages in the same project.

**Do not put a define constraint on the asmdef.** If the assembly stops compiling, every
user call site becomes a compile error. Instead:

```csharp
public static class DevActions
{
    [System.Diagnostics.Conditional("DEVKIT_ENABLED")]
    public static void Register(string path, Action action)
    {
#if DEVKIT_ENABLED
        DevActionRegistry.Add(path, action);
#endif
    }
}
```

`[Conditional]` makes the C# compiler erase the *call site* in release builds — arguments
included, so `DevActions.Register("X", () => Expensive())` costs literally nothing.

Rules:
- Every public `void` API gets `[Conditional("DEVKIT_ENABLED")]`.
- Any API that must return a value cannot use `[Conditional]` — wrap its body in `#if`
  and return a safe default. Prefer designing these away.
- `DevKitBootstrap.Awake()` self-destructs when the symbol is off, so a stale GameObject
  left in a shipped scene is harmless.
- The editor settings provider writes the symbol to
  `PlayerSettings.SetScriptingDefineSymbols` for the active build target group. It must
  read the existing list, add/remove only our symbol, and write it back — never clobber.

### The one asmdef that *does* carry a define constraint

`Core.DevKit.InputSystem` is constrained on `DEVKIT_HAS_INPUT_SYSTEM`, which its own
`versionDefines` sets when `com.unity.inputsystem` is installed. This is **not** the rule above
being broken — the constraint is on the *package being present*, never on `DEVKIT_ENABLED`.

The reason it exists: `Core.DevKit` must never reference the Input System, or a project without
that package fails to compile. So the Input System code lives in a separate assembly that simply
is not built when the package is missing, and hands itself to `DevKitInput.SetProvider` through
`[RuntimeInitializeOnLoadMethod]`. Core depends on nothing; the optional assembly depends on Core.
Keep it that way — never add an Input System reference to `Core.DevKit`.

### IL2CPP / managed stripping

Attributed methods are only reached via reflection, so the linker will strip them.
Mark `DevActionAttribute` usages as preserved by shipping a `link.xml` in `Runtime/`
that preserves the DevKit assembly, and document that users should add
`[UnityEngine.Scripting.Preserve]` to their own attributed methods in stripped builds.

---

## 7. Built-in modules

Modules are static classes with `[DevAction]` methods that self-register via the scan.
Each module must **degrade gracefully** — the game may not have the relevant system.

| Module | Actions |
|---|---|
| `LevelModule` | Win Level, Lose Level, Restart, Go To Level (int), Next, Previous |
| `EconomyModule` | Add Currency (int), Remove Currency (int), Set Currency (int), Max Out |
| `TimeModule` | Pause, Timescale 0.1/0.5/1/2/5, Step One Frame |
| `DiagnosticsModule` | FPS watch, memory watch, resolution, device model, clear PlayerPrefs (confirm) |

`LevelModule` and `EconomyModule` cannot know the game's classes. They call into a small
adapter interface the user implements:

```csharp
public interface IDevKitGameAdapter
{
    void WinLevel();
    void LoseLevel();
    void LoadLevel(int index);
    void AddCurrency(long amount);
    long GetCurrency();
}
```

The adapter is found once via `FindObjectsOfType` / a registered instance. If none is
found, those modules register **nothing** and the panel shows a one-line hint explaining
how to add an adapter. Never throw, never spam the console.

---

## 8. Conventions

- Namespace: `DevKit` for public API, `DevKit.Internal` for everything else.
- Target: Unity 2021.3 LTS and up. No C# 10+ features.
- **No allocations per frame.** Cache `StringBuilder`s, reuse widget instances, pool list
  rows when the panel refreshes.
- Prefer `TryGet`-style methods over exceptions for control flow.
- All logs are prefixed `[DevKit]` and go through `DevKitLog`, which is itself
  `[Conditional("DEVKIT_ENABLED")]`.
- Public API changes require a matching README update in the same commit.
- Input: support both backends behind `DevKitInput`, guarded by `#if ENABLE_INPUT_SYSTEM`
  and `#if ENABLE_LEGACY_INPUT_MANAGER`. Both can be enabled simultaneously — handle it.

---

## 9. Do not

- Do not add a dependency on TextMeshPro, DOTween, Odin, Newtonsoft, or any third party.
- Do not use `Resources.Load` on user-supplied paths.
- Do not create an `Assets/` folder or write files in the project outside `Library/`.
- Do not modify `Time.timeScale`, `Application.targetFrameRate`, `QualitySettings`, or the
  existing `EventSystem` without restoring the prior value.
- Do not run the reflection scan in `Awake` or `Start`.
- Do not swallow exceptions silently — always log with the action path in the message.
- Do not ship a scene, a prefab, or a `.meta`-heavy asset folder in `Runtime/`.

---

## 10. Verification checklist

Before considering any change done:

1. Fresh project, package added via UPM, empty GameObject + `DevKitBootstrap`, press F1 →
   panel appears with the built-in modules. No console errors, no missing font boxes.
2. Remove `DEVKIT_ENABLED` → project still compiles, including user code that calls
   `DevActions.Register`. Panel never appears. Bootstrap GameObject self-destructs.
3. Build for Android/iOS with IL2CPP + managed stripping High → attributed actions still
   appear and still fire.
4. Register 200 actions across 15 categories → panel opens in under 100 ms and scrolls
   without frame drops.
5. Both input backends, and both enabled at once.
6. Scene load while the panel is open → panel survives and stays functional.
