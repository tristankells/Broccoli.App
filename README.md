# Broccoli.App

A cross-platform desktop app for recipe management, meal planning, and nutrition tracking — built with Avalonia UI and .NET.

## Features

- **Recipe Management** — create, edit, import, and browse recipes with images and markdown rendering
- **Ingredient Parsing** — parses ingredient text against a food database with multi-stage fuzzy matching
- **Nutrition Breakdown** — per-recipe and per-serving calories, protein, carbs, and fat
- **Meal Macro Comparison** — compare recipe macros against per-person daily targets
- **Seasonality Scoring** — NZ produce seasonality analysis with scarcity weighting
- **Macro / Calorie Targets** — multi-person BMR, TDEE, and macro calculator
- **Day Plans** — daily food planning with tabs, food rows, and target comparison
- **Meal Prep** — group recipes into plans with batch grocery cart integration
- **Grocery List** — checkable shopping list with ingredient deduplication
- **Pantry** — track what you always have vs. need to check
- **Food Database** — browseable/editable food table with USDA search and JSON import/export
- **Google Drive Backup** — sync and backup via Google Drive

## Google Drive Sync

Two-way backup of recipes + app data to Google Drive. Sync is **on-demand only** — there is no periodic timer, file watcher, or background daemon. It runs in exactly four situations:

| Trigger | Method | When |
|---|---|---|
| App startup (desktop) | `SyncAsync()` | Fire-and-forget after the first frame + DB migration (`App.axaml.cs`) — pulls remote changes, pushes local ones |
| Drive connection | `SyncNowAsync()` | Immediately after the user connects their Google account in Settings > Sync |
| Manual | `SyncNowAsync()` | The "Sync now" button in Settings > Sync |
| App shutdown (desktop) | `PushOnlyAsync()` | Best-effort push of local-only changes, fire-and-forget so closing is never blocked (`App.axaml.cs` shutdown hook) |

Notes:

- Automatic sync only runs on the **desktop** host. Android/iOS/Browser never auto-sync — the sync service is resolved only on the `IClassicDesktopStyleApplicationLifetime` branch in `App.axaml.cs`.
- All triggers share one singleton `IGoogleDriveSyncService` (registered in `ServiceCollectionExtensions`).
- If Drive isn't connected, `SyncAsync`/`PushOnlyAsync` no-op with `SyncResult.NotConnected`.
- There is no automatic sync while the app is open — local edits are pushed at the next trigger (shutdown or a manual "Sync now").
- Progress is reported via `IProgress<SyncProgress>`; the service never throws — failures return `SyncResult { Success = false, ErrorMessage }`.

## Getting Started

### Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download) 10.0 or newer

### Build & Run

```bash
# Desktop (Windows)
dotnet run --project Broccoli.Avalonia/Broccoli.Avalonia.Desktop
```

```bash
# Tests
dotnet test Broccoli.Avalonia/Broccoli.Avalonia.UnitTests
```

## Project Structure

| Folder | Purpose |
|---|---|
| `Broccoli.Avalonia/Broccoli.Avalonia/` | Shared library — models, services, views, and ViewModels |
| `Broccoli.Avalonia/Broccoli.Avalonia.Desktop/` | Desktop host (Windows) |
| `Broccoli.Avalonia/Broccoli.Avalonia.Android/` | Android host |
| `Broccoli.Avalonia/Broccoli.Avalonia.iOS/` | iOS host |
| `Broccoli.Avalonia/Broccoli.Avalonia.Browser/` | Browser (WASM) host |
| `Broccoli.Avalonia/Broccoli.Avalonia.UnitTests/` | Unit tests |

## License

MIT
