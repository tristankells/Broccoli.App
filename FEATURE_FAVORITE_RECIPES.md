# Feature: Favorite Recipes

## Overview

Users can mark any recipe as a favourite directly from the recipe list. Favourites are pinned to the top of the list and can be isolated with a dedicated filter chip. The toggle is **optimistic** — the UI updates instantly without a full reload, and the change is persisted in the background via `UpdateAsync`.

---

## Affected Files

| File | Change |
|---|---|
| `Broccoli.App.Shared/Models/Recipe.cs` | Add `IsFavorite` property |
| `Broccoli.App.Shared/Pages/Recipes.razor` | Favourite button on card + Favourites filter chip |
| `Broccoli.App.Shared/Pages/Recipes.razor.cs` | `ToggleFavorite`, `ApplyFilters` sort + filter logic |
| `Broccoli.App.Shared/Pages/Recipes.razor.css` | Styles for star button, active state, and card highlight |

No changes to `IRecipeService` or `CosmosRecipeService` — `UpdateAsync` already persists the full model.

---

## Step 1 — `Recipe` Model

**File:** `Broccoli.App.Shared/Models/Recipe.cs`

Add a new `bool` property after `UpdatedAt`:

```csharp
/// <summary>
/// Whether the user has marked this recipe as a favourite.
/// Defaults to false; persisted to CosmosDB as "isFavorite".
/// </summary>
[JsonPropertyName("isFavorite")]
public bool IsFavorite { get; set; } = false;
```

No migration is needed — CosmosDB is schema-less and existing documents without the field will deserialise with the default value of `false`.

---

## Step 2 — `Recipes.razor.cs` (code-behind)

**File:** `Broccoli.App.Shared/Pages/Recipes.razor.cs`

### 2a — New state field

```csharp
private bool showFavoritesOnly = false;
```

### 2b — `ApplyFilters` — sort and filter

Replace the final `filteredRecipes = results.ToList();` assignment with:

```csharp
// Favourites-only filter
if (showFavoritesOnly)
{
    results = results.Where(r => r.IsFavorite);
}

// Favourites pinned first, then newest first
filteredRecipes = results
    .OrderByDescending(r => r.IsFavorite)
    .ThenByDescending(r => r.CreatedAt)
    .ToList();
```

### 2c — `ToggleFavorite` method

Add the following method. It mutates the in-memory object and calls `ApplyFilters` immediately (optimistic), then persists in the background. Errors are caught silently and the flag is rolled back so the UI stays consistent.

```csharp
private async Task ToggleFavorite(Recipe recipe)
{
    // Optimistic update — flip the flag and re-sort immediately
    recipe.IsFavorite = !recipe.IsFavorite;
    ApplyFilters();

    try
    {
        await RecipeService.UpdateAsync(recipe);
    }
    catch (Exception ex)
    {
        // Roll back on failure
        Console.WriteLine($"Error saving favourite for '{recipe.Name}': {ex.Message}");
        recipe.IsFavorite = !recipe.IsFavorite;
        ApplyFilters();
    }
}
```

### 2d — `ToggleFavoritesFilter` method

```csharp
private void ToggleFavoritesFilter()
{
    showFavoritesOnly = !showFavoritesOnly;
    ApplyFilters();
}
```

### 2e — `ClearFilters` — reset new flag

Update the existing `ClearFilters` method to also reset `showFavoritesOnly`:

```csharp
private void ClearFilters()
{
    selectedTags.Clear();
    searchTerm = string.Empty;
    showFavoritesOnly = false;   // ← add this line
    ApplyFilters();
}
```

---

## Step 3 — `Recipes.razor` (markup)

**File:** `Broccoli.App.Shared/Pages/Recipes.razor`

### 3a — Favourites filter chip

Inside the existing `.tag-filters` `<div>`, add a **Favourites** chip above (or alongside) the tag chips. Place it so it is always visible even when there are no tags:

```razor
<div class="tag-filters">
    {{-- NEW: Favourites chip (always shown) --}}
    <button class="tag-chip favorites-chip @(showFavoritesOnly ? "selected" : "")"
            @onclick="ToggleFavoritesFilter">
        @(showFavoritesOnly ? "★" : "☆") Favourites
    </button>

    @if (availableTags.Any())
    {
        ... existing tag chip markup ...
    }
</div>
```

### 3b — Favourite button on each recipe card

Inside `.recipe-actions`, add a star button before the Edit button:

```razor
<button class="btn-icon btn-favorite @(recipe.IsFavorite ? "is-favorite" : "")"
        title="@(recipe.IsFavorite ? "Remove from favourites" : "Add to favourites")"
        @onclick="() => ToggleFavorite(recipe)"
        @onclick:stopPropagation="true">
    @(recipe.IsFavorite ? "★" : "☆")
</button>
```

### 3c — Favourite indicator in the card header (optional visual cue)

Inside `.recipe-image`, overlay a star badge when `recipe.IsFavorite` is `true`:

```razor
<div class="recipe-image">
    @if (recipe.IsFavorite)
    {
        <span class="favorite-badge" title="Favourite">★</span>
    }
    ... existing image / placeholder markup ...
</div>
```

---

## Step 4 — `Recipes.razor.css`

**File:** `Broccoli.App.Shared/Pages/Recipes.razor.css`

```css
/* ── Favourite button in recipe-actions ── */
.btn-favorite {
    color: #aaa;
    font-size: 1.1rem;
    transition: color 0.2s ease, transform 0.15s ease;
}

.btn-favorite:hover {
    color: #f0ad00;
    transform: scale(1.2);
}

.btn-favorite.is-favorite {
    color: #f0ad00;
}

/* ── Favourites filter chip ── */
.favorites-chip {
    border-color: #f0ad00;
    color: #856404;
    background-color: #fff9e6;
}

.favorites-chip.selected {
    background-color: #f0ad00;
    color: white;
    border-color: #f0ad00;
}

/* ── Star badge overlaid on recipe image ── */
.recipe-image {
    position: relative; /* ensure badge positions correctly */
}

.favorite-badge {
    position: absolute;
    top: 0.5rem;
    right: 0.5rem;
    font-size: 1.25rem;
    color: #f0ad00;
    text-shadow: 0 1px 3px rgba(0, 0, 0, 0.4);
    pointer-events: none;
    z-index: 1;
}
```

---

## Behaviour Summary

| Scenario | Result |
|---|---|
| User clicks ☆ on a non-favourite | Flips to ★ instantly, card moves to top of list, persisted in background |
| User clicks ★ on a favourite | Flips to ☆ instantly, card drops to its natural position, persisted in background |
| `UpdateAsync` throws | Flag rolled back, card returns to previous position |
| "Favourites" chip active | List shows only `IsFavorite == true` recipes, still sorted newest-first within that set |
| "Favourites" chip + tag filter + search | All three filters are AND-combined before sorting |
| "Clear all filters" | Resets search, tag chips, and the Favourites chip simultaneously |
| New recipes from CosmosDB with no `isFavorite` field | Deserialise as `IsFavorite = false` (default value) |

