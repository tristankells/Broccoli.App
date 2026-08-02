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
