using System.Text;
using System.Text.RegularExpressions;
using Broccoli.Data.Models;

namespace Broccoli.App.Shared.Slices.Recipes.Import;

/// <summary>
/// Parses plain text copied from a Bargain Box meal-kit cookbook page.
/// The user should copy from the top of the "Ingredients" section down to
/// the bottom of the "Nutritional Information" block and paste it here.
/// </summary>
public class BargainBoxPasteImportFormat : IImportFormat
{
    public string DisplayName => "Bargain Box (Paste)";

    /// <summary>Not used for paste-based formats — the dialog shows a textarea instead.</summary>
    public string FileExtension => ".txt";

    public bool IsPasteBased => true;

    public IReadOnlyList<string> ExportInstructions =>
    [
        "Open the Bargain Box website and navigate to your weekly meal cookbook page.",
        "Find the recipe you want to import.",
        "Click at the very start of the Ingredients list (just before the first ingredient group name or first ingredient).",
        "Scroll to the bottom of the Nutritional Information table (the last row is usually 'Sodium').",
        "Click-and-drag, or shift-click, to select all text from the first ingredient down to and including the last nutrition row.",
        "Copy the selected text (Ctrl+C / Cmd+C).",
        "Paste it into the text area below."
    ];

    // Matches a standalone step number on its own line, e.g. "1", "2", "12"
    private static readonly Regex s_stepNumberLine = new(@"^\d+$", RegexOptions.Compiled);

    // ALL-CAPS step name, e.g. "PREP IT", "BOIL IT", "COOK IT"
    private static readonly Regex s_stepNameLine = new(@"^[A-Z][A-Z\s/]+$", RegexOptions.Compiled);

    // Inline allergen note, e.g. "(Contains milk)" or "(Contains sesame)"
    private static readonly Regex s_inlineAllergen = new(@"^\(Contains\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Allergen footer lines
    private static readonly Regex s_allergenFooter = new(
        @"^(Contains |May contain |This is subject|\^ pantry staples)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public Task<Recipe> ParseAsync(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new InvalidOperationException(
                "No text was pasted. Please copy the recipe text from the Bargain Box page and paste it above.");

        var allLines = content
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
            .Select(l => l.TrimEnd())
            .ToList();

        // ── 1. Locate nutrition boundary ────────────────────────────────────────
        int nutritionStart = -1;
        int? servings = null;

        for (int i = 0; i < allLines.Count; i++)
        {
            if (string.Equals(allLines[i].Trim(), "Serving size", StringComparison.OrdinalIgnoreCase))
            {
                nutritionStart = i;
                // Next non-empty line should be the serving count
                for (int j = i + 1; j < allLines.Count; j++)
                {
                    var candidate = allLines[j].Trim();
                    if (string.IsNullOrWhiteSpace(candidate)) continue;
                    if (int.TryParse(candidate, out int sv))
                        servings = sv;
                    break;
                }
                break;
            }
        }

        // Work only with the lines before the nutrition block
        var workLines = nutritionStart >= 0
            ? allLines.Take(nutritionStart).ToList()
            : allLines;

        // ── 2. Locate directions start ──────────────────────────────────────────
        // Directions begin at a lone digit line whose next non-empty line is ALL CAPS.
        int directionsStart = -1;
        for (int i = 0; i < workLines.Count - 1; i++)
        {
            if (!s_stepNumberLine.IsMatch(workLines[i].Trim())) continue;

            // Find the next non-empty line
            for (int j = i + 1; j < workLines.Count; j++)
            {
                if (string.IsNullOrWhiteSpace(workLines[j])) continue;
                if (s_stepNameLine.IsMatch(workLines[j].Trim()))
                    directionsStart = i;
                break;
            }

            if (directionsStart >= 0) break;
        }

        var ingredientLines = directionsStart >= 0
            ? workLines.Take(directionsStart).ToList()
            : workLines;

        var directionLines = directionsStart >= 0
            ? workLines.Skip(directionsStart).ToList()
            : new List<string>();

        // ── 3. Trim allergen footer from ingredient block ──────────────────────
        int allergenFooterStart = -1;
        for (int i = 0; i < ingredientLines.Count; i++)
        {
            var trimmed = ingredientLines[i].Trim();
            if (s_allergenFooter.IsMatch(trimmed))
            {
                allergenFooterStart = i;
                break;
            }
        }

        if (allergenFooterStart >= 0)
            ingredientLines = ingredientLines.Take(allergenFooterStart).ToList();

        // ── 4. Build cleaned ingredients string ────────────────────────────────
        var ingredientBuilder = new StringBuilder();
        foreach (var rawLine in ingredientLines)
        {
            var line = rawLine.Trim();

            if (string.IsNullOrWhiteSpace(line)) continue;

            // Skip inline allergen notes like "(Contains milk)"
            if (s_inlineAllergen.IsMatch(line)) continue;

            // Skip the "^ pantry staples" key line
            if (line.Equals("^ pantry staples", StringComparison.OrdinalIgnoreCase)) continue;

            // Strip trailing " ^" (pantry staple marker)
            if (line.EndsWith(" ^"))
                line = line[..^2].TrimEnd();

            // Determine if this is a section heading (no leading digit → not an ingredient quantity)
            // A heading is a line that does not start with a digit and is not an ingredient
            bool isHeading = !Regex.IsMatch(line, @"^\d") && !IsIngredientLine(line);

            if (isHeading)
            {
                ingredientBuilder.AppendLine($"# {line}");
            }
            else
            {
                ingredientBuilder.AppendLine(line);
            }
        }

        string ingredients = ingredientBuilder.ToString().TrimEnd();

        // ── 5. Build directions string ─────────────────────────────────────────
        string directions = BuildDirections(directionLines);

        if (string.IsNullOrWhiteSpace(ingredients) && string.IsNullOrWhiteSpace(directions))
            throw new InvalidOperationException(
                "Could not find any ingredients or directions in the pasted text. " +
                "Make sure you copied from the beginning of the Ingredients section.");

        return Task.FromResult(new Recipe
        {
            Name        = string.Empty,
            Ingredients = ingredients,
            Directions  = directions,
            Servings    = servings,
            Source      = "Bargain Box",
            Tags        = ["bargain box"],
        });
    }

    /// <summary>
    /// A line is treated as an ingredient (not a heading) if it starts with a digit
    /// or a fraction, or if it begins with a quantity pattern like "900g", "1 pack", etc.
    /// </summary>
    private static bool IsIngredientLine(string line) =>
        Regex.IsMatch(line, @"^\d") ||
        Regex.IsMatch(line, @"^\d+\s*/\s*\d+");   // e.g. "1/2 cup ..."

    /// <summary>
    /// Converts the raw direction lines into a markdown-formatted directions string.
    /// Each step is rendered as: ## N. STEP NAME\n\nbody text
    /// </summary>
    private static string BuildDirections(IList<string> lines)
    {
        if (lines.Count == 0) return string.Empty;

        var steps = new List<(int Number, string Name, List<string> Body)>();
        int? currentNumber = null;
        string currentName = string.Empty;
        var currentBody = new List<string>();

        for (int i = 0; i < lines.Count; i++)
        {
            var line = lines[i].Trim();

            if (string.IsNullOrWhiteSpace(line)) continue;

            if (s_stepNumberLine.IsMatch(line))
            {
                // Save current step if we have one
                if (currentNumber.HasValue)
                    steps.Add((currentNumber.Value, currentName, currentBody));

                currentNumber = int.Parse(line);
                currentName = string.Empty;
                currentBody = new List<string>();
                continue;
            }

            if (currentNumber.HasValue && string.IsNullOrEmpty(currentName) && s_stepNameLine.IsMatch(line))
            {
                currentName = line;
                continue;
            }

            // Strip lines in brackets about doubled protein variants (meal-kit specific)
            if (line.StartsWith('[') && line.EndsWith(']')) continue;

            if (currentNumber.HasValue)
                currentBody.Add(line);
        }

        // Flush last step
        if (currentNumber.HasValue)
            steps.Add((currentNumber.Value, currentName, currentBody));

        if (steps.Count == 0)
        {
            // No step numbers found — return the lines as-is
            return string.Join("\n", lines.Where(l => !string.IsNullOrWhiteSpace(l)));
        }

        var sb = new StringBuilder();
        foreach (var (number, name, body) in steps)
        {
            if (sb.Length > 0) sb.AppendLine();

            var heading = string.IsNullOrWhiteSpace(name)
                ? $"## {number}."
                : $"## {number}. {name}";

            sb.AppendLine(heading);
            sb.AppendLine();
            sb.AppendLine(string.Join("\n", body));
        }

        return sb.ToString().TrimEnd();
    }
}
