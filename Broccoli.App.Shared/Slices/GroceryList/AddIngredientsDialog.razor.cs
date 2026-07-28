using Broccoli.App.Shared._Shared.IngredientParsing;
using Broccoli.App.Shared.Models;
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

    /// <summary>
    /// Optional callback raised when the user wants to add selected items to their pantry.
    /// Receives the clean food names (not full ingredient lines).
    /// When not set the "Add to Pantry" button is hidden.
    /// </summary>
    [Parameter]
    public EventCallback<List<string>> OnAddToPantry { get; set; }

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

        // Gram-normalize and cross-unit merge all matched items.
        var normalizedRows = GramNormalizeAndMerge(dedupedMatches);

        foreach (var (displayLine, originalDisplay, isMerged, isMatchedRow) in normalizedRows)
        {
            // Skip ingredients that should never appear in a grocery list (e.g. water)
            // For matched rows we need to check via the underlying match; for unmatched rows
            // we do a simple name check inline.
            if (string.IsNullOrWhiteSpace(displayLine)) continue;

            var (status, isChecked) = GetPantryStatus(displayLine);

            // Extract a clean food name for use when adding to pantry:
            // for matched rows use the matched food name embedded in the display line
            // (the last word-group after the quantity/unit prefix).
            // We'll keep it simple — strip the leading "Xg " / "X unit " prefix.
            string foodName = ExtractFoodName(displayLine);

            ingredientRows.Add(new IngredientRow
            {
                IngredientLine = displayLine,
                OriginalDisplay = originalDisplay,
                PantryStatus = status,
                IsChecked = isChecked,
                IsMerged = isMerged,
                FoodName = foodName
            });
        }

        // Sort by pantry status so "definitely need to buy" items appear first.
        // NotInPantry(0) → CheckIfHave(1) → AlwaysHave(2)
        ingredientRows = ingredientRows
            .OrderBy(r => r.PantryStatus == PantryMatchStatus.AlwaysHave ? 2
                        : r.PantryStatus == PantryMatchStatus.CheckIfHave ? 1
                        : 0)
            .ToList();

        isLoading = false;
    }

    /// <summary>
    /// Converts matched ingredients to grams (recording the original representation in brackets),
    /// then cross-unit merges any two entries that resolved to the same food.
    /// Unmatched items pass through unchanged.
    /// Returns tuples of (displayLine, originalDisplay, isMerged, isMatchedRow).
    /// </summary>
    private static List<(string displayLine, string originalDisplay, bool isMerged, bool isMatchedRow)>
        GramNormalizeAndMerge(List<(ParsedIngredientMatch match, bool wasMerged)> dedupedMatches)
    {
        // --- Step 1: normalize each matched item to grams -----------------------
        // key = MatchedFood.Id; value = list of (grams, originalPart, wasMerged)
        var matchedByFoodId = new Dictionary<int, List<(double grams, string originalPart, bool wasMerged)>>();

        foreach (var (match, wasMerged) in dedupedMatches)
        {
            if (!match.IsMatched || IngredientCartService.IsIgnoredIngredient(match))
                continue;

            string foodName = match.MatchedFood!.Name;
            double qty      = match.ParsedIngredient.Quantity;
            string unit     = match.ParsedIngredient.CanonicalUnit ?? string.Empty;

            double grams = match.GetWeightInGrams();

            string originalPart;
            double effectiveQty;
            string effectiveUnit;

            if (grams > 0 && !string.Equals(unit, "g", StringComparison.OrdinalIgnoreCase))
            {
                // Record original before normalizing.
                originalPart  = IngredientCartService.BuildLine(qty, unit, foodName);
                effectiveQty  = grams;
                effectiveUnit = "g";
            }
            else
            {
                // Already in grams or no conversion available — no bracket display.
                originalPart  = string.Empty;
                effectiveQty  = qty;
                effectiveUnit = unit;
            }

            // Temporarily store the effective quantity back so the cross-unit merge can sum.
            match.ParsedIngredient.Quantity     = effectiveQty;
            match.ParsedIngredient.CanonicalUnit = effectiveUnit;

            if (!matchedByFoodId.TryGetValue(match.MatchedFood.Id, out var bucket))
            {
                bucket = new List<(double, string, bool)>();
                matchedByFoodId[match.MatchedFood.Id] = bucket;
            }
            bucket.Add((effectiveQty, originalPart, wasMerged));
        }

        // --- Step 2: cross-unit merge per food ----------------------------------
        // We need to emit rows in the same order they first appeared, so build an
        // ordered list of food IDs (first-seen order).
        var foodIdOrder = new List<int>();
        var foodNames   = new Dictionary<int, string>();
        foreach (var (match, _) in dedupedMatches)
        {
            if (!match.IsMatched || IngredientCartService.IsIgnoredIngredient(match)) continue;
            int id = match.MatchedFood!.Id;
            if (!foodNames.ContainsKey(id))
            {
                foodIdOrder.Add(id);
                foodNames[id] = match.MatchedFood.Name;
            }
        }

        var result = new List<(string, string, bool, bool)>();

        foreach (int foodId in foodIdOrder)
        {
            var bucket   = matchedByFoodId[foodId];
            string food  = foodNames[foodId];

            double totalGrams = bucket.Sum(b => b.grams);
            // Unit: if any bucket entry used grams (i.e., was normalized), the merged result
            // is in grams.  If all were already grams, still grams.  Use the unit from the
            // first entry in case no normalization happened.
            string mergedUnit = bucket.Any(b => b.originalPart.Length > 0) ? "g"
                              : (dedupedMatches.FirstOrDefault(d => d.match.IsMatched && d.match.MatchedFood!.Id == foodId)
                                    .match?.ParsedIngredient.CanonicalUnit ?? "g");

            string displayLine    = IngredientCartService.BuildLine(totalGrams, mergedUnit, food);
            string originalDisplay = string.Join(" + ", bucket
                .Where(b => b.originalPart.Length > 0)
                .Select(b => b.originalPart));

            bool isMerged = bucket.Count > 1 || bucket.Any(b => b.wasMerged);

            result.Add((displayLine, originalDisplay, isMerged, true));
        }

        // --- Step 3: append unmatched items untouched ---------------------------
        foreach (var (match, wasMerged) in dedupedMatches)
        {
            if (match.IsMatched) continue;
            if (IngredientCartService.IsIgnoredIngredient(match)) continue;

            string line = IngredientCartService.BuildLine(
                match.ParsedIngredient.Quantity,
                match.ParsedIngredient.CanonicalUnit ?? string.Empty,
                match.ParsedIngredient.FoodDescription);

            result.Add((line, string.Empty, wasMerged, false));
        }

        return result;
    }

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

    private async Task AddRowToPantry(IngredientRow row)
    {
        if (string.IsNullOrWhiteSpace(row.FoodName)) return;
        row.WasAddedToPantry = true;  // immediate visual feedback
        await OnAddToPantry.InvokeAsync(new List<string> { row.FoodName });
    }

    /// <summary>
    /// Strips the leading quantity + unit prefix from a display line to get a clean food name.
    /// E.g. "200g Chicken Breast" → "Chicken Breast", "2 tbsp Olive Oil" → "Olive Oil".
    /// </summary>
    private static string ExtractFoodName(string line)
    {
        if (string.IsNullOrWhiteSpace(line)) return line;
        // Match an optional number, optional unit word, then the rest as the food name.
        var m = System.Text.RegularExpressions.Regex.Match(
            line.Trim(),
            @"^[\d.,/]+\s*(?:[a-zA-Z]+\s+)?(.+)$");
        return m.Success ? m.Groups[1].Value.Trim() : line.Trim();
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

        /// <summary>
        /// When the ingredient was normalised to grams, this holds the original representation(s)
        /// shown in brackets after the input, e.g. "2 carrots" or "100g + 2 carrots".
        /// Empty when no conversion was needed.
        /// </summary>
        public string OriginalDisplay { get; set; } = string.Empty;

        /// <summary>Clean food name (no quantity/unit) used when adding to pantry.</summary>
        public string FoodName { get; set; } = string.Empty;

        /// <summary>Set to true after the user clicks the pantry button, for immediate visual feedback.</summary>
        public bool WasAddedToPantry { get; set; }

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