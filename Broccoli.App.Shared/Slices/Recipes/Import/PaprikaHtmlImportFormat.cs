using AngleSharp;
using AngleSharp.Dom;
using Broccoli.Data.Models;
using System.Text;

namespace Broccoli.App.Shared.Slices.Recipes.Import;

/// <summary>
/// Parses a Paprika recipe manager HTML export file into a Recipe.
/// Uses AngleSharp CSS selectors against the Paprika Schema.org/itemprop structure.
/// </summary>
public class PaprikaHtmlImportFormat : IImportFormat
{
    public string DisplayName => "Paprika — HTML Export";
    public string FileExtension => ".html";

    public IReadOnlyList<string> ExportInstructions =>
    [
        "Open Paprika on your device.",
        "Select the recipes you want to export (or use Edit ? Select All).",
        "Tap the Share / Export button.",
        "Choose \"HTML\" as the export format.",
        "Save or share the exported .html file(s) to your device.",
        "Drop the file(s) into the box below."
    ];

    public async Task<Recipe> ParseAsync(string fileContent)
    {
        var config = AngleSharp.Configuration.Default;
        var context = BrowsingContext.New(config);
        var document = await context.OpenAsync(req => req.Content(fileContent));

        // Name — required field; throw if missing
        var name = document.QuerySelector("[itemprop=\"name\"]")?.TextContent.Trim();
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException(
                "Could not find a recipe name in this file. " +
                "Please make sure it is a valid Paprika HTML export.");

        // Ingredients — one <p itemprop="recipeIngredient"> per line
        var ingredientNodes = document.QuerySelectorAll("[itemprop=\"recipeIngredient\"]");
        var ingredients = string.Join("\n",
            ingredientNodes
                .Select(n => n.TextContent.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s)));

        // Directions — paragraphs inside [itemprop="recipeInstructions"]
        var directionsEl = document.QuerySelector("[itemprop=\"recipeInstructions\"]");
        var directionParagraphs = directionsEl?
            .QuerySelectorAll("p")
            .Select(GetPlainText)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList() ?? [];
        var directions = string.Join("\n\n", directionParagraphs);

        // Notes — paragraphs inside [itemprop="comment"]
        var notesEl = document.QuerySelector("[itemprop=\"comment\"]");
        var notesParagraphs = notesEl?
            .QuerySelectorAll("p")
            .Select(GetPlainText)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList() ?? [];
        var notes = notesParagraphs.Count > 0 ? string.Join("\n", notesParagraphs) : null;

        // Tags — comma-separated .categories text
        var categoriesText = document.QuerySelector(".categories")?.TextContent.Trim() ?? string.Empty;
        var tags = categoriesText
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim())
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .ToList();

        // Servings and Total Time — parsed from <b> labels inside .metadata
        int? servings = null;
        int? cookTimeMinutes = null;
        var metadataEl = document.QuerySelector(".metadata");
        if (metadataEl is not null)
        {
            foreach (var b in metadataEl.QuerySelectorAll("b"))
            {
                var label = b.TextContent.Trim().ToLowerInvariant();
                var valueText = b.NextElementSibling?.TextContent.Trim() ?? string.Empty;

                if (label.StartsWith("servings") && int.TryParse(valueText, out var sv))
                    servings = sv;
                else if (label.StartsWith("total time") && int.TryParse(valueText, out var tt))
                    cookTimeMinutes = tt;
            }
        }

        // Source / Author
        var source = document.QuerySelector("[itemprop=\"author\"]")?.TextContent.Trim();

        return new Recipe
        {
            Name = name,
            Ingredients = ingredients,
            Directions = directions,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes,
            Tags = tags,
            Servings = servings,
            CookTimeMinutes = cookTimeMinutes,
            Source = string.IsNullOrWhiteSpace(source) ? null : source,
            Images = []
        };
    }

    /// <summary>
    /// Extracts plain text from an element, converting &lt;br&gt; to newlines
    /// and preserving entity-decoded text from child text nodes.
    /// </summary>
    private static string GetPlainText(IElement element)
    {
        var sb = new StringBuilder();

        foreach (var node in element.ChildNodes)
        {
            if (node.NodeType == NodeType.Text)
            {
                sb.Append(node.TextContent);
            }
            else if (node is IElement child)
            {
                if (child.TagName.Equals("BR", StringComparison.OrdinalIgnoreCase))
                    sb.Append('\n');
                else
                    sb.Append(GetPlainText(child));
            }
        }

        return sb.ToString().Trim();
    }
}


