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

## Namespace Irregularity

The codebase uses three root namespaces — always check before adding `using` statements:
- `Broccoli.Data.Models` — all model classes (`Recipe`, `Food`, `PantryItem`, etc.)
- `Broccoli.App.Shared.Services` — service interfaces and most implementations
- `Broccoli.Shared.Services` — CosmosDB service implementations (`CosmosRecipeService`, `GroceryListService`, etc.)
- `Broccoli.App.Shared.Services.IngredientParsing` — `IngredientParserService`, `LocalJsonFoodService`, `IFoodService`

## Platform Abstraction Pattern

Two interfaces must be implemented separately per host project — never add platform APIs directly to Shared:

- `IFormFactor` → `Broccoli.App/Services/FormFactor.cs` (uses `DeviceInfo`) and `Broccoli.App.Web/Services/FormFactor.cs` (returns `"Web"`)
- `ISecureStorageService` → similarly split; used by `AuthenticationStateService` to persist auth state

## Key Service Flows

**Authentication guard:** `Routes.razor` calls `AuthStateService.InitializeAsync()` on startup and redirects to `/login` if unauthenticated. All Cosmos services call `EnsureAuthenticated()` which throws if no user is logged in.

**Ingredient parsing pipeline:** `IngredientParserService.ParseAndMatchIngredientsAsync()` → regex parse → `LocalJsonFoodService.FindBestMatch()` (exact match → stopword-stripped exact → token-set ratio ≥ 0.7 → FuzzySharp WRatio ≥ 0.6). The food database is loaded once into `Dictionary<string, Food>` at startup.

**Data files:**
- `Broccoli.App.Shared/Data/FoodDatabase.json` — linked as `<Content>` into both host projects; resolved via filesystem path at startup (see `MauiProgram.cs`/`Program.cs` path-fallback logic)
- `Broccoli.App.Shared/Data/nz-produce.json` — embedded resource in Shared, loaded by `LocalJsonSeasonalityService`

## CosmosDB

Database `BroccoliAppDb` with **shared 600 RU/s throughput** across all containers. Containers: `Users` (partitionKey `/partitionKey`), `Recipes` (`/userId`), `GroceryListItems` (`/partitionKey`="user"), plus Pantry, MacroTargets, MealPrepPlans, DailyFoodPlans. Every Cosmos service calls `InitializeAsync()` at startup (Web does it in `Program.cs`; MAUI fires `Task.Run` in background).

Default `appsettings.json` points to the CosmosDB **local emulator** with its well-known dev key. Production secrets go in `appsettings.Production.json`.

## Feature Flags

`FeatureFlagsSettings` (config section `"FeatureFlags"`) controls:
- `FoodDatabaseEditing` — enables inline CRUD and USDA import on the Foods page. Set to `true` in `appsettings.Development.json` (Web only). **MAUI hardcodes this to `false`** and does not register `IUsdaFoodSearchService`.

## Razor Component Pattern

Pages use a three-file code-behind pattern: `Foo.razor` + `Foo.razor.cs` (partial class) + `Foo.razor.css`. Inject services via `[Inject]` attributes in the `.cs` file, not `@inject` in markup.

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

- `Broccoli.App.Shared/Services/IngredientParsing/IngredientParserService.cs` — core parsing regex and unit normalization map
- `Broccoli.App.Shared/Services/IngredientParsing/LocalJsonFoodService.cs` — multi-stage fuzzy matching thresholds
- `Broccoli.App.Web/Program.cs` — canonical DI registration order (MAUI mirrors this in `MauiProgram.cs`)
- `Broccoli.App.Shared/Routes.razor` — authentication gate for all routes
- `Broccoli.App.Shared/Configuration/` — all settings POCOs with their `SectionName` constants

