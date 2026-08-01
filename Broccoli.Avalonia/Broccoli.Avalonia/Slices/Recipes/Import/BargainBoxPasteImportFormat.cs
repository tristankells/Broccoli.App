using System.Text.RegularExpressions;
using Broccoli.Avalonia.Models;

namespace Broccoli.Avalonia.Slices.Recipes.Import;

public class BargainBoxPasteImportFormat : IImportFormat
{
    public string DisplayName => "Bargain Box — Paste";
    public string FileExtension => ".txt";
    public bool IsPasteBased => true;

    public IReadOnlyList<string> ExportInstructions => new[]
    {
        "Open the Bargain Box cookbook page for the recipe.",
        "Select all text (Ctrl+A) and copy (Ctrl+C).",
        "Paste into the text area below.",
        "Click Parse."
    };

    public Task<Recipe> ParseAsync(string fileContent)
    {
        var lines = fileContent.Replace("\r\n", "\n").Split('\n')
            .Select(l => l.Trim()).ToList();

        int servings = 1;
        int servingsIdx = lines.FindIndex(l => l.StartsWith("Serving size", StringComparison.OrdinalIgnoreCase));
        if (servingsIdx >= 0 && servingsIdx + 1 < lines.Count)
        {
            var nextLine = lines[servingsIdx + 1];
            if (int.TryParse(nextLine, out var s)) servings = s;
        }

        int directionsStart = -1;
        for (int i = 0; i < lines.Count - 1; i++)
        {
            if (int.TryParse(lines[i], out _) && lines[i + 1].All(c => char.IsUpper(c) || char.IsWhiteSpace(c)))
            {
                directionsStart = i;
                break;
            }
        }

        var ingredients = new List<string>();
        var directions = new List<string>();
        bool inDirections = false;
        string? currentSection = null;

        foreach (var line in lines)
        {
            if (line.StartsWith("# "))
            {
                ingredients.Add(line);
                continue;
            }

            if (line.Contains("Contains ", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("May contain ", StringComparison.OrdinalIgnoreCase))
                continue;

            if (!string.IsNullOrEmpty(currentSection))
            {
                if (line.StartsWith("## "))
                {
                    directions.Add(line);
                    continue;
                }
                directions.Add(line);
                continue;
            }
        }

        for (int i = 0; i < lines.Count; i++)
        {
            if (i == directionsStart) break;
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;
            if (line.StartsWith("Serving size", StringComparison.OrdinalIgnoreCase)) { i++; continue; }
            if (int.TryParse(line, out _) && i + 1 < lines.Count && lines[i + 1].All(c => char.IsUpper(c) || char.IsWhiteSpace(c)))
                break;

            line = Regex.Replace(line, @"\s*\(Contains [^)]+\)", "");
            line = line.Replace("Contains ", "").Replace("May contain ", "");
            if (line.Contains('^')) line = line.Replace("^", "");
            line = line.Replace("\\s+", " ").Trim();
            if (!string.IsNullOrEmpty(line))
                ingredients.Add(line);
        }

        if (directionsStart >= 0)
        {
            int sectionNum = 0;
            string? sectionTitle = null;
            for (int i = directionsStart; i < lines.Count; i++)
            {
                var line = lines[i];
                if (int.TryParse(line, out _) && i + 1 < lines.Count && lines[i + 1].Length > 0)
                {
                    sectionTitle = lines[++i];
                    continue;
                }
                if (!string.IsNullOrEmpty(sectionTitle))
                {
                    directions.Add($"## {sectionTitle}");
                    sectionTitle = null;
                }
                if (!string.IsNullOrWhiteSpace(line))
                    directions.Add(line);
            }
        }

        return Task.FromResult(new Recipe
        {
            Name = "Imported Recipe",
            Ingredients = string.Join("\n", ingredients),
            Directions = string.Join("\n", directions),
            Servings = servings,
            Source = "Bargain Box",
            Tags = new List<string> { "bargain box" }
        });
    }
}
