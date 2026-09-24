# Sailwind Food Preservation

A BepInEx mod for Sailwind that lets you adjust smoking and drying speed for fish, meat, and produce. It changes preservation progress without changing food nutrition, cooking speed, or fuel burn rate.

## Configuration

After the first launch, edit `BepInEx/config/skeptic043.sailwind.foodpreservation.cfg`. Each section has `FishMultiplier`, `MeatMultiplier`, and `ProduceMultiplier`:

- `[Smoking]` changes smoking progress.
- `[Drying]` changes ambient drying progress.
- `[SaltedDrying]` adds a multiplier to salted drying. The extra effect scales with how salted the food is.

Every multiplier accepts `0.1` to `5.0`; `1.0` keeps vanilla speed. Food the mod cannot classify keeps vanilla speed. Optional diagnostic logging is under `[Diagnostics]` and defaults off.

## Install and build

Requires Sailwind with [BepInEx 5](https://github.com/BepInEx/BepInEx/releases). Build with `./Build.ps1 -GameDir <Sailwind folder> -BepInExCore <BepInEx core folder>`, then place `SailwindFoodPreservation.dll` from `src/bin/Release/netstandard2.0/` in `BepInEx/plugins/`.

This is a development build. In-game timing, save/reload, and mod combinations still need testing.

## Compatibility

BetterFishing changes stove and smoker efficiency, so it can also affect smoking time. Combined behavior has not yet been tested. Sailwind-Coop compatibility has not yet been tested; this mod uses vanilla preservation values and adds no network state.

Hooks are checked at runtime without a fixed game-build allowlist. If a preservation hook changes in a future Sailwind update, the affected rate stays vanilla and a warning is logged.

Licensed under MIT. See [LICENSE](LICENSE).
