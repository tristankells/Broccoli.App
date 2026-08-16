using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Broccoli.Avalonia.Models;

namespace Broccoli.Avalonia.Slices.Recipes.Import;

public class PaprikaHtmlImportFormat : IImportFormat
{
    public string DisplayName => "Paprika — HTML Export";

    public string FileExtension => ".html";

    public IReadOnlyList<string> ExportInstructions => new[]
    {
        "Open Paprika Recipe Manager.",
        "File → Export → HTML.",
        "Select the recipes you want to export.",
        "Save the HTML file.",
        "Drag the file here, or click to browse.",
    };

    public async Task<Recipe> ParseAsync(string fileContent)
    {
        var parser = new HtmlParser();
        IHtmlDocument document = await parser.ParseDocumentAsync(fileContent);

        string name = document.QuerySelector("[itemprop=\"name\"]")?.TextContent.Trim()
            ?? throw new InvalidOperationException("Could not find recipe name.");

        string ingredients = string.Join(
            "\n",
            document.QuerySelectorAll("[itemprop=\"recipeIngredient\"]")
                .Select(el => el.TextContent.Trim()));

        IHtmlCollection<IElement> instructionNodes = document.QuerySelectorAll("[itemprop=\"recipeInstructions\"] p");
        string directions = string.Join("\n\n", instructionNodes.Select(p => GetPlainText(p)));

        string? notes = document.QuerySelector("[itemprop=\"comment\"]")?.TextContent.Trim();

        IElement? categoriesEl = document.QuerySelector(".categories");
        List<string> tags = categoriesEl?.TextContent
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList() ?? new List<string>();

        int? servings = null;
        int? cookTime = null;

        IElement? metadata = document.QuerySelector(".metadata");
        if (metadata is not null)
        {
            foreach (IElement b in metadata.QuerySelectorAll("b"))
            {
                string label = b.TextContent.Trim().ToLowerInvariant();
                string? value = b.NextSibling?.TextContent.Trim();
                if (label.Contains("serving") && int.TryParse(value, out int s))
                {
                    servings = s;
                }

                if (label.Contains("cook") && int.TryParse(value, out int ct))
                {
                    cookTime = ct;
                }
            }
        }

        string? source = document.QuerySelector("[itemprop=\"author\"]")?.TextContent.Trim();

        return new Recipe
        {
            Name = name,
            Ingredients = ingredients,
            Directions = directions,
            Notes = notes,
            Tags = tags,
            Servings = servings,
            CookTimeMinutes = cookTime,
            Source = source,
        };
    }

    private static string GetPlainText(INode node)
    {
        if (node is IText text)
        {
            return text.TextContent;
        }

        if (node.NodeName.Equals("BR", StringComparison.OrdinalIgnoreCase))
        {
            return "\n";
        }

        return string.Join(string.Empty, node.ChildNodes.Select(GetPlainText));
    }
}
