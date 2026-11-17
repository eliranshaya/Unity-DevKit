# Changelog

All notable changes to this package are documented in this file.
The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.2.0] - 2026-11-17

Tables: many things that share the same properties, tuned in one grid. Before, the same job took a Select
row plus a Set row and a Run button per property.

### Added

- `DevActions.RegisterTable` and `DevTable<TRow>`. One row per item (an enum's values, your own objects,
  plain indices), one column per property. Column types:
  - `Label`: row names, optionally a button (the row's main action) and optionally highlighted
  - `Float` and `Int`
  - `Toggle`
  - `Choice`: a dropdown over a list of options, or over an enum's values
  - `Text`: a read-only live value
  - `Button`: the same button on every row, with an optional per-row enabled state
- `DevTable<TRow>.Action` adds a full-width button under the table for something that acts on all of it,
  such as "Reset All". Like an action row, it can ask for confirmation first and reports the outcome in a
  toast.
- Nothing in a table needs a Run button. A typed value is written the moment the edit is committed: Enter,
  Done on a phone keyboard, or tapping anywhere else. A dropdown or a toggle writes on the tap. After every
  change the whole table is read again, so a setter that also changes other columns shows its effect
  immediately. Tables are polled at the watch rate too, so changes made elsewhere appear without reopening
  the panel.
- Columns are sized to what they hold (the longest name, the widest option) and stay aligned row to row.
  Spare width goes to the editable columns, capped at 1.25 times the table's natural width, so a wide screen
  does not stretch a short name column across half the pane.
- Table cells use a larger font than list rows: 30 against 26.
- A dropdown list for table cells. It opens below the cell, or above it when the cell is near the bottom of
  the screen. It scrolls when the options do not fit, and closes when you pick an option or tap anywhere
  outside it. It is not a uGUI `Dropdown`, whose list renders on a nested canvas at sorting order 30000,
  underneath the panel.
- Search matches the names of table rows as well as entry paths, and narrows a matching table to just the
  rows that matched.

### Changed

- The package description no longer says to press F1. The hotkey was removed in 1.1.0.

### Migration

Nothing to migrate: every existing API is unchanged. To move a set of per-item "Select" and "Set" actions
onto a table:

```csharp
DevActions.RegisterTable("Audio/Mixer", new DevTable<AudioSource>(sources)
    .Label("Source", s => s.name, onClick: s => s.Play())
    .Float("Volume", s => s.volume, (s, v) => s.volume = v)
    .Toggle("Mute", s => s.mute, (s, v) => s.mute = v)
    .Action("Stop All", StopAll));
```

## [1.1.1] - 2026-07-28

DevKit stops pretending to know what your game is.

> **Contains breaking changes**, despite being numbered as a patch. Public API is removed. Pin
> `#1.1.0` to keep the adapter and the optional Input System assembly.

### Removed

- `IDevKitGameAdapter`, `DevActions.SetAdapter`, `LevelModule` and `EconomyModule`. What a "level"
  or a "currency" means differs in every project, and implementing five interface methods to get
  six generic buttons was more work than the four `DevActions.Register` lines that give you
  exactly the buttons you want, named the way you think about them.
- With them goes the "adapter missing" hint row, which appeared in projects that never asked for
  those modules in the first place.
- `TimeModule`'s preset buttons - Pause, Resume, Timescale 0.1 / 0.5 / 1 / 2 / 5, Step One Frame.
  One typed field covers every value including the ones nobody guessed, in one row instead of six.
- `DevKitRunner`, the internal coroutine host, which existed only for Step One Frame.
- The optional `Core.DevKit.InputSystem` assembly, along with `IDevKitInputProvider` and
  `DevKitInput.SetProvider`. Three files and a second asmdef existed to make one `AddComponent`
  call; `DevKitInput` now resolves `InputSystemUIInputModule` through `Type.GetType` instead.
  The package still never references `com.unity.inputsystem` from an asmdef, which is the
  constraint that mattered.

### Changed

- `Time` is now a single `Set Timescale` float field plus a live readout. Values are clamped to
  zero and above.
- The package ships a single runtime assembly again.

### Fixed

- The "adapter missing" hint had been registered under `Game/`, a category name host projects
  routinely want for their own actions, so it appeared interleaved with them. Moot now that the
  hint is gone, but it is why the row was in your way.
- A project with `com.unity.inputsystem` installed but **Active Input Handling** set to *Input
  Manager (Old)* was given an `InputSystemUIInputModule`, which receives nothing in that
  configuration. The choice is now guarded on `ENABLE_INPUT_SYSTEM` - the backend being active -
  rather than on the package merely being present.
- When no UI input module can be attached at all, DevKit now logs an error naming the exact
  cause instead of a vague warning. This only arises when the scene has no EventSystem of its
  own, since one the project already owns is never touched.

### Migration

Register the cheats you actually want. This replaces everything the two modules did:

```csharp
DevActions.Register("Level/Win", () => LevelManager.Win());
DevActions.Register<int>("Level/Go To", i => LevelManager.Load(i));
DevActions.Register<int>("Economy/Add Coins", amount => Wallet.Add(amount));
DevActions.RegisterWatch("Economy/= Coins", () => Wallet.Coins.ToString());
```

Delete your `IDevKitGameAdapter` implementation and any `DevActions.SetAdapter` call.

## [1.1.0] - 2026-07-28

DevKit no longer binds any input. You decide what opens the panel.

> **Contains breaking changes** despite the minor version bump. If you were on 1.0.0 and relied on
> the built-in hotkey or gesture, see Migration below. Pin `#1.0.0` to stay on the old behaviour.

### Changed

- `DevKitBootstrap.Open()`, `Close()` and `Toggle()` are now **public and parameterless**, so they
  appear in a UI Button's OnClick dropdown and can be called from your own code. Added
  `DevKitBootstrap.IsOpen`.
- `DevKitBootstrap` has no `Update` at all. Previously it polled for a hotkey and a touch gesture
  every frame of every play session; now a project that never opens the panel runs no DevKit code.

### Removed

- The `toggleKey` inspector field and the `DevKey` enum.
- The mobile multi-finger gesture, along with its `mobileGesture`, `gestureFingers` and
  `gestureHold` fields.
- `IDevKitInputProvider.GetKeyDown` and `.TouchCount`. The interface is now just
  `TryAttachUIModule` - selecting the right UI input module is the only thing DevKit still needs
  from an input backend.

### Migration

Wire a UI Button to `DevKitBootstrap.Open`, or tick **Open On Start**. For a keyboard shortcut,
bind it yourself against whichever backend you already use and call `DevActions.Toggle()`:

```csharp
if (Input.GetKeyDown(KeyCode.F1)) DevActions.Toggle();
```

Serialized values for the removed fields are dropped silently by Unity; nothing needs cleaning up.

## [1.0.0] - 2026-07-27

First release.

### Added

- `DevKitBootstrap` component - one empty GameObject is the entire setup.
- `[DevAction]` attribute registration with `int`, `float`, `string`, `bool` and `enum` parameters.
- `DevActions.Register` / `Register<T>` / `RegisterWatch` / `Unregister` manual API,
  all erased from release builds through `[Conditional("DEVKIT_ENABLED")]`.
- Runtime-generated uGUI panel: category rail, action pane, search, confirm dialog, toasts.
- Responsive layout. Below `DevPanelTheme.NarrowWidthThreshold` canvas units of width the category
  rail becomes a horizontal strip of chips above the pane, so a portrait phone - only ~966 units
  wide at 1080x2400 - keeps the full window width for its rows. Re-evaluated on rotation and on
  Game-view resizes.
- Built-in modules: Level, Economy, Time, Diagnostics.
- `IDevKitGameAdapter` for wiring the Level and Economy modules to a game.
- Both input backends supported; the new Input System lives in an optional assembly
  that compiles only when the package is installed.
- **Project Settings > DevKit** page to toggle the `DEVKIT_ENABLED` define per build target.
- **GameObject > Dev > Add DevKit Bootstrap** menu item.
- **Tools > DevKit > Clean Up Leftover Objects** to clear DevKit's runtime objects from a scene.
- `link.xml` preserving the DevKit assemblies under managed stripping.
