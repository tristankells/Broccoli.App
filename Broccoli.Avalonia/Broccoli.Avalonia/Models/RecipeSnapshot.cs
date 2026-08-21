namespace Broccoli.Avalonia.Models;

/// <summary>
/// An immutable capture of a recipe's content at a point in time, used for the ingredient
/// history feature. Stores the content fields that can change while a user edits a recipe;
/// intentionally excludes <see cref="Recipe.Images"/> and favourite status, which are not
/// part of the "what did this recipe originally contain" story.
/// </summary>
public class RecipeSnapshot
{
    /// <summary>Unique id of this snapshot.</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>The id of the recipe this snapshot belongs to.</summary>
    public string RecipeId { get; set; } = string.Empty;

    /// <summary>UTC timestamp at which this snapshot was captured.</summary>
    public DateTime CapturedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Local-time display string for when this snapshot was captured.</summary>
    public string CapturedAtDisplay => CapturedAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm", System.Globalization.CultureInfo.InvariantCulture);

    public string Name { get; set; } = string.Empty;

    public string Ingredients { get; set; } = string.Empty;

    public string Directions { get; set; } = string.Empty;

    public string? Notes { get; set; }

    public int? Servings { get; set; }

    public int? PrepTimeMinutes { get; set; }

    public int? CookTimeMinutes { get; set; }

    public string? Source { get; set; }

    public string? Url { get; set; }

    public List<string> Tags { get; set; } = new();

    /// <summary>Creates a snapshot from a recipe's current content.</summary>
    public static RecipeSnapshot FromRecipe(Recipe recipe, DateTime capturedAtUtc) => new()
    {
        RecipeId = recipe.Id,
        CapturedAtUtc = capturedAtUtc,
        Name = recipe.Name,
        Ingredients = recipe.Ingredients,
        Directions = recipe.Directions,
        Notes = recipe.Notes,
        Servings = recipe.Servings,
        PrepTimeMinutes = recipe.PrepTimeMinutes,
        CookTimeMinutes = recipe.CookTimeMinutes,
        Source = recipe.Source,
        Url = recipe.Url,
        Tags = new List<string>(recipe.Tags),
    };
}
