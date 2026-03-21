# Feature Plan: Food Database Editing

## Overview

Add inline CRUD editing and USDA FoodData Central (FDC) search/import to the Food Database page (`Foods.razor`).  
All editing features are gated behind a `FoodDatabaseEditing` feature flag that is only active in **Web Development** and **MAUI DEBUG** builds.  
All writes persist back to `FoodDatabase.json`.

---

## Scope

| Capability | Notes |
|---|---|
| Inline Add row | New row form at the bottom of the table |
| Inline Edit row | Per-row edit mode, all fields editable |
| Inline Delete row | Per-row delete with confirmation |
| USDA search dialog | Search FDC API → results table → import selected rows |
| Feature flag | `FeatureFlags:FoodDatabaseEditing` — on in Dev/DEBUG only |
| Persist to disk | All changes written to `FoodDatabase.json` |

---

## Data Model Changes

### `Food.cs` — Add 4 properties

The following fields already exist in `FoodDatabase.json` but are missing from the C# model. They will be added:

| Property | JSON Key | Unit |
|---|---|---|
| `SaturatedFatPer100g` | `SaturatedFatPer100g` | g per 100 g |
| `DietaryFiberPer100g` | `DietaryFiberPer100g` | g per 100 g |
| `SugarsPer100g` | `SugarsPer100g` | g per 100 g |
| `SodiumMgPer100g` | `SodiumMgPer100g` | mg per 100 g |

Final full property list on `Food`:  
`Id`, `Name`, `Measure`, `GramsPerMeasure`, `Notes`, `CaloriesPer100g`, `FatPer100g`, `SaturatedFatPer100g`, `CarbohydratesPer100g`, `DietaryFiberPer100g`, `SugarsPer100g`, `ProteinPer100g`, `SodiumMgPer100g`

### `FoodDatabase.json` — Already updated ✅

The following 6 fields have been removed from all 49 existing entries as part of this plan:  
`TransFatPer100g`, `CholesterolMgPer100g`, `VitaminAMcgPer100g`, `VitaminCMgPer100g`, `CalciumMgPer100g`, `IronMgPer100g`

---

## Feature Flag

### Config class — `FeatureFlagsSettings.cs`

**Location:** `Broccoli.App.Shared/Configuration/FeatureFlagsSettings.cs`

```csharp
public class FeatureFlagsSettings
{
    public const string SectionName = "FeatureFlags";
    public bool FoodDatabaseEditing { get; set; } = false;
}
```

### appsettings entries

Add to **both** files:

- `Broccoli.App.Web/appsettings.Development.json`
- `Broccoli.App/appsettings.Development.json` ← loaded in `#if DEBUG` in `MauiProgram.cs`

```json
"FeatureFlags": {
  "FoodDatabaseEditing": true
}
```

### Registration

**`Broccoli.App.Web/Program.cs`** and **`Broccoli.App/MauiProgram.cs`** — bind and register as singleton:

```csharp
var featureFlags = new FeatureFlagsSettings();
builder.Configuration.GetSection(FeatureFlagsSettings.SectionName).Bind(featureFlags);
builder.Services.AddSingleton(featureFlags);
```

---

## USDA Food Search Service

### Config class — `UsdaSettings.cs`

**Location:** `Broccoli.App.Shared/Configuration/UsdaSettings.cs`

```csharp
public class UsdaSettings
{
    public const string SectionName = "Usda";
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.nal.usda.gov/fdc/v1";
}
```

Add to **both** `appsettings.Development.json` files:

```json
"Usda": {
  "ApiKey": "YOUR_KEY_HERE"
}
```

### Nutrient ID mapping

The API call requests only the 8 nutrient IDs relevant to the `Food` model, keeping response payloads small:

| USDA Nutrient ID | Nutrient Name | Maps To |
|---|---|---|
| 1008 | Energy | `CaloriesPer100g` (kcal) |
| 1004 | Total lipid (fat) | `FatPer100g` (g) |
| 1003 | Protein | `ProteinPer100g` (g) |
| 1005 | Carbohydrate, by difference | `CarbohydratesPer100g` (g) |
| 1258 | Fatty acids, total saturated | `SaturatedFatPer100g` (g) |
| 1079 | Fiber, total dietary | `DietaryFiberPer100g` (g) |
| 1063 | Sugars, Total | `SugarsPer100g` (g) |
| 1093 | Sodium, Na | `SodiumMgPer100g` (mg) |

### Example request

```
GET https://api.nal.usda.gov/fdc/v1/foods/search
  ?query=cheddar cheese
  &dataType=Foundation,SR%20Legacy
  &pageSize=10
  &pageNumber=1
  &nutrients=1008&nutrients=1004&nutrients=1003&nutrients=1005
  &nutrients=1258&nutrients=1079&nutrients=1063&nutrients=1093
  &api_key={key}
```

`dataType=Foundation,SR Legacy` is used so searches return results even for foods that aren't in the smaller Foundation dataset.

### Interface — `IUsdaFoodSearchService.cs`

**Location:** `Broccoli.App.Shared/Services/IUsdaFoodSearchService.cs`

```csharp
public interface IUsdaFoodSearchService
{
    Task<UsdaSearchResult> SearchAsync(string query, int page = 1, int pageSize = 10);
}

public class UsdaSearchResult
{
    public int TotalHits { get; set; }
    public int TotalPages { get; set; }
    public int CurrentPage { get; set; }
    public List<UsdaFoodItem> Foods { get; set; } = new();
}

public class UsdaFoodItem
{
    public int FdcId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    // Mapped nutrient values (all per 100 g)
    public double Calories { get; set; }
    public double Fat { get; set; }
    public double SaturatedFat { get; set; }
    public double Carbohydrates { get; set; }
    public double DietaryFiber { get; set; }
    public double Sugars { get; set; }
    public double Protein { get; set; }
    public double SodiumMg { get; set; }
}
```

### Implementation — `UsdaFoodSearchService.cs`

**Location:** `Broccoli.App.Shared/Services/UsdaFoodSearchService.cs`

- Injects `HttpClient` and `UsdaSettings`
- Builds the query string with the 8 nutrient IDs
- Deserialises the `foods[].foodNutrients` array, looks up each nutrient by `nutrientId`, reads its `value`
- Returns a mapped `UsdaSearchResult` (no raw USDA types exposed outside the service)

### Registration

**`Program.cs`** and **`MauiProgram.cs`**:

```csharp
var usdaSettings = new UsdaSettings();
builder.Configuration.GetSection(UsdaSettings.SectionName).Bind(usdaSettings);
builder.Services.AddSingleton(usdaSettings);

builder.Services.AddHttpClient<UsdaFoodSearchService>(client =>
    client.BaseAddress = new Uri(usdaSettings.BaseUrl));
builder.Services.AddSingleton<IUsdaFoodSearchService, UsdaFoodSearchService>();
```

---

## IFoodService Write Methods

### Interface changes — `IFoodService.cs`

Add to `Broccoli.App.Shared/Services/IngredientParsing/IFoodService.cs`:

```csharp
Task<Food> AddAsync(Food food);
Task UpdateAsync(Food food);
Task DeleteAsync(int id);
```

### Implementation — `LocalJsonFoodService.cs`

- Add a `SemaphoreSlim _writeLock = new(1, 1)` field and store `_databasePath` on construction
- `AddAsync`: assigns the next available `Id` (max existing + 1), adds to `_foodByName`, calls `PersistAsync()`
- `UpdateAsync`: replaces the entry in `_foodByName` (keyed on name; handles rename by removing old key), calls `PersistAsync()`
- `DeleteAsync`: removes by id from `_foodByName`, calls `PersistAsync()`
- `PersistAsync()`: acquires `_writeLock`, serialises `_foodByName.Values` to JSON (`WriteIndented = true`), writes to `_databasePath`

---

## USDA Search Dialog Component

**Location:** `Broccoli.App.Shared/Components/UsdaSearchDialog.razor` (+ `.cs` + `.css`)

Follows the existing modal pattern used by `AddIngredientsDialog.razor`:

```
modal-backdrop  ←  click to close
modal-dialog-container
  modal-header-bar  [🔍 Search USDA FoodData Central]  [×]
  modal-body-scroll
    search-bar
      <input placeholder="Search foods…" />  <button>Search</button>
    results-table (scrollable)
      columns: ☐ | Description | Type | Cal | Fat | Sat.Fat | Carbs | Fiber | Sugars | Protein | Sodium
      one row per UsdaFoodItem, checkbox per row
    pagination-bar  [← Prev]  Page X of Y  [Next →]
  modal-footer
    [Cancel]  [Import Selected (N)]
```

**Parameters:**

| Parameter | Type | Description |
|---|---|---|
| `IsVisible` | `bool` | Show/hide the dialog |
| `OnImport` | `EventCallback<List<Food>>` | Fires with mapped `Food` objects on confirm |
| `OnCancel` | `EventCallback` | Fires when dialog is dismissed |

**Import mapping defaults:**

| Food field | Default value |
|---|---|
| `Id` | 0 (assigned by `AddAsync`) |
| `Name` | USDA `description` |
| `Measure` | `"100g"` |
| `GramsPerMeasure` | `100.0` |
| `Notes` | `"Imported from USDA FDC (fdcId: {id})"` |
| All nutrient fields | From `UsdaFoodItem` values |

---

## Foods Page Changes

**Files:** `Broccoli.App.Shared/Pages/Foods.razor` + `Foods.razor.cs` + `Foods.razor.css`

### Injections added

```csharp
@inject FeatureFlagsSettings FeatureFlags
@inject IUsdaFoodSearchService UsdaService  // only used when flag is on
```

### UI additions (only rendered when `FeatureFlags.FoodDatabaseEditing == true`)

**Table header row** — extra `Actions` column appended.

**Each data row** — two icon buttons in the Actions column:
- ✏️ **Edit** — switches that row into inline edit mode (all cells become `<input>` fields); a ✅ Save and ✕ Cancel button appear
- 🗑️ **Delete** — shows an inline `"Delete [Name]? [Confirm] [Cancel]"` prompt, then calls `IFoodService.DeleteAsync`

**Table footer** — an **Add Row** form:
- Inline inputs for all 13 editable fields (Name, Measure, GramsPerMeasure, Notes, and the 8 nutrient fields)
- ➕ **Add** button calls `IFoodService.AddAsync`

**Page action bar** (above table):
- 🔍 **Search USDA** button — sets `_usdaDialogVisible = true`
- `<UsdaSearchDialog>` component wired to `OnImport` → loops `IFoodService.AddAsync` for each selected food

### Code-behind additions (`Foods.razor.cs`)

```csharp
// Edit state
private Food? _editingFood;         // clone of the row being edited
private bool _usdaDialogVisible;
private Food _newFood = new();      // bound to the Add Row form

private async Task SaveEdit() { ... }    // calls IFoodService.UpdateAsync
private async Task DeleteFood(Food f) { ... }
private async Task AddFood() { ... }     // calls IFoodService.AddAsync, resets _newFood
private async Task OnUsdaImport(List<Food> foods) { ... }
```

---

## Files Summary

### New files

| File | Purpose |
|---|---|
| `Broccoli.App.Shared/Configuration/FeatureFlagsSettings.cs` | Feature flag config class |
| `Broccoli.App.Shared/Configuration/UsdaSettings.cs` | USDA API config class |
| `Broccoli.App.Shared/Services/IUsdaFoodSearchService.cs` | Interface + DTOs |
| `Broccoli.App.Shared/Services/UsdaFoodSearchService.cs` | Implementation |
| `Broccoli.App.Shared/Components/UsdaSearchDialog.razor` | Dialog markup |
| `Broccoli.App.Shared/Components/UsdaSearchDialog.razor.cs` | Dialog code-behind |
| `Broccoli.App.Shared/Components/UsdaSearchDialog.razor.css` | Dialog styles |

### Modified files

| File | Change |
|---|---|
| `Broccoli.App.Shared/Models/Food.cs` | Add 4 properties |
| `Broccoli.App.Shared/Services/IngredientParsing/IFoodService.cs` | Add 3 write method signatures |
| `Broccoli.App.Shared/Services/IngredientParsing/LocalJsonFoodService.cs` | Implement write methods + file persistence |
| `Broccoli.App.Shared/Data/FoodDatabase.json` | ✅ Already cleaned (6 fields removed) |
| `Broccoli.App.Shared/Pages/Foods.razor` | Add CRUD UI + USDA dialog |
| `Broccoli.App.Shared/Pages/Foods.razor.cs` | Add CRUD + dialog logic |
| `Broccoli.App.Shared/Pages/Foods.razor.css` | Add edit/add row styles |
| `Broccoli.App.Web/appsettings.Development.json` | Add `FeatureFlags` + `Usda` sections |
| `Broccoli.App/appsettings.Development.json` | Add `FeatureFlags` + `Usda` sections |
| `Broccoli.App.Web/Program.cs` | Register `FeatureFlagsSettings`, `UsdaSettings`, `IUsdaFoodSearchService` |
| `Broccoli.App/MauiProgram.cs` | Register `FeatureFlagsSettings`, `UsdaSettings`, `IUsdaFoodSearchService` |

---

## Implementation Considerations

1. **MAUI write-back path** — In development, `MauiProgram.cs` resolves `foodDatabasePath` to the source file on disk via a relative `../Ginger.Data/Data/FoodDatabase.json` path. The `PersistAsync` write-back in `LocalJsonFoodService` will write to whichever path was resolved at startup. Confirm this path is correct before testing on MAUI.

2. **USDA per-100g** — All USDA FDC nutrient values in the search response are already expressed per 100 g. No conversion is needed.

3. **Singleton concurrency** — `LocalJsonFoodService` is registered as a singleton. The `SemaphoreSlim` write lock ensures file writes don't race. Reads (`GetAllAsync`) do not need locking since `Dictionary` reads are safe with concurrent non-mutating readers.

4. **Name-keyed dictionary on rename** — When editing a food's `Name`, `UpdateAsync` must remove the old key and add the new one before persisting, to keep the in-memory dictionary consistent.

