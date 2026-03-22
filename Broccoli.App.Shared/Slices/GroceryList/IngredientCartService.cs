using Broccoli.App.Shared.IngredientParsing;
using Broccoli.Data.Models;

namespace Broccoli.App.Shared.Slices.GroceryList;

/// <summary>
/// Handles the "add to grocery cart" action with two levels of deduplication:
/// <list type="number">
///   <item>Within the batch — ingredients that resolve to the same food and unit are merged.</item>
///   <item>Against the existing grocery list — if the item is already there, its quantity is
///         updated rather than a duplicate being inserted.</item>
/// </list>
/// Only items with the same canonical unit can be merged (e.g. g+g ?, g+cup ?).
/// Items already checked off in the grocery list are left untouched.
/// </summary>
public class IngredientCartService(
    IngredientParserService parser,
    IGroceryListService groceryListService)
{
    // -- Ignored ingredients ---------------------------------------------------

    /// <summary>
    /// Foods that should never appear in the grocery cart (e.g. tap water).
    /// Matched against the normalised food name after comma-stripping.
    /// </summary>
    private static readonly HashSet<string> s_ignoredFoods = new(StringComparer.OrdinalIgnoreCase)
    {
        "water"
    };

    /// <summary>
    /// Returns true when the ingredient should be silently excluded from cart operations
    /// and from the add-ingredients dialog.
    /// </summary>
    public static bool IsIgnoredIngredient(ParsedIngredientMatch match)
    {
        string food = match.IsMatched
            ? match.MatchedFood!.Name
            : match.ParsedIngredient.FoodDescription;

        return s_ignoredFoods.Contains(NormalizeFood(food));
    }

    // -- Public API ------------------------------------------------------------

    /// <summary>
    /// Formats a <see cref="ParsedIngredientMatch"/> into a human-readable ingredient string,
    /// e.g. "300g Carrots, Raw", "2 cup flour", "1 drizzle olive oil".
    /// </summary>
    public static string Format(ParsedIngredientMatch match)
    {
        double qty  = match.ParsedIngredient.Quantity;
        string unit = match.ParsedIngredient.CanonicalUnit ?? string.Empty;
        string food = match.IsMatched
            ? match.MatchedFood!.Name
            : match.ParsedIngredient.FoodDescription;

        return BuildLine(qty, unit, food);
    }

    /// <summary>
    /// Parses <paramref name="selectedLines"/>, merges intra-batch duplicates, then for each
    /// resulting ingredient either updates an existing grocery list item (same food + same unit)
    /// or inserts it as a new item.
    /// </summary>
    public async Task AddToCartAsync(IEnumerable<string> selectedLines, string userId)
    {
        var lines = selectedLines
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToList();

        if (lines.Count == 0) return;

        // Parse the batch. The parser already merges matched duplicates (by Food ID + unit).
        var newMatches = await parser.ParseAndMatchIngredientsAsync(string.Join("\n", lines));
        // Apply secondary dedup for unmatched items and drop ignored foods (e.g. water).
        newMatches = DeduplicateUnmatched(newMatches)
            .Where(m => !IsIgnoredIngredient(m))
            .ToList();

        // Load the current grocery list once.
        var existingItems = await groceryListService.GetAllAsync(userId);

        var toUpdate = new List<GroceryListItem>();
        var toAdd    = new List<GroceryListItem>();

        // Track which existing items we've already matched so we don't merge the same
        // cart item twice (e.g. if the batch somehow contains two potato entries).
        var claimedIds = new HashSet<string>();

        foreach (var newMatch in newMatches)
        {
            var (existingItem, existingQty) = FindMatch(newMatch, existingItems, claimedIds);

            if (existingItem is not null)
            {
                double merged     = existingQty + newMatch.ParsedIngredient.Quantity;
                string unit       = newMatch.ParsedIngredient.CanonicalUnit ?? string.Empty;
                string food       = newMatch.IsMatched
                    ? newMatch.MatchedFood!.Name
                    : newMatch.ParsedIngredient.FoodDescription;

                existingItem.Name = BuildLine(merged, unit, food);
                claimedIds.Add(existingItem.Id);
                toUpdate.Add(existingItem);
            }
            else
            {
                toAdd.Add(new GroceryListItem
                {
                    Name      = Format(newMatch),
                    UserId    = userId,
                    IsChecked = false
                });
            }
        }

        // Persist changes.
        foreach (var item in toUpdate)
            await groceryListService.UpdateAsync(item);

        if (toAdd.Count > 0)
            await groceryListService.AddMultipleAsync(toAdd);
    }

    // -- Private helpers -------------------------------------------------------

    /// <summary>
    /// For unmatched ingredients (no food-DB match), merge those with the same normalised
    /// food description and canonical unit, summing their quantities.
    /// Matched ingredients are already deduplicated by the parser (by Food.Id).
    /// </summary>
    private static List<ParsedIngredientMatch> DeduplicateUnmatched(
        List<ParsedIngredientMatch> matches)
    {
        var result  = new List<ParsedIngredientMatch>(matches.Count);
        // key = "normalised-food|unit"
        var seen    = new Dictionary<string, ParsedIngredientMatch>(StringComparer.OrdinalIgnoreCase);

        foreach (var match in matches)
        {
            if (match.IsMatched)
            {
                result.Add(match); // parser already merged these
                continue;
            }

            string key = $"{NormalizeFood(match.ParsedIngredient.FoodDescription)}|{match.ParsedIngredient.CanonicalUnit}";
            if (seen.TryGetValue(key, out var existing))
            {
                existing.ParsedIngredient.Quantity += match.ParsedIngredient.Quantity;
            }
            else
            {
                seen[key] = match;
                result.Add(match);
            }
        }

        return result;
    }

    /// <summary>
    /// Finds an existing grocery list item that represents the same ingredient as
    /// <paramref name="newMatch"/> and has the same canonical unit.
    /// Returns the item and its parsed quantity, or (null, 0) if no match.
    /// </summary>
    private (GroceryListItem? item, double existingQty) FindMatch(
        ParsedIngredientMatch newMatch,
        IEnumerable<GroceryListItem> existingItems,
        HashSet<string> claimedIds)
    {
        string newFood = NormalizeFood(newMatch.IsMatched
            ? newMatch.MatchedFood!.Name
            : newMatch.ParsedIngredient.FoodDescription);

        string newUnit = (newMatch.ParsedIngredient.CanonicalUnit ?? string.Empty).ToLowerInvariant();

        foreach (var item in existingItems)
        {
            if (item.IsChecked)          continue; // already checked off — don't touch
            if (claimedIds.Contains(item.Id)) continue; // already claimed by another match

            // Quick food-name check before paying for a full parse
            if (!item.Name.Contains(newFood, StringComparison.OrdinalIgnoreCase))
                continue;

            // Parse the existing item to extract its quantity and unit
            var parsed = parser.ParseAndMatchIngredientsAsync(item.Name).Result; // sync internally
            var existingMatch = parsed.FirstOrDefault();
            if (existingMatch is null) continue;

            // Same ingredient?
            string existingFood = NormalizeFood(existingMatch.IsMatched
                ? existingMatch.MatchedFood!.Name
                : existingMatch.ParsedIngredient.FoodDescription);

            string existingUnit = (existingMatch.ParsedIngredient.CanonicalUnit ?? string.Empty).ToLowerInvariant();

            bool sameFood = newMatch.IsMatched && existingMatch.IsMatched
                ? newMatch.MatchedFood!.Id == existingMatch.MatchedFood!.Id
                : newFood == existingFood;

            if (sameFood && newUnit == existingUnit)
                return (item, existingMatch.ParsedIngredient.Quantity);
        }

        return (null, 0);
    }

    internal static string BuildLine(double qty, string unit, string food)
    {
        // Suppress trailing zeros: 200.0 ? "200", 1.5 ? "1.5", 0.333 ? "0.33"
        string qtyStr = qty.ToString("0.##");

        if (string.IsNullOrEmpty(unit)) return $"{qtyStr} {food}";

        // Weight/volume units attach directly to the number: "200g", "1.5kg", "250ml"
        bool attach = unit is "g" or "kg" or "ml" or "l";
        return attach ? $"{qtyStr}{unit} {food}" : $"{qtyStr} {unit} {food}";
    }

    private static string NormalizeFood(string name)
    {
        // Strip preparation notes after first comma: "Carrots, Raw" ? "carrots"
        int comma = name.IndexOf(',');
        return (comma >= 0 ? name[..comma] : name).Trim().ToLowerInvariant();
    }
}

