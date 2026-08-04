using System.Text.RegularExpressions;

namespace Broccoli.Avalonia.IngredientParsing;

public class IngredientParserService(IFoodService foodService)
{
    const double MinimumMatchThreshold = 0.6;

    private static readonly Regex s_ingredientPattern = new(
        @"^(?<qty>\d[\d\s]*(?:[./]\d+)?)\s*" +
        @"(?:(?<unit>g|kg|ml|l|cups?|tbsp|tsp|tablespoons?|teaspoons?|oz|lbs?|pounds?|kilograms?|grams?|liters?|litres?|milliliters?|millilitres?|drizzle|pack|pinch|twin\s*pack|medium|large|small|head|can|clove|bunch|stalk|slice|piece|sheet)\b)?\s*" +
        @"(?:of\s+)?" +
        @"(?<food>.+)$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    private static readonly Dictionary<string, string> s_unitNormalizationMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "g",            "g"       }, { "gram",         "g"       }, { "grams",        "g"       },
        { "kg",           "kg"      }, { "kilogram",      "kg"      }, { "kilograms",     "kg"      },
        { "ml",           "ml"      }, { "milliliter",    "ml"      }, { "milliliters",   "ml"      },
        { "millilitre",   "ml"      }, { "millilitres",   "ml"      },
        { "l",            "l"       }, { "liter",         "l"       }, { "liters",        "l"       },
        { "litre",        "l"       }, { "litres",        "l"       },
        { "cup",          "cup"     }, { "cups",          "cup"     }, { "c",             "cup"     },
        { "tbsp",         "tbsp"    }, { "tablespoon",    "tbsp"    }, { "tablespoons",   "tbsp"    }, { "tbl", "tbsp" },
        { "tsp",          "tsp"     }, { "teaspoon",      "tsp"     }, { "teaspoons",     "tsp"     },
        { "oz",           "oz"      }, { "ounce",         "oz"      }, { "ounces",        "oz"      },
        { "lb",           "lb"      }, { "lbs",           "lb"      }, { "pound",         "lb"      }, { "pounds", "lb" },
        { "drizzle",      "drizzle" },
        { "pinch",        "pinch"   },
        { "pack",         "pack"    },
        { "twin pack",    "pack"    },
        { "medium",       "medium"  },
        { "large",        "large"   },
        { "small",        "small"   },
        { "head",         "head"    },
        { "can",          "can"     },
        { "clove",        "clove"   }, { "cloves",  "clove"  },
        { "bunch",        "bunch"   }, { "bunches", "bunch"  },
        { "stalk",        "stalk"   }, { "stalks",  "stalk"  },
        { "slice",        "slice"   }, { "slices",  "slice"  },
        { "piece",        "piece"   }, { "pieces",  "piece"  },
        { "sheet",        "sheet"   }, { "sheets",  "sheet"  },
    };

    public List<ParsedIngredientMatch> ParseAndMatchIngredients(string ingredientLines)
    {
        var results = new List<ParsedIngredientMatch>();

        if (string.IsNullOrWhiteSpace(ingredientLines))
        {
            return results;
        }

        foreach (string rawLine in SplitLines(ingredientLines))
        {
            ParsedIngredient? parsed = ParseIngredient(rawLine);
            if (parsed == null)
            {
                continue;
            }

            FoodMatchResult match = foodService.FindBestMatch(parsed.FoodDescription);

            if (match.Score >= MinimumMatchThreshold)
            {
                ParsedIngredientMatch? duplicateMatch = results.FirstOrDefault(im =>
                    im.MatchedFood is not null &&
                    match.Food is not null &&
                    im.MatchedFood!.Id == match.Food.Id &&
                    string.Equals(im.ParsedIngredient.CanonicalUnit, parsed.CanonicalUnit, StringComparison.OrdinalIgnoreCase));

                if (duplicateMatch is not null)
                {
                    duplicateMatch.ParsedIngredient.Quantity += parsed.Quantity;
                    continue;
                }
            }

            int matchDistance = match.Method == "Exact"
                ? 0
                : match.IsMatch
                    ? (int)Math.Round((1.0 - match.Score) * Math.Max(parsed.FoodDescription.Length, match.Food?.Name.Length ?? 1))
                    : -1;

            results.Add(new ParsedIngredientMatch
            {
                ParsedIngredient = parsed,
                MatchedFood      = match.Food,
                MatchScore       = match.IsMatch ? match.Score : 0,
                MatchDistance    = matchDistance,
                MatchMethod      = match.Method,
                IsMatched        = match.Score >= MinimumMatchThreshold
            });
        }

        return results;
    }

    private static ParsedIngredient? ParseIngredient(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return null;
        }

        if (line.TrimStart().StartsWith('#'))
        {
            return null;
        }

        if (Regex.IsMatch(line.Trim(), @"^[\d\s./]+$"))
        {
            return null;
        }

        string cleaned = Regex.Replace(line.Trim(), @",\s*.+$", "").Trim();
        if (string.IsNullOrWhiteSpace(cleaned))
        {
            return null;
        }

        Match m = s_ingredientPattern.Match(cleaned);

        double quantity = 1.0;
        string unitRaw = string.Empty;
        string foodDescription;

        if (m.Success)
        {
            string qtyGroup = m.Groups["qty"].Value.Trim();
            unitRaw = m.Groups["unit"].Value.Trim();
            foodDescription = m.Groups["food"].Value.Trim();

            if (!string.IsNullOrWhiteSpace(qtyGroup))
            {
                quantity = ParseQuantity(qtyGroup);
            }

            if (string.IsNullOrWhiteSpace(foodDescription) && !string.IsNullOrWhiteSpace(unitRaw))
            {
                foodDescription = unitRaw;
                unitRaw = string.Empty;
            }

            if (string.IsNullOrWhiteSpace(foodDescription))
            {
                foodDescription = cleaned;
            }
        }
        else
        {
            foodDescription = cleaned;
        }

        if (string.IsNullOrWhiteSpace(foodDescription))
        {
            return null;
        }

        string canonicalUnit = s_unitNormalizationMap.TryGetValue(unitRaw, out string? mapped)
            ? mapped
            : unitRaw.ToLowerInvariant();

        return new ParsedIngredient
        {
            RawLine         = line,
            Quantity        = quantity,
            Unit            = canonicalUnit,
            CanonicalUnit   = canonicalUnit,
            FoodDescription = foodDescription
        };
    }

    private static double ParseQuantity(string qty)
    {
        double result = 0;
        foreach (string part in qty.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (part.Contains('/'))
            {
                string[] fracParts = part.Split('/');
                if (fracParts.Length == 2
                    && double.TryParse(fracParts[0], out double num)
                    && double.TryParse(fracParts[1], out double den)
                    && den != 0)
                {
                    result += num / den;
                }
            }
            else if (double.TryParse(part, out double d))
            {
                result += d;
            }
        }
        return result == 0 ? 1.0 : result;
    }

    private static IEnumerable<string> SplitLines(string text) =>
        text.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .Where(l => l.Length > 0);
}
