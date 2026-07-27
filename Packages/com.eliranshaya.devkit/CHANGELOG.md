# Changelog

All notable changes to this package are documented in this file.
The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
