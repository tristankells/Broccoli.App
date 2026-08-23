# Broccoli.Avalonia.Desktop.E2ETests

End-to-end UI tests that drive the **real desktop app** through Appium + the Windows driver
(WinAppDriver). These launch the actual `Broccoli.Avalonia.Desktop.exe`, click real controls, and
verify real persisted state — the opposite end of the spectrum from the in-process headless UI
tests in `Broccoli.Avalonia.UnitTests`.

## What it covers

One end-to-end scenario (add more as separate test methods):

1. Launch the app
2. Click **Add Recipe**
3. Type a name
4. Click **Save**
5. Confirm the recipe card shows up in the recipe list
6. Confirm the recipe was persisted under a **throwaway data folder** (never the real app data)

## Prerequisites

- **Windows** (WinAppDriver is Windows-only).
- **Appium 2.x** installed globally: `npm install -g appium`
- **Windows driver**: `appium driver install windows` (the driver manages WinAppDriver itself;
  it downloads/uses classic WinAppDriver 1.2.x).
- .NET SDK (same as the rest of the repo).

### One-time driver patch

The Appium **windows driver 2.x** has a known incompatibility with classic WinAppDriver
([appium/appium-windows-driver#316](https://github.com/appium/appium-windows-driver/issues/316)):
it probes WinAppDriver's `GET /status` (which classic WinAppDriver answers with HTTP 500 even when
healthy) and it forwards `appium:`-prefixed capabilities that WinAppDriver doesn't understand.
Until a fixed driver ships, run the included patch once (it is idempotent and only edits the
driver's `winappdriver.js`):

```bash
node Broccoli.Avalonia/Broccoli.Avalonia.Desktop.E2ETests/patch-windows-driver.js
```

Re-run it after any `appium driver update windows`. The test suite fails fast with a reminder if
it's missing.

## Running

```bash
dotnet test Broccoli.Avalonia/Broccoli.Avalonia.Desktop.E2ETests/Broccoli.Avalonia.Desktop.E2ETests.csproj
```

The test auto-starts an Appium server on `127.0.0.1:4723` if one isn't already running, and stops
it when the run finishes (a server you started yourself is left alone). Appium logs go to
`%TEMP%\broccoli-e2e-appium.log`.

Environment overrides:

| Variable | Purpose |
|---|---|
| `BROCCOLI_DESKTOP_EXE` | Path to `Broccoli.Avalonia.Desktop.exe` if not resolved from the build output |
| `APPIUM_BIN` | Path to the `appium` executable/cmd if it's not on `PATH` |

## How data isolation works

- The desktop app accepts `--appdata <folder>` (see `AppPaths.OverrideRootFolder` +
  `Program.ApplyAppDataOverride`). Every test launches the app with this argument.
- `TestData.CreateScratchDataFolder` creates a unique, **empty** folder under `%TEMP%\broccoli-e2e`
  per test — that empty folder *is* the data reset. The real `%LocalAppData%\Broccoli` data is
  never touched, so you can run the suite on a machine that has real recipes.
- The folder is deleted on cleanup; the recipes folder is asserted to contain exactly the one
  recipe the test created.

## Structure

| File | Purpose |
|---|---|
| `RecipeWorkflowTests.cs` | The test scenario (add more `[TestMethod]`s here) |
| `AppiumSession.cs` | Per-test session: launches the app, finds/wait-for elements |
| `AppiumServer.cs` | Process-wide Appium server lifecycle (start/stop/health check) |
| `DesktopAppPaths.cs` | Resolves the built desktop exe |
| `TestData.cs` | Scratch data-folder reset helpers |
| `patch-windows-driver.js` | One-time patch for the windows driver (see above) |

## Finding controls

The app exposes UI Automation identifiers via `AutomationProperties.AutomationId`/`Name` on the
controls the tests drive (the recipe list/edit pages). Add more `AutomationProperties` when you
test new screens. Elements are located with `By.XPath("//*[@AutomationId='...']")`.

## macOS note

Appium's `mac2` driver is the macOS equivalent, but it is a separate driver with its own
capabilities and element semantics, and WinAppDriver does not run on macOS. The client code here
is Windows-specific; a `mac2` variant would need a separate driver factory and a macOS runner.
