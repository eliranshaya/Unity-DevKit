# Changelog

All notable changes to this package are documented in this file.
The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Fixed

- Panel layout on narrow screens. At 1080x2400 the canvas is only ~966 reference units wide, so
  the 420-unit category rail left the action pane too small for its own rows and they clipped off
  the right edge. Below `NarrowWidthThreshold` the rail is now a horizontal strip of category
  chips above the pane, which hands the pane the full window width.
- Every row widget now declares a minimum width as well as a preferred one, so rows shrink to fit
  instead of overflowing. Input fields, watch values and category chips clip their text.
- The panel re-evaluates its layout on rotation and Game-view resizes.
- The confirm dialog was a fixed 900-unit box, wider than the whole canvas on a portrait phone.
  It now stretches on narrow screens.
- **`[DevKit] Panel`, `[DevKit] EventSystem` and `[DevKit] Runner` survived Play Mode.** They were
  flagged `HideFlags.DontSave`, which also means "survive a scene unload" - and leaving Play Mode
  is a scene unload, so one copy of each was left behind per run. They now use `HideFlags.None`,
  are created through the new `DevKitScene.NewRoot`, and are destroyed explicitly on
  `Application.quitting`. **Tools > DevKit > Clean Up Leftover Objects** removes copies left by
  earlier versions.

## [1.0.0] - 2026-07-27

### Added

- `DevKitBootstrap` component - one empty GameObject is the entire setup.
- `[DevAction]` attribute registration with `int`, `float`, `string`, `bool` and `enum` parameters.
- `DevActions.Register` / `Register<T>` / `RegisterWatch` / `Unregister` manual API,
  all erased from release builds through `[Conditional("DEVKIT_ENABLED")]`.
- Runtime-generated uGUI panel: category rail, action pane, search, confirm dialog, toasts.
- Built-in modules: Level, Economy, Time, Diagnostics.
- `IDevKitGameAdapter` for wiring the Level and Economy modules to a game.
- Both input backends supported; the new Input System lives in an optional assembly
  that compiles only when the package is installed.
- **Project Settings > DevKit** page to toggle the `DEVKIT_ENABLED` define per build target.
- **GameObject > Dev > Add DevKit Bootstrap** menu item.
- `link.xml` preserving the DevKit assemblies under managed stripping.
