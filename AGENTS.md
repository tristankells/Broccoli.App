# Broccoli.App — Agent Guide

## Architecture Overview

Cross-platform **Avalonia 12** app (Windows / macOS / Linux / Android / iOS / Browser) built with **MVVM** (`CommunityToolkit.Mvvm`) and `Microsoft.Extensions.DependencyInjection` for DI.

| Project | Purpose |
|---|---|
| `Broccoli.Avalonia/` | **Core** — all views, view models, models, services, and storage. |
| `Broccoli.Avalonia.Desktop/` | Desktop entry point (Windows/macOS/Linux). Thin shell. |
| `Broccoli.Avalonia.Android/` | Android entry point + platform Google OAuth. Thin shell. |
| `Broccoli.Avalonia.iOS/` | iOS entry point + platform Google OAuth. Thin shell. |
| `Broccoli.Avalonia.Browser/` | WebAssembly (WASM) host. Thin shell. |
| `Broccoli.Avalonia.UnitTests/` | MSTest + Moq unit tests. |

**All UI and business logic lives in `Broccoli.Avalonia/`.** Platform host projects only supply entry points and platform-specific OAuth implementations.

## Project Layout — Vertical Slice Architecture

```
Broccoli.Avalonia/
    App.axaml(.cs)              ← composition root: builds DI container, applies EF migrations, sets MainWindow
    ServiceCollectionExtensions.cs  ← AddAppServices(): registers all services + view models
    ViewLocator.cs              ← explicit VM → View type map (no reflection/name-convention guessing)
    Shell/                      ← MainView (DrawerPage), MainViewModel, MainWindow
    Models/                     ← domain models (namespace Broccoli.Avalonia.Models)
    Storage/                    ← AppPaths, BroccoliDbContext (EF Core/SQLite), RecipeMarkdownStore,
                                  TombstoneStore, Migrations/
    Shared/                     ← ViewModelBase, cross-slice messenger records (e.g. PantryListChangedMessage),
                                  XAML value converters (Shared/Converters/)
    _Shared/IngredientParsing/  ← IFoodService, IngredientParserService, FoodService, FoodDatabaseSeeder, ParsedIngredient, ...
    _Shared/Seasonality/        ← ISeasonalityService, SeasonalityService, SeasonalityDataStore, ProduceSeeder, SeasonHelper
    Slices/
        Groceries/              ← GroceriesView(+VM), AddToCartDialog, IGroceryListService, IngredientCartService
        Pantry/                 ← PantryView(+VM), IPantryService, PantryService
        Seasonality/            ← SeasonalityView(+VM), ProduceItemRowViewModel (editable seasonality data tab)
        Planning/               ← PlanningPageView, MacroTargetsView, DayPlanView, MealPrepView + services
        Recipes/                ← RecipeListPageView, RecipeDetailView, RecipeEditView, IRecipeService, RecipeService
            Import/             ← ImportDialog, IImportFormat (PaprikaHtmlImportFormat, BargainBoxPasteImportFormat), RecipeImportService
        Settings/               ← SettingsView, FoodDatabaseView, RecipeSettingsView, USDA search, Google Drive account
            Sync/               ← GoogleDriveSyncService, SyncModels, DriveFileHelper
```

Each slice owns its views, view models, service interfaces, and implementations side-by-side. Services are registered centrally in `ServiceCollectionExtensions.AddAppServices()` (and the two `_Shared` helpers `AddIngredientParsing()` / `AddSeasonality()`), not per-slice.

## Namespace Convention

| Location | Namespace |
|---|---|
| `Shell/` | `Broccoli.Avalonia.Shell` |
| `Models/` | `Broccoli.Avalonia.Models` |
| `Storage/` | `Broccoli.Avalonia.Storage` |
| `Shared/` | `Broccoli.Avalonia.Shared` |
| `_Shared/IngredientParsing/` | `Broccoli.Avalonia.IngredientParsing` |
| `_Shared/Seasonality/` | `Broccoli.Avalonia.Seasonality` |
| `Slices/Groceries/` | `Broccoli.Avalonia.Slices.Groceries` |
| `Slices/Recipes/Import/` | `Broccoli.Avalonia.Slices.Recipes.Import` |
| *(other slices follow the same pattern)* | `Broccoli.Avalonia.Slices.<SliceName>` |

## MVVM & DI Pattern

- View models derive from `ViewModelBase` (`ObservableObject`) and use `[ObservableProperty]` / `[RelayCommand]` source generators. Full-word naming only — no abbreviations or single-letter names (except trivial loop counters).
- Views are `UserControl`s with **compiled bindings** (`x:DataType`), plus a `.axaml.cs` code-behind (usually just `InitializeComponent()`). Each page is a three-file set: `Foo.axaml` + `Foo.axaml.cs` + `Foo.axaml` (no per-page `.razor`; styles are inline or in `App.axaml`).
- DI: `App.OnFrameworkInitializationCompleted()` builds a `ServiceCollection`, calls `AddAppServices()`, and publishes the provider via `CommunityToolkit.Mvvm`'s `Ioc.Default`. View models use constructor injection.
- **Views are NOT registered in DI** — `ViewLocator` maps a view-model type to its view via an explicit `Dictionary<Type, Func<Control>>`. Add a mapping entry whenever you add a new page view model.

## Navigation

- The shell is `Shell/MainView.axaml`, which wraps an Avalonia 12 **`DrawerPage`**.
- `MainViewModel` exposes `MenuItems` (each a `Title`, `Icon` Geometry, and a singleton page view model), `SelectedMenuItem`, `CurrentPage`, and `IsMenuOpen`.
- `DrawerPage` is **adaptive by default**: with `DrawerBreakpointLength="600"` and `DrawerLayoutBehavior="CompactInline"` it shows a 56px icon rail on wide screens and collapses to a hamburger button + overlay drawer below the breakpoint — no code-behind needed.
- Selecting a nav item (ListBox `SelectedItem`) sets `SelectedMenuItem`; `OnSelectedMenuItemChanged` updates `CurrentPage` and closes the drawer (`IsMenuOpen = false`). Content is a `TransitioningContentControl` bound to `CurrentPage`, resolved by `ViewLocator`.
- Page view models are registered as **singletons** so switching sections preserves each page's in-progress state.

## Storage

- **Structured data** (grocery list, pantry, meal prep plans, macro targets, daily food plans) → local **SQLite via EF Core** (`Storage/BroccoliDbContext.cs`). Schema migrations in `Storage/Migrations/`, applied on startup (`db.Database.Migrate()`).
- **Recipes** → human-readable **Markdown + YAML frontmatter + images**, one folder per recipe (`Storage/RecipeMarkdownStore.cs`), so they're easy to edit outside the app and cheap to sync incrementally.
- `Storage/AppPaths.cs` resolves the platform app-data folder (`{AppData}/Broccoli/`) and creates it lazily — the app works fully offline from first launch.

## Ingredient Parsing Pipeline

`IngredientParserService.ParseAndMatchIngredientsAsync()` → regex parse → `FoodService.FindBestMatch()` (exact → stopword-stripped → token ratio → `FuzzySharp` WRatio). Foods live in SQLite (`Foods` table); `FoodDatabase.json` is embedded as an `avares://` resource and used only as the initial seed (and to reset the database from Settings). Register via `services.AddIngredientParsing()`.

## Seasonality

Produce data lives in SQLite (`ProduceItems` table) with **per-month availability** (`SeasonalityState` = InSeason / PartiallyInSeason / OutOfSeason for months 1–12), seeded once from the embedded `Assets/nz-produce.json` via `ProduceSeeder` (the JSON is never the live store). `SeasonalityService` scores recipe ingredients against that data (partial counts at half weight) and reloads its cache when `SeasonalityDataChangedMessage` is raised. `SeasonHelper` maps months→seasons for the banner and derives the scarcity weight from the in-season month count. The Seasonality nav tab (Slices/Seasonality, hideable from Settings > Seasonality) edits the dataset month by month. Register via `services.AddSeasonality()`.

## Google Drive Sync (Settings > Sync)

- `GoogleDriveSyncService` backs up/restores recipes + database to Drive; `TombstoneStore` records deletions so they propagate; `sync-state.json` tracks per-device progress.
- OAuth is platform-specific: `DesktopGoogleDriveOAuthPlatform` (loopback) on desktop; `AndroidGoogleDriveAuthService` (Google Identity Services) on Android; an iOS implementation on iOS. Platform heads set `App.GoogleDriveOAuthPlatformOverride` / `App.GoogleDriveAuthServiceOverride` before the app initializes.

## Build & Run Commands

```bash
# Desktop (Windows/macOS/Linux)
dotnet run --project Broccoli.Avalonia/Broccoli.Avalonia.Desktop/Broccoli.Avalonia.Desktop.csproj

# Core project (fastest full compile + XAML validation)
dotnet build Broccoli.Avalonia/Broccoli.Avalonia/Broccoli.Avalonia.csproj

# Android
dotnet build Broccoli.Avalonia/Broccoli.Avalonia.Android/Broccoli.Avalonia.Android.csproj

# Unit tests
dotnet test Broccoli.Avalonia/Broccoli.Avalonia.UnitTests/Broccoli.Avalonia.UnitTests.csproj
```

## Key Files to Read First

- `Broccoli.Avalonia/App.axaml.cs` — composition root, DI, startup (DB migrate, sync kickoff)
- `Broccoli.Avalonia/ServiceCollectionExtensions.cs` — canonical service/view-model registration
- `Broccoli.Avalonia/ViewLocator.cs` — VM → View map (add new pages here)
- `Broccoli.Avalonia/Shell/MainView.axaml` — `DrawerPage` adaptive navigation shell
- `Broccoli.Avalonia/Shell/MainViewModel.cs` — nav items + selection state
- `Broccoli.Avalonia/Storage/BroccoliDbContext.cs` — SQLite schema
- `Broccoli.Avalonia/Storage/RecipeMarkdownStore.cs` — recipe persistence format
- `Broccoli.Avalonia/_Shared/IngredientParsing/FoodService.cs` — SQLite-backed food store + fuzzy matching thresholds
