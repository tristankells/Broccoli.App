using Broccoli.Avalonia.IngredientParsing;
using Broccoli.Avalonia.Models;

namespace Broccoli.Avalonia.Slices.Groceries;

public class IngredientCartService(
    IngredientParserService parser,
    IGroceryListService groceryListService)
{
    private static readonly HashSet<string> s_ignoredFoods = new(StringComparer.OrdinalIgnoreCase)
    {
        "water"
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
        var lines = selectedLines
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToList();

        if (lines.Count == 0)
        {
            return;
        }

        var newMatches = parser.ParseAndMatchIngredients(string.Join("\n", lines));
        newMatches = DeduplicateUnmatched(newMatches)
            .Where(m => !IsIgnoredIngredient(m))
            .ToList();

        var existingItems = groceryListService.GetAll();

        var toUpdate = new List<GroceryListItem>();
        var toAdd    = new List<GroceryListItem>();
        var claimedIds = new HashSet<string>();

        foreach (var newMatch in newMatches)
        {
            var (existingItem, existingQty, effectiveUnit) = FindMatch(newMatch, existingItems, claimedIds);

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
                claimedIds.Add(existingItem.Id);
                toUpdate.Add(existingItem);
            }
            else
            {
                toAdd.Add(new GroceryListItem
                {
                    Name      = Format(newMatch),
                    IsChecked = false
                });
            }
        }

        foreach (var item in toUpdate)
        {
            groceryListService.Update(item);
        }

        if (toAdd.Count > 0)
        {
            groceryListService.AddMultiple(toAdd);
        }
    }

    private static List<ParsedIngredientMatch> DeduplicateUnmatched(List<ParsedIngredientMatch> matches)
    {
        var result = new List<ParsedIngredientMatch>(matches.Count);
        var seen = new Dictionary<string, ParsedIngredientMatch>(StringComparer.OrdinalIgnoreCase);

        foreach (var match in matches)
        {
            if (match.IsMatched)
            {
                result.Add(match);
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

    private (GroceryListItem? item, double existingQty, string effectiveUnit) FindMatch(
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

            var parsed = parser.ParseAndMatchIngredients(item.Name);
            var existingMatch = parsed.FirstOrDefault();
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

    private static string NormalizeFood(string name)
    {
        int comma = name.IndexOf(',');
        return (comma >= 0 ? name[..comma] : name).Trim().ToLowerInvariant();
    }
}
