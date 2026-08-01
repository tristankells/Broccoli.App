# Feature: Meal Macro Comparison on Recipe Detail Page

## Overview

Add an optional panel to the Recipe Detail page that compares the recipe's per-serving macros against the per-meal targets (÷3) of a chosen person from the Macro Targets page. The panel is opt-in via a settings dialog, and deviation from the ideal meal is colour-coded for quick visual feedback.

---

## User Story

> As a user editing a recipe, I want to see how the recipe's per-serving macros compare to my ideal meal macros, so I can adjust serving sizes to hit my targets.

---

## Behaviour

### Settings Dialog (⚙️ button on Recipe Detail header)

| Control | Description |
|---|---|
| Toggle | Enable / disable the meal comparison panel |
| Person dropdown | Choose whose macro profile to base the meal targets on |

- The dropdown is populated from all `MacroTarget` rows belonging to the current user.
- If the user has no macro profiles yet, a friendly message is shown: *"No macro profiles found — add one on the Macro Targets page."*
- Settings are saved per-user to the existing `MacroTargetSettings` Cosmos document (two new fields, backward-compatible).

### Comparison Panel (below `ParsedIngredientsTable`)

Visible when:
- The toggle is **on**, AND
- The recipe has a **Servings** value > 0 (otherwise shows *"Set a serving count to enable comparison"*), AND
- The recipe has ingredients entered.

For each macro (Calories, Protein, Carbs, Fat):

| Column | Content |
|---|---|
| Macro name | e.g. "Protein" |
| Target (per meal) | `RecommendedProteinG / 3` from the chosen profile |
| Actual (per serving) | Recipe total ÷ servings |
| Δ | Absolute difference |
| % off | `|actual - target| / target × 100` |
| Badge | Colour-coded deviation indicator |

### Colour-Coding Rules

| Deviation | Colour | CSS Class |
|---|---|---|
| ≤ 15% | 🟢 Green | `macro-ok` |
| ≤ 25% | 🟡 Yellow/Amber | `macro-warn` |
| > 25% | 🔴 Red | `macro-over` |

Deviation is **absolute** (both over and under target are treated equally), because the goal is to match the meal size, not avoid excess.

---

## Architecture

### Settings Persistence

Extend `MacroTargetSettings` (in `Broccoli.Data.Models`) with two new JSON-serialised properties. The document already lives in the `MacroTargets` Cosmos container scoped per-user, so no new container is needed.

```csharp
// MacroTargetSettings.cs — two new fields
[JsonPropertyName("recipeMealComparisonEnabled")]
public bool RecipeMealComparisonEnabled { get; set; } = false;

[JsonPropertyName("recipeMealComparisonPersonId")]
public string RecipeMealComparisonPersonId { get; set; } = string.Empty;
```

`IMacroTargetService.GetSettingsAsync` / `SaveSettingsAsync` require no changes — the new fields are carried automatically.

### Nutrition Totals Source

`RecipeDetail.razor.cs` already calls `IngredientParserService.ParseAndMatchIngredientsAsync()` inside `ScoreSeasonalityAsync()`. That same `List<ParsedIngredientMatch>` is used to sum per-serving nutrition totals without any extra Cosmos or HTTP calls:

```
perServingCalories  = matches.Sum(m => m.GetCalories())  / servings
perServingProteinG  = matches.Sum(m => m.GetProtein())   / servings
perServingCarbsG    = matches.Sum(m => m.GetCarbohydrates()) / servings
perServingFatG      = matches.Sum(m => m.GetFat())       / servings
```

These four `double` fields are stored on the code-behind and passed as parameters to `MealMacroComparison`.

---

## Files Changed / Created

### 1. `Broccoli.App.Shared/Models/MacroTargetSettings.cs` *(modified)*

Add two new properties before `UpdatedAt`:

```csharp
[JsonPropertyName("recipeMealComparisonEnabled")]
public bool RecipeMealComparisonEnabled { get; set; } = false;

[JsonPropertyName("recipeMealComparisonPersonId")]
public string RecipeMealComparisonPersonId { get; set; } = string.Empty;
```

No other model or service changes needed.

---

### 2. `Broccoli.App.Shared/Slices/Recipes/RecipeDetailSettingsDialog.razor` *(new)*

A modal dialog following the same structural pattern as `MacroTargetSettingsDialog.razor`.

**Parameters:**

| Parameter | Type | Description |
|---|---|---|
| `IsVisible` | `bool` | Controls visibility |
| `Settings` | `MacroTargetSettings` | Current settings (deep-copied to a draft) |
| `AvailableTargets` | `List<MacroTarget>` | Populated from `IMacroTargetService.GetAllAsync` |
| `OnSave` | `EventCallback<MacroTargetSettings>` | Fired on Save |
| `OnCancel` | `EventCallback` | Fired on Cancel / backdrop click |

**Body sections:**

1. **Meal Comparison** toggle (on/off buttons, same style as Unit System toggle in macro settings)
2. **Compare Against** person `<select>` — enabled only when toggle is on; lists `MacroTarget.Name` values; shows empty-state message if `AvailableTargets` is empty.

---

### 3. `Broccoli.App.Shared/Slices/Recipes/MealMacroComparison.razor` *(new)*

A display-only component showing the four-row comparison table.

**Parameters:**

| Parameter | Type | Description |
|---|---|---|
| `PerServingCalories` | `double` | Calories per serving from parsed ingredients |
| `PerServingProteinG` | `double` | Protein (g) per serving |
| `PerServingCarbsG` | `double` | Carbs (g) per serving |
| `PerServingFatG` | `double` | Fat (g) per serving |
| `Target` | `MacroTarget` | The chosen macro profile |

**Internal logic:**

```csharp
private double MealCalories  => Target.RecommendedCalories  / 3.0;
private double MealProteinG  => Target.RecommendedProteinG  / 3.0;
private double MealCarbsG    => Target.RecommendedCarbsG    / 3.0;
private double MealFatG      => Target.RecommendedFatG      / 3.0;

private static string DeviationClass(double actual, double target)
{
    if (target <= 0) return string.Empty;
    var pct = Math.Abs(actual - target) / target * 100.0;
    return pct <= 15 ? "macro-ok" : pct <= 25 ? "macro-warn" : "macro-over";
}
```

Renders a compact four-row card (Calories / Protein / Carbs / Fat) showing target, actual, Δ, and % deviation badge.

---

### 4. `Broccoli.App.Shared/Slices/Recipes/MealMacroComparison.razor.css` *(new)*

Scoped styles:

```css
.macro-ok   { color: #2e7d32; font-weight: 600; }   /* green  — ≤15%  */
.macro-warn { color: #f57c00; font-weight: 600; }   /* amber  — ≤25%  */
.macro-over { color: #c62828; font-weight: 600; }   /* red    — >25%  */
```

Plus card/table layout styles consistent with the existing `ParsedIngredientsTable` aesthetic.

---

### 5. `Broccoli.App.Shared/Slices/Recipes/RecipeDetail.razor.cs` *(modified)*

**New injections:**

```csharp
[Inject] IMacroTargetService MacroTargetService { get; set; } = default!;
[Inject] IAuthenticationStateService AuthStateService { get; set; } = default!;
```

**New state fields:**

```csharp
private MacroTargetSettings _recipeDetailSettings = new();
private List<MacroTarget> _allTargets = new();
private bool _recipeSettingsDialogOpen;

private double _perServingCalories;
private double _perServingProteinG;
private double _perServingCarbsG;
private double _perServingFatG;
```

**`OnInitializedAsync` additions** (after existing recipe load):

```csharp
var userId = AuthStateService.CurrentUserId ?? string.Empty;
_recipeDetailSettings = await MacroTargetService.GetSettingsAsync(userId);
_allTargets = await MacroTargetService.GetAllAsync(userId);
```

**`ScoreSeasonalityAsync` additions** — after `ParseAndMatchIngredientsAsync`, compute totals:

```csharp
var servings = recipe?.Servings ?? 0;
if (servings > 0)
{
    _perServingCalories = matches.Sum(m => m.GetCalories())         / servings;
    _perServingProteinG = matches.Sum(m => m.GetProtein())          / servings;
    _perServingCarbsG   = matches.Sum(m => m.GetCarbohydrates())    / servings;
    _perServingFatG     = matches.Sum(m => m.GetFat())              / servings;
}
```

**New handler:**

```csharp
private async Task OnRecipeSettingsSaved(MacroTargetSettings updated)
{
    _recipeSettingsDialogOpen = false;
    _recipeDetailSettings = await MacroTargetService.SaveSettingsAsync(updated);
}
```

---

### 6. `Broccoli.App.Shared/Slices/Recipes/RecipeDetail.razor` *(modified)*

**Header** — add settings button beside the autosave indicator / View Recipe button:

```razor
<button class="btn btn-outline" @onclick="() => _recipeSettingsDialogOpen = true">⚙️ Settings</button>
```

**Below `<ParsedIngredientsTable>`** — conditionally render the comparison panel:

```razor
@if (_recipeDetailSettings.RecipeMealComparisonEnabled)
{
    @{
        var chosenTarget = _allTargets.FirstOrDefault(
            t => t.Id == _recipeDetailSettings.RecipeMealComparisonPersonId);
    }
    @if (chosenTarget is not null && (recipe.Servings ?? 0) > 0)
    {
        <MealMacroComparison
            PerServingCalories="@_perServingCalories"
            PerServingProteinG="@_perServingProteinG"
            PerServingCarbsG="@_perServingCarbsG"
            PerServingFatG="@_perServingFatG"
            Target="@chosenTarget" />
    }
    else if ((recipe.Servings ?? 0) <= 0)
    {
        <div class="meal-comparison-hint">Set a serving count to enable meal comparison.</div>
    }
}
```

**Before closing `</AuthorizeView>`** — add the dialog:

```razor
<RecipeDetailSettingsDialog
    IsVisible="@_recipeSettingsDialogOpen"
    Settings="@_recipeDetailSettings"
    AvailableTargets="@_allTargets"
    OnSave="OnRecipeSettingsSaved"
    OnCancel="() => _recipeSettingsDialogOpen = false" />
```

---

### 7. `Broccoli.App.Shared/Slices/Recipes/RecipeDetail.razor.css` *(modified)*

Add styling for the settings button and the comparison hint message, consistent with existing page chrome.

---

## Edge Cases

| Scenario | Handling |
|---|---|
| User has no macro profiles | Dialog shows info message; comparison panel is hidden |
| Servings not set (null / 0) | Panel shows "Set a serving count" hint instead of comparison |
| Selected person deleted from Macro Targets page | `FirstOrDefault` returns null; panel hides gracefully |
| Ingredients not yet parsed | `_perServing*` fields remain 0; comparison still renders with zeros (indicates all macros are missing) |
| New recipe (not yet saved) | Settings can still be opened; comparison works as ingredients are typed |

---

## Out of Scope

- Adjusting the number of meals per day (currently hardcoded to ÷3). Could be a future setting.
- Showing comparison on the **RecipeReadOnly** page (read-only view; no editing context).
- Per-ingredient target breakdown.

