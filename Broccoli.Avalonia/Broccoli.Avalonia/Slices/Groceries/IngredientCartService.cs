using Broccoli.Avalonia.IngredientParsing;
using Broccoli.Avalonia.Models;

namespace Broccoli.Avalonia.Slices.Groceries;

public sealed class CartPreviewData
{
    public string DisplayName { get; init; } = string.Empty;
    public string FormattedLine { get; init; } = string.Empty;
    public string FoodName { get; init; } = string.Empty;
    public string OriginalLine { get; init; } = string.Empty;
    public bool IsMerge { get; init; }
}

public class IngredientCartService(
    IngredientParserService parser,
    IGroceryListService groceryListService)
{
    private static readonly HashSet<string> s_ignoredFoods = new(StringComparer.OrdinalIgnoreCase)
    {
        "water",
    };

    public static bool IsIgnoredIngredient(ParsedIngredientMatch match)
    {
        string food = match.IsMatched
            ? match.MatchedFood!.Name
            : match.ParsedIngredient.FoodDescription;

        return s_ignoredFoods.Contains(NormalizeFood(food));
    }

    public static string Format(ParsedIngredientMatch match)
    {
        double qty  = match.ParsedIngredient.Quantity;
        string unit = match.ParsedIngredient.CanonicalUnit ?? string.Empty;
        string food = match.IsMatched
            ? match.MatchedFood!.Name
            : match.ParsedIngredient.FoodDescription;

        return BuildLine(qty, unit, food);
    }

    public void AddToCart(IEnumerable<string> selectedLines)
    {
        List<CartChange> changes = ComputeCartChanges(selectedLines);

        foreach (GroceryListItem item in changes.Where(c => c.IsUpdate).Select(c => c.Item))
        {
            groceryListService.Update(item);
        }

        List<GroceryListItem> toAdd = changes.Where(c => !c.IsUpdate).Select(c => c.Item).ToList();
        if (toAdd.Count > 0)
        {
            groceryListService.AddMultiple(toAdd);
        }
    }

    public List<CartPreviewData> PreviewAddToCart(IEnumerable<string> selectedLines)
    {
        List<CartChange> changes = ComputeCartChanges(selectedLines);

        List<CartPreviewData> preview = [];

        foreach (CartChange change in changes)
        {
            if (change.IsUpdate)
            {
                preview.Add(new CartPreviewData
                {
                    DisplayName = $"{change.Item.Name} (merge with existing)",
                    FormattedLine = change.Item.Name,
                    FoodName = ExtractFoodName(change.Item.Name),
                    OriginalLine = change.OriginalLine,
                    IsMerge = true,
                });
            }
            else
            {
                preview.Add(new CartPreviewData
                {
                    DisplayName = change.Item.Name,
                    FormattedLine = change.Item.Name,
                    FoodName = ExtractFoodName(change.Item.Name),
                    OriginalLine = change.OriginalLine,
                    IsMerge = false,
                });
            }
        }

        return preview;
    }

    private sealed record CartChange(GroceryListItem Item, string OriginalLine, bool IsUpdate);

    private List<CartChange> ComputeCartChanges(IEnumerable<string> selectedLines)
    {
        var lines = selectedLines
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToList();

        if (lines.Count == 0)
        {
            return [];
        }

        List<ParsedIngredientMatch> newMatches = parser.ParseAndMatchIngredients(string.Join("\n", lines));
        newMatches = DeduplicateUnmatched(newMatches)
            .Where(m => !IsIgnoredIngredient(m))
            .ToList();

        List<GroceryListItem> existingItems = groceryListService.GetAll();

        var changes = new List<CartChange>();
        var claimedIds = new HashSet<string>();

        foreach (ParsedIngredientMatch newMatch in newMatches)
        {
            string originalLine = newMatch.ParsedIngredient.RawLine;
            (GroceryListItem? existingItem, double existingQty, string? effectiveUnit) = FindMatch(newMatch, existingItems, claimedIds);

            if (existingItem is not null)
            {
                bool unifiedToGrams = effectiveUnit == "g"
                    && (newMatch.ParsedIngredient.CanonicalUnit ?? string.Empty).ToLowerInvariant() != "g";
                double newQty  = unifiedToGrams ? newMatch.GetWeightInGrams() : newMatch.ParsedIngredient.Quantity;
                double merged  = existingQty + newQty;
                string unit    = effectiveUnit;
                string food    = newMatch.IsMatched
                    ? newMatch.MatchedFood!.Name
                    : newMatch.ParsedIngredient.FoodDescription;

                existingItem.Name = BuildLine(merged, unit, food);
                existingItem.QuantityHint = ComputeHint(existingItem.Name, parser);
                claimedIds.Add(existingItem.Id);
                changes.Add(new CartChange(existingItem, originalLine, true));
            }
            else
            {
                var newItem = new GroceryListItem
                {
                    Name      = Format(newMatch),
                    IsChecked = false,
                    QuantityHint = newMatch.GetQuantityHint(),
                };
                changes.Add(new CartChange(newItem, originalLine, false));
            }
        }

        return changes;
    }

    private static List<ParsedIngredientMatch> DeduplicateUnmatched(List<ParsedIngredientMatch> matches)
    {
        var result = new List<ParsedIngredientMatch>(matches.Count);
        var seen = new Dictionary<string, ParsedIngredientMatch>(StringComparer.OrdinalIgnoreCase);

        foreach (ParsedIngredientMatch match in matches)
        {
            if (match.IsMatched)
            {
                result.Add(match);
                continue;
            }

            string key = $"{NormalizeFood(match.ParsedIngredient.FoodDescription)}|{match.ParsedIngredient.CanonicalUnit}";
            if (seen.TryGetValue(key, out ParsedIngredientMatch? existing))
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

    private (GroceryListItem? item, double existingQty, string effectiveUnit) FindMatch(
        ParsedIngredientMatch newMatch,
        IEnumerable<GroceryListItem> existingItems,
        HashSet<string> claimedIds)
    {
        string newFood = NormalizeFood(newMatch.IsMatched
            ? newMatch.MatchedFood!.Name
            : newMatch.ParsedIngredient.FoodDescription);

        string newUnit = (newMatch.ParsedIngredient.CanonicalUnit ?? string.Empty).ToLowerInvariant();

        foreach (GroceryListItem item in existingItems)
        {
            if (item.IsChecked)
            {
                continue;
            }

            if (claimedIds.Contains(item.Id))
            {
                continue;
            }

            if (!item.Name.Contains(newFood, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            List<ParsedIngredientMatch> parsed = parser.ParseAndMatchIngredients(item.Name);
            ParsedIngredientMatch? existingMatch = parsed.FirstOrDefault();
            if (existingMatch is null)
            {
                continue;
            }

            string existingFood = NormalizeFood(existingMatch.IsMatched
                ? existingMatch.MatchedFood!.Name
                : existingMatch.ParsedIngredient.FoodDescription);

            string existingUnit = (existingMatch.ParsedIngredient.CanonicalUnit ?? string.Empty).ToLowerInvariant();

            bool sameFood = newMatch.IsMatched && existingMatch.IsMatched
                ? newMatch.MatchedFood!.Id == existingMatch.MatchedFood!.Id
                : newFood == existingFood;

            if (!sameFood)
            {
                continue;
            }

            if (newUnit == existingUnit)
            {
                return (item, existingMatch.ParsedIngredient.Quantity, newUnit);
            }

            double existingGrams = existingMatch.GetWeightInGrams();
            double newGrams      = newMatch.GetWeightInGrams();
            if (existingGrams > 0 && newGrams > 0)
            {
                return (item, existingGrams, "g");
            }
        }

        return (null, 0, newUnit);
    }

    public static string BuildLine(double qty, string unit, string food)
    {
        string qtyStr = qty.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);

        if (string.IsNullOrEmpty(unit))
        {
            return $"{qtyStr} {food}";
        }

        bool attach = unit is "g" or "kg" or "ml" or "l";
        return attach ? $"{qtyStr}{unit} {food}" : $"{qtyStr} {unit} {food}";
    }

    private static string? ComputeHint(string formattedLine, IngredientParserService parser)
    {
        List<ParsedIngredientMatch> matches = parser.ParseAndMatchIngredients(formattedLine);
        return matches.FirstOrDefault()?.GetQuantityHint();
    }

    private static string NormalizeFood(string name)
    {
        int comma = name.IndexOf(',');
        return (comma >= 0 ? name[..comma] : name).Trim().ToLowerInvariant();
    }

    private static string ExtractFoodName(string line)
    {
        if (line.EndsWith(" (merge with existing)", StringComparison.Ordinal))
        {
            line = line[..^22];
        }

        System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(
            line,
            @"^[\d.]+(?:g|kg|ml|l|cups?|tbsp|tsp|oz|lbs?)?\s*(?:of\s+)?(.+)$",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        if (match.Success)
        {
            string food = match.Groups[1].Value.Trim();
            int comma = food.IndexOf(',');
            return (comma >= 0 ? food[..comma] : food).Trim();
        }

        return line;
    }
}
