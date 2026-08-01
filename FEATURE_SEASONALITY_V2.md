# Implementation Plan: Ingredient Seasonality Scoring (v2)

> **Current season at time of writing:** Autumn (March 2026 — NZ Southern Hemisphere calendar)

---

## Overview

This feature adds a seasonality score (0–100) to each recipe, computed at render time from a
bundled NZ produce dataset. The score is a pure computation: given a list of already-parsed
ingredient matches and the current date, return a weighted score and a per-ingredient breakdown.
No network calls. No caching concerns.

---

## Codebase Context

| Concern | File |
|---|---|
| Ingredient parsing + fuzzy matching | `Broccoli.App.Shared/Services/IngredientParsing/IngredientParserService.cs` |
| Food DB lookup (multi-stage fuzzy) | `Broccoli.App.Shared/Services/IngredientParsing/LocalJsonFoodService.cs` |
| Per-ingredient grams already computed | `ParsedIngredientMatch.GetWeightInGrams()` |
| Static food data | `Broccoli.App.Shared/Data/FoodDatabase.json` |
| Nutrition table component (pattern to follow) | `Broccoli.App.Shared/Components/ParsedIngredientsTable.razor` |
| Recipe list page + cards | `Broccoli.App.Shared/Pages/Recipes.razor` + `Recipes.razor.cs` |
| Recipe read-only view | `Broccoli.App.Shared/Pages/RecipeReadOnly.razor` + `RecipeReadOnly.razor.cs` |
| Service registration — web | `Broccoli.App.Web/Program.cs` |
| Service registration — MAUI | `Broccoli.App/MauiProgram.cs` |
| Unit tests | `Broccoli.App.UnitTests/Services/` |

---

## Step 1 — Bundle the Dataset

**New file:** `Broccoli.App.Shared/Data/nz-produce.json`

Copy the provided `nz-produce.json` into `Broccoli.App.Shared/Data/` alongside the existing
`FoodDatabase.json`.

Mark it as an **embedded resource** in `Broccoli.App.Shared/Broccoli.App.Shared.csproj`:

```xml
<ItemGroup>
  <EmbeddedResource Include="Data\nz-produce.json" />
</ItemGroup>
```

Using an embedded resource (rather than a filesystem path) means the same loading code works
identically in both the web project and MAUI — no per-platform path-resolution logic needed.
This follows the same pattern as how `appsettings.json` is loaded via
`Assembly.GetManifestResourceStream(...)` in `MauiProgram.cs`.

**Notes about the actual dataset file:**

- The root JSON object is a wrapper: `{ "meta": { ... }, "produce": [ ... ] }` — not a flat
  array. Deserialisation must unwrap the `produce` property.
- 74 items total: 33 fruits, 41 vegetables.
- Several items carry extra optional fields (`peak_seasons`, `notes`) not in the scoring spec.
  The model should include them as nullable fields so the data is preserved even if the scoring
  logic ignores them.
- `leek` and `swede` are `year_round: true` but their notes flag seasonal gaps. These receive
  scarcity weight 0.25 (lowest impact). Revisit by setting `year_round: false` if the gap
  months feel under-penalised after testing.
- `rhubarb` is `"type": "vegetable"` (NZ produce guide convention). The app's tag system is
  independent, so no action needed.
- `silverbeet` is the canonical spelling. The existing fuzzy matcher in `LocalJsonFoodService`
  will handle the `silver beet` variant before seasonality scoring runs.

---

## Step 2 — Data Models

### `Broccoli.App.Shared/Models/ProduceItem.cs` *(new)*

```csharp
using System.Text.Json.Serialization;

namespace Broccoli.Data.Models;

public class ProduceItem
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>"fruit" or "vegetable"</summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Seasons this ingredient is in season: "spring", "summer", "autumn", "winter".
    /// </summary>
    [JsonPropertyName("seasons")]
    public List<string> Seasons { get; set; } = new();

    /// <summary>
    /// When true the ingredient is always InSeason and receives scarcity weight 0.25.
    /// </summary>
    [JsonPropertyName("year_round")]
    public bool YearRound { get; set; }

    // Optional fields present in the dataset — retained for future use.
    [JsonPropertyName("peak_seasons")]
    public List<string>? PeakSeasons { get; set; }

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }
}
```

The dataset JSON has a wrapper object, so also add a private DTO for deserialisation inside the
service (see Step 5):

```csharp
// Used only during JSON loading — not exposed publicly.
internal class ProduceDataset
{
    [JsonPropertyName("produce")]
    public List<ProduceItem> Produce { get; set; } = new();
}
```

### `Broccoli.App.Shared/Models/SeasonalityResult.cs` *(new)*

```csharp
namespace Broccoli.Data.Models;

public enum SeasonalityLabel
{
    PeakSeason,          // score 75–100
    PartiallyInSeason,   // score 40–74
    OffSeason,           // score 0–39
    Unavailable          // score is null — no produce ingredients matched
}

/// <summary>
/// Top-level result of the seasonality scoring algorithm for a single recipe.
/// </summary>
public class SeasonalityResult
{
    /// <summary>
    /// Normalised score 0–100, or null when no produce ingredients were matched.
    /// </summary>
    public double? Score { get; init; }

    public SeasonalityLabel Label { get; init; }

    /// <summary>
    /// Per-ingredient breakdown — only for produce items that were found in the dataset.
    /// </summary>
    public List<IngredientSeasonalityDetail> Breakdown { get; init; } = new();

    /// <summary>
    /// Human-readable best-seasons string, e.g. "Best in summer and autumn".
    /// Empty string when Label is Unavailable.
    /// </summary>
    public string BestSeasons { get; init; } = string.Empty;
}

/// <summary>
/// Seasonality information for one matched produce ingredient.
/// </summary>
public class IngredientSeasonalityDetail
{
    /// <summary>Display name from the produce dataset (e.g. "Strawberry").</summary>
    public string Name { get; init; } = string.Empty;

    public bool IsInSeason { get; init; }

    /// <summary>1.0 / 0.75 / 0.5 / 0.25 per the fixed scarcity lookup table.</summary>
    public double ScarcityWeight { get; init; }

    public double WeightInGrams { get; init; }

    /// <summary>
    /// True when scarcityWeight >= 0.75.
    /// Used to surface the "Limited season — consider substituting" callout in the UI.
    /// </summary>
    public bool IsLimitedSeason => ScarcityWeight >= 0.75;
}
```

---

## Step 3 — Season Helper

**New file:** `Broccoli.App.Shared/Services/SeasonHelper.cs`

A small static utility. Keep it separate so it can be unit-tested independently and reused in
the best-season calculation.

```csharp
using Broccoli.Data.Models;

namespace Broccoli.App.Shared.Services;

public static class SeasonHelper
{
    public static readonly IReadOnlyList<string> AllSeasons =
        ["spring", "summer", "autumn", "winter"];

    /// <summary>
    /// Returns the NZ season name for a given date (Southern Hemisphere).
    /// Spring = Sep/Oct/Nov · Summer = Dec/Jan/Feb · Autumn = Mar/Apr/May · Winter = Jun/Jul/Aug
    /// </summary>
    public static string GetCurrentSeason(DateTime date) => date.Month switch
    {
        9 or 10 or 11 => "spring",
        12 or 1 or 2  => "summer",
        3 or 4 or 5   => "autumn",
        6 or 7 or 8   => "winter",
        _             => "summer"   // unreachable
    };

    /// <summary>
    /// Returns the fixed scarcity weight for a produce item.
    /// year_round overrides to 0.25 regardless of the seasons list length.
    /// </summary>
    public static double GetScarcityWeight(ProduceItem item)
    {
        if (item.YearRound) return 0.25;
        return item.Seasons.Count switch
        {
            1 => 1.00,
            2 => 0.75,
            3 => 0.50,
            _ => 0.25
        };
    }
}
```

---

## Step 4 — Service Interface

**New file:** `Broccoli.App.Shared/Services/ISeasonalityService.cs`

```csharp
using Broccoli.Data.Models;

namespace Broccoli.App.Shared.Services;

public interface ISeasonalityService
{
    /// <summary>
    /// Scores a recipe's seasonality from a list of already-parsed ingredient matches.
    /// Only matched items with weight ≥ 5 g that appear in the NZ produce dataset
    /// contribute to the score. All other ingredients are silently ignored.
    /// </summary>
    /// <param name="matches">
    ///   Output of <see cref="IngredientParserService.ParseAndMatchIngredientsAsync"/>.
    /// </param>
    /// <param name="asOf">Date to score against; defaults to DateTime.Now.</param>
    SeasonalityResult Score(IEnumerable<ParsedIngredientMatch> matches, DateTime? asOf = null);
}
```

---

## Step 5 — Service Implementation

**New file:** `Broccoli.App.Shared/Services/LocalJsonSeasonalityService.cs`

### Construction

Load `nz-produce.json` from the embedded resource stream of the `Broccoli.App.Shared` assembly:

```csharp
var assembly = typeof(LocalJsonSeasonalityService).Assembly;
using var stream = assembly.GetManifestResourceStream(
    "Broccoli.App.Shared.Data.nz-produce.json");
```

Deserialise into `ProduceDataset` (the wrapper DTO), then build two lookups:

1. `Dictionary<string, ProduceItem>` keyed on **normalised produce name** for fast O(1) matching.
2. Keep the raw `List<ProduceItem>` for the best-season enumeration.

### Name Normalisation

`FoodDatabase.json` uses names such as `"Carrots, Raw"` and `"Strawberries"` while
`nz-produce.json` uses `"Carrot"` and `"Strawberry"`. A shared normalisation function bridges
the gap:

```
1. Lower-case the input.
2. Strip everything from the first comma onward  ("carrots, raw" → "carrots").
3. Remove stopwords (reuse the same set from LocalJsonFoodService:
   raw, fresh, free, range, diced, sliced, grated, skinless, lite, baby,
   chopped, minced, peeled, deseeded, rinsed, drained, cooked, uncooked,
   dried, frozen, canned, tin, tinned, large, small, medium, whole,
   halved, roughly, finely, thinly).
4. Apply a small explicit plural-fix map for known mismatches:
     "strawberries" → "strawberry"
     "raspberries"  → "raspberry"
     "blackberries" → "blackberry"
     "boysenberries"→ "boysenberry"
     "blueberries"  → "blueberry"
     "cherries"     → "cherry"
     "gooseberries" → "gooseberry"
     "redcurrants"  → "redcurrant"
     "blackcurrants"→ "blackcurrant"
   For all other words: if the string ends in "s" and is ≥ 4 chars, strip the "s".
   (Handles "carrots"→"carrot", "mushrooms"→"mushroom", etc.)
5. Trim whitespace.
```

> **Why not just reuse `LocalJsonFoodService.FindBestMatch`?**
> `FindBestMatch` maps a raw text fragment to a `Food` (the nutrition DB).
> Seasonality matching happens *after* that step — we already have a `Food` and need to
> map its name to a `ProduceItem`. These are two independent datasets. A lightweight
> normalise-and-lookup is sufficient because the produce dataset is only 74 items and the
> FoodDatabase names are already clean strings.

Build the lookup dictionary by normalising every `ProduceItem.Name` at construction time.
During scoring, normalise the incoming `MatchedFood.Name` the same way and do an exact lookup.
If the exact lookup misses, do a linear scan checking whether either string contains the other —
this catches cases like the FoodDatabase having `"Green Cabbage"` matching the produce item
`"Green Cabbage"` vs a recipe using plain `"Cabbage"`.

### `Score` Method — Algorithm

```
const double MinGrams = 5.0;

season = SeasonHelper.GetCurrentSeason(asOf ?? DateTime.Now)

double totalWeighted = 0, totalPossible = 0
breakdown = []

for each ParsedIngredientMatch m in matches:
    if !m.IsMatched                        → skip
    grams = m.GetWeightInGrams()
    if grams < MinGrams                    → skip
    produce = Lookup(m.MatchedFood.Name)   → normalised match
    if produce == null                     → skip (non-produce ingredient)

    inSeason       = produce.YearRound || produce.Seasons.Contains(season)
    scarcityWeight = SeasonHelper.GetScarcityWeight(produce)
    contribution   = (inSeason ? 1.0 : 0.0) × scarcityWeight × grams
    possible       = scarcityWeight × grams

    breakdown.Add(new IngredientSeasonalityDetail { ... })
    totalWeighted += contribution
    totalPossible += possible

if totalPossible == 0:
    return SeasonalityResult { Score = null, Label = Unavailable, BestSeasons = "" }

score = (totalWeighted / totalPossible) × 100
label = score >= 75 ? PeakSeason
      : score >= 40 ? PartiallyInSeason
      : OffSeason

bestSeasons = ComputeBestSeasons(breakdown, allSeasons)

return SeasonalityResult { Score = score, Label = label,
                           Breakdown = breakdown, BestSeasons = bestSeasons }
```

### `ComputeBestSeasons` (private helper)

For each of the four seasons, re-run the weighted sum using that season as the reference instead
of the current one (iterating the already-built `breakdown` list — no re-parsing needed).
Rank seasons by score descending. Include all seasons whose score is within 10 points of the
top-ranked season. Format as:

- `"Best in summer"` (one season)
- `"Best in summer and autumn"` (two or more)
- `""` if the top score across all seasons is 0 (all-non-produce recipe)

---

## Step 6 — UI Component: `SeasonalityBadge`

**New files:**
- `Broccoli.App.Shared/Components/SeasonalityBadge.razor`
- `Broccoli.App.Shared/Components/SeasonalityBadge.razor.css`

A small inline pill/chip for use on recipe cards. Single parameter:

```csharp
[Parameter] public SeasonalityResult? Result { get; set; }
```

When `Result` is null (scores not yet computed), render nothing — the badge appears
progressively once the background scoring pass completes.

| Label | Emoji | Background | Text |
|---|---|---|---|
| `PeakSeason` | 🌿 | `#e8f5e9` | `#2e7d32` |
| `PartiallyInSeason` | 🌤️ | `#fff8e1` | `#f57f17` |
| `OffSeason` | ❄️ | `#eceff1` | `#546e7a` |
| `Unavailable` | — | `#f5f5f5` | `#9e9e9e` (text: "Not scored") |

Style to match the existing `.recipe-tag` pill in `Recipes.razor.css`
(padding `0.25rem 0.75rem`, `border-radius: 12px`, `font-size: 0.75rem`, `font-weight: 500`).

---

## Step 7 — UI Component: `SeasonalityPanel`

**New files:**
- `Broccoli.App.Shared/Components/SeasonalityPanel.razor`
- `Broccoli.App.Shared/Components/SeasonalityPanel.razor.css`

The detailed panel shown in recipe views. Parameters:

```csharp
[Parameter] public SeasonalityResult? Result { get; set; }
[Parameter] public bool IsLoading { get; set; }
```

### Layout

```
┌─────────────────────────────────────────────────────────┐
│  Seasonality                                            │
│  🌿  Peak season   82 / 100   Best in summer and autumn │
├─────────────────────────────────────────────────────────┤
│  Ingredient    In Season   Weight     Scarcity          │
│  Strawberry    ✓           240 g      ●● (high)         │
│  Carrot        ✓           120 g      ○ (low, yr-round) │
│  Blackberry    ✗           80 g                         │
│  ⚠️ Limited season — consider substituting              │
└─────────────────────────────────────────────────────────┘
```

- Show the "Limited season — consider substituting" callout inline under any row where
  `detail.IsLimitedSeason && !detail.IsInSeason`.
- Style to match `ParsedIngredientsTable`:
  - Container: `background: #f8f9fa`, `border: 1px solid #dee2e6`, `border-radius: 8px`
  - Header (score + label): `background: #ffffff`, `border-bottom: 2px solid #dee2e6`,
    `padding: 1rem`, sticky positioning.
  - When `IsLoading` is true, show the same spinner/text pattern used in
    `ParsedIngredientsTable` (`"Parsing ingredients..."` → `"Calculating seasonality..."`).

---

## Step 8 — Wire Up: `RecipeReadOnly.razor` / `RecipeReadOnly.razor.cs`

This is the primary surface for the detailed panel.

### `RecipeReadOnly.razor.cs`

1. Inject `IngredientParserService` and `ISeasonalityService`.
2. Add state fields:
   ```csharp
   private SeasonalityResult? _seasonality;
   private bool _seasonalityLoading;
   ```
3. In `LoadRecipe()`, after `recipe` is successfully populated, kick off scoring:
   ```csharp
   _seasonalityLoading = true;
   StateHasChanged();
   var matches = await ingredientParserService.ParseAndMatchIngredientsAsync(recipe.Ingredients);
   _seasonality = seasonalityService.Score(matches);
   _seasonalityLoading = false;
   ```
   The parse+score runs in the same `LoadRecipe` call, so it's sequential but still async.

### `RecipeReadOnly.razor`

Add a new section after the existing ingredients section:

```razor
<section class="seasonality-section">
    <SeasonalityPanel Result="@_seasonality" IsLoading="@_seasonalityLoading" />
</section>
```

---

## Step 9 — Wire Up: `Recipes.razor` / `Recipes.razor.cs` (recipe cards)

Scoring all recipes eagerly at page load would parse every recipe's ingredient text
synchronously — expensive at scale. Use a **fire-and-forget background pass** instead.

### `Recipes.razor.cs`

1. Inject `IngredientParserService` and `ISeasonalityService`.
2. Add state:
   ```csharp
   private Dictionary<string, SeasonalityResult?> _seasonalityScores = new();
   ```
3. Override `OnAfterRenderAsync`:
   ```csharp
   protected override async Task OnAfterRenderAsync(bool firstRender)
   {
       if (firstRender && allRecipes.Any())
       {
           await ScoreAllRecipesAsync();
           StateHasChanged();
       }
   }
   ```
4. `ScoreAllRecipesAsync` iterates `allRecipes` in batches of ~10 (to keep the UI
   responsive between batches), calling `ParseAndMatchIngredientsAsync` then
   `seasonalityService.Score` for each, populating `_seasonalityScores` by `recipe.Id`.

### `Recipes.razor`

Inside the recipe card, add the badge immediately below the recipe tags section:

```razor
<SeasonalityBadge Result="@(_seasonalityScores.GetValueOrDefault(recipe.Id))" />
```

No changes needed to the existing card layout — the badge slots in alongside `.recipe-tags`
using the existing flex/wrap flow in `.recipe-content` (see `Recipes.razor.css`).

---

## Step 10 — Register Services

Add to both `Broccoli.App.Web/Program.cs` and `Broccoli.App/MauiProgram.cs`, alongside the
existing `IngredientParserService` registration:

```csharp
builder.Services.AddSingleton<ISeasonalityService, LocalJsonSeasonalityService>();
```

`LocalJsonSeasonalityService` loads the embedded resource from the `Broccoli.App.Shared`
assembly at construction time — no path arguments, no environment-specific branching.

---

## Step 11 — Unit Tests

**New file:** `Broccoli.App.UnitTests/Services/SeasonalityServiceTests.cs`

Follow the `[TestClass]` / `[TestMethod]` + Moq pattern from `IngredientParserServiceTests.cs`.
Build test `ParsedIngredientMatch` instances by hand (mock `MatchedFood` with a `Food` whose
`Name` maps to a known entry in `nz-produce.json`).

| Test method | What it covers |
|---|---|
| `GetCurrentSeason_AllMonths_CorrectMapping` | Table-driven across all 12 months |
| `GetScarcityWeight_OneSeason_Returns1_0` | `seasons.Count == 1`, `year_round == false` |
| `GetScarcityWeight_TwoSeasons_Returns0_75` | |
| `GetScarcityWeight_ThreeSeasons_Returns0_5` | |
| `GetScarcityWeight_YearRound_Returns0_25` | `year_round == true` regardless of seasons count |
| `Score_AllInSeason_Returns100` | Every produce ingredient is in current season |
| `Score_AllOutOfSeason_Returns0` | Every produce ingredient is out of season |
| `Score_MixedIngredients_CorrectWeightedScore` | Manual calculation to verify formula |
| `Score_NoProduceMatched_ReturnsNull` | Non-produce-only recipe → `Score == null`, `Label == Unavailable` |
| `Score_GramsBelowNoiseFloor_Excluded` | Ingredient with < 5 g is not counted |
| `Score_YearRoundAlwaysInSeason` | `year_round` item contributes positively regardless of season |
| `Score_SingleIngredient_NormalisesCorrectly` | Edge case: single item → score is 0 or 100 |
| `Score_BestSeasons_ComputedCorrectly` | Known recipe → expected best-seasons string |
| `Score_UnmatchedIngredient_IsSkipped` | `IsMatched == false` items have no effect |

---

## File Summary

| Status | Path |
|---|---|
| **New** | `Broccoli.App.Shared/Data/nz-produce.json` |
| **New** | `Broccoli.App.Shared/Models/ProduceItem.cs` |
| **New** | `Broccoli.App.Shared/Models/SeasonalityResult.cs` |
| **New** | `Broccoli.App.Shared/Services/SeasonHelper.cs` |
| **New** | `Broccoli.App.Shared/Services/ISeasonalityService.cs` |
| **New** | `Broccoli.App.Shared/Services/LocalJsonSeasonalityService.cs` |
| **New** | `Broccoli.App.Shared/Components/SeasonalityBadge.razor` |
| **New** | `Broccoli.App.Shared/Components/SeasonalityBadge.razor.css` |
| **New** | `Broccoli.App.Shared/Components/SeasonalityPanel.razor` |
| **New** | `Broccoli.App.Shared/Components/SeasonalityPanel.razor.css` |
| **New** | `Broccoli.App.UnitTests/Services/SeasonalityServiceTests.cs` |
| **Modified** | `Broccoli.App.Shared/Broccoli.App.Shared.csproj` — add `<EmbeddedResource>` |
| **Modified** | `Broccoli.App.Shared/Pages/RecipeReadOnly.razor` — add `<SeasonalityPanel>` |
| **Modified** | `Broccoli.App.Shared/Pages/RecipeReadOnly.razor.cs` — inject + compute score |
| **Modified** | `Broccoli.App.Shared/Pages/Recipes.razor` — add `<SeasonalityBadge>` to cards |
| **Modified** | `Broccoli.App.Shared/Pages/Recipes.razor.cs` — inject + background scoring pass |
| **Modified** | `Broccoli.App.Web/Program.cs` — register `ISeasonalityService` |
| **Modified** | `Broccoli.App/MauiProgram.cs` — register `ISeasonalityService` |

---

## Open Questions / Deferred Decisions

1. **Stopword deduplication.** `LocalJsonFoodService` has a private `s_stopwords` set.
   `LocalJsonSeasonalityService` needs the same list for name normalisation. Extract the set
   to a shared internal static class (e.g. `IngredientTextUtils`) to avoid duplication, or
   simply duplicate it for now and note the tech debt.

2. **Plural normalisation edge cases.** The explicit plural-fix map in Step 5 covers the 74
   dataset items well. If the food database is expanded, new irregular plurals may need to be
   added. A comment in the code noting this is sufficient.

3. **Performance on large recipe lists.** The background scoring pass in `Recipes.razor.cs`
   calls `ParseAndMatchIngredientsAsync` once per recipe. If a user has hundreds of recipes
   this could be slow. A future optimisation is to cache `ParsedIngredientMatch[]` on first
   parse (e.g. after `RecipeDetail` loads a recipe) and expose it for reuse by the seasonality
   service. For the initial implementation, batching in groups of 10 is sufficient.

4. **`year_round` judgment calls in the dataset.** `leek` (hard to find Nov–Feb) and `swede`
   (limited Dec–Jan) are `year_round: true`, giving them scarcity weight 0.25. These are
   data-only changes — set `year_round: false` in `nz-produce.json` if the seasonal gap is
   significant enough to penalise. No code changes required.

5. **`RecipeDetail.razor` (edit view).** The spec calls out `RecipeReadOnly` as the primary
   scoring surface, and the edit page is likely too noisy a context for a seasonality panel.
   Skip for the initial implementation; add later if wanted.

