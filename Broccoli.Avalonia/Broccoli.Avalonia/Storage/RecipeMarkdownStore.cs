using Broccoli.Avalonia.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Broccoli.Avalonia.Storage;

/// <summary>
/// Reads/writes <see cref="Recipe"/> as a human-readable Markdown file with a YAML
/// frontmatter block for structured fields, stored one-folder-per-recipe alongside its
/// images (see <see cref="AppPaths.RecipeFolder"/>). This keeps recipes easy to read/edit
/// outside the app and cheap to back up incrementally (only changed files re-sync).
/// </summary>
public interface IRecipeMarkdownStore
{
    /// <summary>Loads every recipe found under the Recipes folder.</summary>
    IReadOnlyList<Recipe> LoadAll();

    /// <summary>Loads a single recipe by id, or null if it doesn't exist.</summary>
    Recipe? Load(string recipeId);

    /// <summary>Writes (creates or overwrites) a recipe's Markdown file.</summary>
    void Save(Recipe recipe);

    /// <summary>Deletes a recipe's entire folder (Markdown file + images).</summary>
    void Delete(string recipeId);

    /// <summary>
    /// Copies an image file into the recipe's folder and returns the stored filename
    /// to append to <see cref="Recipe.Images"/>.
    /// </summary>
    string AddImage(string recipeId, string sourceFilePath);

    /// <summary>Deletes a previously-added image file from the recipe's folder.</summary>
    void RemoveImage(string recipeId, string fileName);
}

public class RecipeMarkdownStore : IRecipeMarkdownStore
{
    private const string FrontmatterDelimiter = "---";

    private static readonly ISerializer YamlSerializer = new SerializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();

    private static readonly IDeserializer YamlDeserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    public IReadOnlyList<Recipe> LoadAll()
    {
        var recipes = new List<Recipe>();

        foreach (string folder in Directory.EnumerateDirectories(AppPaths.RecipesFolder))
        {
            string recipeId = Path.GetFileName(folder);
            Recipe? recipe = Load(recipeId);
            if (recipe is not null)
            {
                recipes.Add(recipe);
            }
        }

        return recipes;
    }

    public Recipe? Load(string recipeId)
    {
        string path = AppPaths.RecipeMarkdownFilePath(recipeId);
        if (!File.Exists(path))
        {
            return null;
        }

        string content = File.ReadAllText(path);
        (string? frontmatter, string? body) = SplitFrontmatter(content);

        RecipeFrontmatter meta = YamlDeserializer.Deserialize<RecipeFrontmatter>(frontmatter) ?? new RecipeFrontmatter();
        (string? ingredients, string? directions, string? notes) = ParseSections(body);

        return new Recipe
        {
            Id = string.IsNullOrWhiteSpace(meta.Id) ? recipeId : meta.Id,
            Name = meta.Name,
            Ingredients = ingredients,
            Directions = directions,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes,
            Servings = meta.Servings,
            PrepTimeMinutes = meta.PrepTimeMinutes,
            CookTimeMinutes = meta.CookTimeMinutes,
            Source = meta.Source,
            Url = meta.Url,
            Tags = meta.Tags ?? new List<string>(),
            Images = meta.Images ?? new List<string>(),
            CreatedAt = meta.CreatedAt,
            UpdatedAt = meta.UpdatedAt,
            IsFavorite = meta.IsFavorite
        };
    }

    public void Save(Recipe recipe)
    {
        var meta = new RecipeFrontmatter
        {
            Id = recipe.Id,
            Name = recipe.Name,
            Servings = recipe.Servings,
            PrepTimeMinutes = recipe.PrepTimeMinutes,
            CookTimeMinutes = recipe.CookTimeMinutes,
            Source = recipe.Source,
            Url = recipe.Url,
            Tags = recipe.Tags,
            Images = recipe.Images,
            CreatedAt = recipe.CreatedAt,
            UpdatedAt = recipe.UpdatedAt,
            IsFavorite = recipe.IsFavorite
        };

        string yaml = YamlSerializer.Serialize(meta);

        string body = $"""
## Ingredients

{recipe.Ingredients.Trim()}

## Directions

{recipe.Directions.Trim()}

## Notes

{(recipe.Notes ?? string.Empty).Trim()}
""";

        string content = $"{FrontmatterDelimiter}\n{yaml}{FrontmatterDelimiter}\n\n{body}\n";

        File.WriteAllText(AppPaths.RecipeMarkdownFilePath(recipe.Id), content);
    }

    public void Delete(string recipeId)
    {
        string folder = AppPaths.RecipeFolder(recipeId);
        if (Directory.Exists(folder))
        {
            Directory.Delete(folder, recursive: true);
        }

        // Record the deletion so Google Drive sync propagates it to other devices instead of
        // treating this device as merely "missing" a recipe that should be re-downloaded.
        TombstoneStore.RecordDeletion(recipeId);
    }

    public string AddImage(string recipeId, string sourceFilePath)
    {
        string fileName = Path.GetFileName(sourceFilePath);
        string destination = Path.Combine(AppPaths.RecipeFolder(recipeId), fileName);
        File.Copy(sourceFilePath, destination, overwrite: true);
        return fileName;
    }

    public void RemoveImage(string recipeId, string fileName)
    {
        string path = Path.Combine(AppPaths.RecipeFolder(recipeId), fileName);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private static (string Frontmatter, string Body) SplitFrontmatter(string content)
    {
        string[] lines = content.Replace("\r\n", "\n").Split('\n');
        if (lines.Length == 0 || lines[0].Trim() != FrontmatterDelimiter)
        {
            return (string.Empty, content);
        }

        int endIndex = Array.FindIndex(lines, 1, l => l.Trim() == FrontmatterDelimiter);
        if (endIndex < 0)
        {
            return (string.Empty, content);
        }

        string frontmatter = string.Join('\n', lines[1..endIndex]);
        string body = string.Join('\n', lines[(endIndex + 1)..]);
        return (frontmatter, body);
    }

    private static (string Ingredients, string Directions, string Notes) ParseSections(string body)
    {
        string ingredients = string.Empty, directions = string.Empty, notes = string.Empty;
        string? current = null;
        var buffer = new System.Text.StringBuilder();

        void Flush()
        {
            string text = buffer.ToString().Trim();
            switch (current)
            {
                case "ingredients": ingredients = text; break;
                case "directions": directions = text; break;
                case "notes": notes = text; break;
            }
            buffer.Clear();
        }

        foreach (string line in body.Replace("\r\n", "\n").Split('\n'))
        {
            string trimmed = line.Trim();
            if (trimmed.Equals("## Ingredients", StringComparison.OrdinalIgnoreCase))
            {
                Flush();
                current = "ingredients";
            }
            else if (trimmed.Equals("## Directions", StringComparison.OrdinalIgnoreCase))
            {
                Flush();
                current = "directions";
            }
            else if (trimmed.Equals("## Notes", StringComparison.OrdinalIgnoreCase))
            {
                Flush();
                current = "notes";
            }
            else
            {
                buffer.AppendLine(line);
            }
        }

        Flush();
        return (ingredients, directions, notes);
    }

    /// <summary>YAML-serializable subset of <see cref="Recipe"/> stored in the frontmatter block.</summary>
    private sealed class RecipeFrontmatter
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int? Servings { get; set; }
        public int? PrepTimeMinutes { get; set; }
        public int? CookTimeMinutes { get; set; }
        public string? Source { get; set; }
        public string? Url { get; set; }
        public List<string> Tags { get; set; } = new();
        public List<string> Images { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsFavorite { get; set; }
    }
}
