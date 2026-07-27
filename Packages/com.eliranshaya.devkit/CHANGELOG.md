# Changelog

All notable changes to this package are documented in this file.
The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
