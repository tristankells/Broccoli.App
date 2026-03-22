# Broccoli.App — Agent Guide

## Architecture Overview

Four-project solution targeting .NET 10:

| Project | Purpose |
|---|---|
| `Broccoli.App.Shared/` | **All** Blazor pages, components, models, and shared service interfaces/implementations |
| `Broccoli.App/` | .NET MAUI host (Windows/Android/iOS/Mac Catalyst) — thin shell, DI wiring only |
| `Broccoli.App.Web/` | ASP.NET Core Blazor Server host — thin shell, DI wiring only |
| `Broccoli.App.UnitTests/` + `Broccoli.App.IntegrationTests/` | MSTest + Moq test projects |

**All UI and business logic belongs in `Broccoli.App.Shared/`.** Both host projects reference it and register the same interfaces with platform-appropriate implementations.

## Vertical Slice Architecture

`Broccoli.App.Shared/` is organised by **feature slice**, not technical layer. Each slice owns its Razor pages, components, service interfaces, and implementations side-by-side.

```
Broccoli.App.Shared/
    Slices/
        Auth/           ← Login, AuthorizeView, IAuthenticationService, AuthenticationService, etc.
        AppSettings/    ← AppSettingsDialog, IThemeService, ThemeService
        Pantry/         ← Pantry.razor, IPantryService, PantryService
        GroceryList/    ← GroceryList.razor, AddIngredientsDialog, IGroceryListService, IngredientCartService
        Foods/          ← Foods.razor, UsdaSearchDialog, IUsdaFoodSearchService, UsdaFoodSearchService
        Recipes/        ← Recipes/RecipeDetail/RecipeReadOnly pages, MarkdownRenderer, IRecipeService, etc.
            Import/     ← ImportRecipesDialog, IImportFormat, RecipeImportService, PaprikaHtmlImportFormat
        MealPrep/       ← MealPrepPlans.razor, AddRecipesToPlanDialog, IMealPrepPlanService
        Nutrition/      ← DailyFoodPlanning/MacroTargets pages, IMacroTargetService, MacroCalculatorService, etc.
        Seasonality/    ← SeasonalityBadge, SeasonalityPanel, ISeasonalityService, SeasonHelper
    _Shared/
        Infrastructure/ ← ICosmosDbService, CosmosDbService
        Platform/       ← IFormFactor, ISecureStorageService  (implemented per-host, never in Shared)
        IngredientParsing/ ← IFoodService, IngredientParserService, LocalJsonFoodService,
                             ParsedIngredient, ParsedIngredientMatch, ParsedIngredientsTable
    Configuration/      ← Settings POCOs (unchanged)
    Models/             ← Domain model classes (unchanged, Broccoli.Data.Models namespace)
    Layout/             ← MainLayout, NavMenu, LoginLayout (unchanged)
    Data/               ← FoodDatabase.json, nz-produce.json (unchanged)
```

Each slice has a `XxxSliceExtensions.cs` with `AddXxxSlice(IServiceCollection)`. Both host projects call these instead of registering services inline.

## Namespace Convention

All slice and shared namespaces follow a consistent pattern:

| Location | Namespace |
|---|---|
| `Slices/Auth/` | `Broccoli.App.Shared.Slices.Auth` |
| `Slices/Recipes/` | `Broccoli.App.Shared.Slices.Recipes` |
| `Slices/Recipes/Import/` | `Broccoli.App.Shared.Slices.Recipes.Import` |
| *(all other slices follow the same pattern)* | `Broccoli.App.Shared.Slices.<SliceName>` |
| `_Shared/Infrastructure/` | `Broccoli.App.Shared.Infrastructure` |
| `_Shared/Platform/` | `Broccoli.App.Shared.Platform` |
| `_Shared/IngredientParsing/` | `Broccoli.App.Shared.IngredientParsing` |
| `Models/` | `Broccoli.Data.Models` *(preserved as-is)* |

All slice namespaces are imported globally in `_Imports.razor` — individual Razor pages do not need per-file `@using` directives for slice types.

## Platform Abstraction Pattern

Two interfaces must be implemented separately per host project — never add platform APIs directly to Shared:

- `IFormFactor` → `Broccoli.App/Services/FormFactor.cs` (uses `DeviceInfo`) and `Broccoli.App.Web/Services/FormFactor.cs` (returns `"Web"`)
- `ISecureStorageService` → similarly split; used by `AuthenticationStateService` to persist auth state

Both interfaces live in `_Shared/Platform/` (`Broccoli.App.Shared.Platform` namespace).

## Key Service Flows

**Authentication guard:** `Routes.razor` calls `AuthStateService.InitializeAsync()` on startup and redirects to `/login` if unauthenticated. All Cosmos services call `EnsureAuthenticated()` which throws if no user is logged in.

**Ingredient parsing pipeline:** `IngredientParserService.ParseAndMatchIngredientsAsync()` → regex parse → `LocalJsonFoodService.FindBestMatch()` (exact match → stopword-stripped exact → token-set ratio ≥ 0.7 → FuzzySharp WRatio ≥ 0.6). The food database is loaded once into `Dictionary<string, Food>` at startup. Register via `services.AddIngredientParsing(foodDatabasePath)`.

**Data files:**
- `Broccoli.App.Shared/Data/FoodDatabase.json` — linked as `<Content>` into both host projects; resolved via filesystem path at startup (see `MauiProgram.cs`/`Program.cs` path-fallback logic)
- `Broccoli.App.Shared/Data/nz-produce.json` — embedded resource in Shared, loaded by `LocalJsonSeasonalityService`

## CosmosDB

Database `BroccoliAppDb` with **shared 600 RU/s throughput** across all containers. Containers: `Users` (partitionKey `/partitionKey`), `Recipes` (`/userId`), `GroceryListItems` (`/partitionKey`="user"), plus Pantry, MacroTargets, MealPrepPlans, DailyFoodPlans. Every Cosmos service calls `InitializeAsync()` at startup (Web does it in `Program.cs`; MAUI fires `Task.Run` in background).

Default `appsettings.json` points to the CosmosDB **local emulator** with its well-known dev key. Production secrets go in `appsettings.Production.json`.

## Feature Flags

`FeatureFlagsSettings` (config section `"FeatureFlags"`) controls:
- `FoodDatabaseEditing` — enables inline CRUD and USDA import on the Foods page. Set to `true` in `appsettings.Development.json` (Web only). **MAUI hardcodes this to `false`** via `AddFoodsSlice(new FeatureFlagsSettings { FoodDatabaseEditing = false })`.
- When `FoodDatabaseEditing = true`, the Web host registers `IUsdaFoodSearchService` via `AddHttpClient` directly in `Program.cs` (not in the slice extension, because `AddHttpClient` is not available in the Shared project).

## Razor Component Pattern

Pages use a three-file code-behind pattern: `Foo.razor` + `Foo.razor.cs` (partial class) + `Foo.razor.css`. Inject services via `[Inject]` attributes in the `.cs` file, not `@inject` in markup. The `.razor.cs` namespace must match the slice folder (e.g. `Broccoli.App.Shared.Slices.Recipes`).

## Adding a New Slice

1. Create `Slices/MyFeature/` directory
2. Add pages, components, service interfaces, and implementations — all in `namespace Broccoli.App.Shared.Slices.MyFeature`
3. Create `MyFeatureSliceExtensions.cs` with `AddMyFeatureSlice(IServiceCollection)`
4. Call `services.AddMyFeatureSlice()` in both `Program.cs` and `MauiProgram.cs`
5. Add `@using Broccoli.App.Shared.Slices.MyFeature` to `_Imports.razor`

## Build & Run Commands

```bash
# Web app (development)
dotnet run --project Broccoli.App.Web

# MAUI Windows
dotnet build Broccoli.App/Broccoli.App.csproj -t:Run -f net10.0-windows10.0.19041.0

# All tests
dotnet test

# Unit tests only (no CosmosDB required)
dotnet test Broccoli.App.UnitTests/Broccoli.App.UnitTests.csproj

# Integration tests (requires CosmosDB emulator running)
dotnet test Broccoli.App.IntegrationTests/Broccoli.App.IntegrationTests.csproj
```

## Key Files to Read First

- `Broccoli.App.Shared/_Shared/IngredientParsing/IngredientParserService.cs` — core parsing regex and unit normalization map
- `Broccoli.App.Shared/_Shared/IngredientParsing/LocalJsonFoodService.cs` — multi-stage fuzzy matching thresholds
- `Broccoli.App.Web/Program.cs` — canonical DI registration order using slice extensions (MAUI mirrors this in `MauiProgram.cs`)
- `Broccoli.App.Shared/Routes.razor` — authentication gate for all routes
- `Broccoli.App.Shared/Configuration/` — all settings POCOs with their `SectionName` constants
- `Broccoli.App.Shared/Slices/*/XxxSliceExtensions.cs` — per-slice DI wiring
