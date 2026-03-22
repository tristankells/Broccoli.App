using Broccoli.App.Shared.IngredientParsing;
using Broccoli.Data.Models;
using Microsoft.AspNetCore.Components;

namespace Broccoli.App.Shared.Slices.GroceryList;

public partial class AddIngredientsDialog
{
    /// <summary>Whether the dialog is visible.</summary>
    [Parameter]
    public bool IsVisible { get; set; }

    /// <summary>Display name of the recipe (shown in the dialog subtitle).</summary>
    [Parameter]
    public string? RecipeName { get; set; }

    /// <summary>Newline-separated ingredient string from the recipe.</summary>
    [Parameter]
    public string? IngredientsText { get; set; }

    /// <summary>All pantry items for the current user (injected by parent).</summary>
    [Parameter]
    public List<PantryItem> PantryItems { get; set; } = new();

    /// <summary>Callback raised when the user cancels.</summary>
    [Parameter]
    public EventCallback OnCancel { get; set; }

    /// <summary>Callback raised with the list of checked GroceryListItems to add.</summary>
    [Parameter]
    public EventCallback<List<string>> OnConfirm { get; set; }

    private List<IngredientRow> ingredientRows = new();
    private bool isLoading = true;
    private bool _prevVisible = false;
    private string? _lastIngredientsText;

    protected override async Task OnParametersSetAsync()
    {
        bool becameVisible = IsVisible && !_prevVisible;
        bool ingredientsChanged = IsVisible && IngredientsText != _lastIngredientsText;
        _prevVisible = IsVisible;

        if (becameVisible || ingredientsChanged)
        {
            _lastIngredientsText = IngredientsText;
            await BuildRowsAsync();
        }
    }

    private async Task BuildRowsAsync()
    {
        isLoading = true;
        ingredientRows.Clear();

        if (string.IsNullOrWhiteSpace(IngredientsText))
        {
            isLoading = false;
            return;
        }

        // Parse the full ingredient text. The parser already merges matched duplicates
        // (same Food ID). We then apply a secondary pass for unmatched items.
        var matches = await IngredientParserService.ParseAndMatchIngredientsAsync(IngredientsText);

        // Secondary dedup: group unmatched items by normalised description + unit.
        var dedupedMatches = new List<(ParsedIngredientMatch match, bool wasMerged)>();
        var unmatchedSeen = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var match in matches)
        {
            if (match.IsMatched)
            {
                // Count as merged if quantity > what a single line would produce
                // (the parser already summed quantities for matched dupes)
                dedupedMatches.Add((match, false));
            }
            else
            {
                string key =
                    $"{NormalizeFood(match.ParsedIngredient.FoodDescription)}|{match.ParsedIngredient.CanonicalUnit}";
                if (unmatchedSeen.TryGetValue(key, out int idx))
                {
                    dedupedMatches[idx].match.ParsedIngredient.Quantity
                        += match.ParsedIngredient.Quantity;
                    // Mark as merged
                    var existing = dedupedMatches[idx];
                    dedupedMatches[idx] = (existing.match, true);
                }
                else
                {
                    unmatchedSeen[key] = dedupedMatches.Count;
                    dedupedMatches.Add((match, false));
                }
            }
        }

        foreach (var (match, wasMerged) in dedupedMatches)
        {
            // Skip ingredients that should never appear in a grocery list (e.g. water)
            if (IngredientCartService.IsIgnoredIngredient(match)) continue;

            string displayLine = FormatIngredient(match);
            var (status, isChecked) = GetPantryStatus(displayLine);
            ingredientRows.Add(new IngredientRow
            {
                IngredientLine = displayLine,
                PantryStatus = status,
                IsChecked = isChecked,
                IsMerged = wasMerged
            });
        }

        isLoading = false;
    }

    private static string FormatIngredient(ParsedIngredientMatch match) =>
        IngredientCartService.BuildLine(
            match.ParsedIngredient.Quantity,
            match.ParsedIngredient.CanonicalUnit ?? string.Empty,
            match.IsMatched ? match.MatchedFood!.Name : match.ParsedIngredient.FoodDescription);

    private static string NormalizeFood(string name)
    {
        int comma = name.IndexOf(',');
        return (comma >= 0 ? name[..comma] : name).Trim().ToLowerInvariant();
    }

    private (PantryMatchStatus status, bool isChecked) GetPantryStatus(string line)
    {
        var lineLower = line.ToLowerInvariant();

        foreach (var pantryItem in PantryItems)
        {
            var nameLower = pantryItem.Name.ToLowerInvariant();
            if (lineLower.Contains(nameLower) || nameLower.Contains(lineLower))
            {
                return pantryItem.Category == PantryCategory.AlwaysHave
                    ? (PantryMatchStatus.AlwaysHave, false) // auto-uncheck staples
                    : (PantryMatchStatus.CheckIfHave, false); // keep checked, but flag it
            }
        }

        return (PantryMatchStatus.NotInPantry, true); // not in pantry ? default checked
    }

    private async Task Cancel()
    {
        await OnCancel.InvokeAsync();
    }

    private async Task Confirm()
    {
        var selectedLines = ingredientRows
            .Where(r => r.IsChecked)
            .Select(r => r.IngredientLine)
            .ToList();

        await OnConfirm.InvokeAsync(selectedLines);
    }

    private static string GetPantryStatusLabel(PantryMatchStatus status) =>
        status switch
        {
            PantryMatchStatus.AlwaysHave => "in pantry: Always Have",
            PantryMatchStatus.CheckIfHave => "in pantry: Check If Have",
            _ => string.Empty
        };

    private static string GetPantryBadgeClass(PantryMatchStatus status) =>
        status switch
        {
            PantryMatchStatus.AlwaysHave => "badge-always-have",
            PantryMatchStatus.CheckIfHave => "badge-check-if-have",
            _ => string.Empty
        };

    private class IngredientRow
    {
        public string IngredientLine { get; set; } = string.Empty;

        public PantryMatchStatus PantryStatus { get; set; }

        public bool IsChecked { get; set; }

        /// <summary>True when this row is the result of merging two or more duplicate ingredient lines.</summary>
        public bool IsMerged { get; set; }
    }

    private enum PantryMatchStatus
    {
        NotInPantry,
        AlwaysHave,
        CheckIfHave
    }
}