# Basic Integration

1. Create an empty GameObject and add **DevKitBootstrap** to it
   (or use **GameObject > Dev > Add DevKit Bootstrap**).
2. Create a second GameObject and add both **SampleWallet** and **SampleCheats**.
3. Make sure `DEVKIT_ENABLED` is on under **Project Settings > DevKit**.
4. Tick **Open On Start** on the bootstrap, or wire a UI Button to `DevKitBootstrap.Open`.
5. Enter Play Mode.

You should see these categories in the left rail:

| Category | Where it comes from |
|---|---|
| `Sample/Wallet` | `[DevAction]` attributes on `SampleWallet` |
| `Sample/Level` | `DevActions.Register` / `RegisterWatch` in `SampleCheats.Awake` |
| `Time`, `Diagnostics` | built-in modules, always present |

The two styles are interchangeable. Use `[DevAction]` when the method is static or lives on a
component DevKit can find; use `DevActions.Register` when you need a closure over something, or a
path you only know at runtime.

DevKit ships no level or economy cheats of its own — `Sample/Level` above is ordinary user code,
and yours will look the same.
