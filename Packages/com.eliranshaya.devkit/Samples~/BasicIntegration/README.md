# Basic Integration

1. Create an empty GameObject and add **DevKitBootstrap** to it
   (or use **GameObject > Dev > Add DevKit Bootstrap**).
2. Create a second GameObject and add both **SampleWallet** and **SampleGameAdapter**.
3. Make sure `DEVKIT_ENABLED` is on under **Project Settings > DevKit**.
4. Tick **Open On Start** on the bootstrap (or wire a UI Button to `DevKitBootstrap.Open`).
5. Enter Play Mode.

You should see these categories in the left rail:

| Category | Where it comes from |
|---|---|
| `Sample/Wallet` | `[DevAction]` attributes on `SampleWallet` |
| `Sample/Level` | `DevActions.Register` / `RegisterWatch` in `SampleGameAdapter.Start` |
| `Level`, `Economy` | built-in modules, enabled by `SampleGameAdapter` |
| `Time`, `Diagnostics` | built-in modules, always present |

Remove `SampleGameAdapter` from the scene and the `Level` and `Economy` categories disappear,
replaced by a single hint row under `Game`. That is the intended degradation - never an error.
