using Avalonia.Media;

namespace Broccoli.Avalonia.Slices.Recipes;

/// <summary>
/// Static metadata for <see cref="RecipeListColumn"/>: display titles, fixed column widths so the
/// table header and rows stay aligned, text alignment, and serialization to/from the stored
/// comma-separated setting.
/// </summary>
internal static class RecipeListColumnDefinitions
{
    /// <summary>The canonical order used when nothing is stored yet.</summary>
    public static readonly RecipeListColumn[] DefaultOrder =
    [
        RecipeListColumn.Name,
        RecipeListColumn.CookingTime,
        RecipeListColumn.Servings,
        RecipeListColumn.Source,
        RecipeListColumn.DateAdded,
        RecipeListColumn.Calories,
        RecipeListColumn.Protein,
        RecipeListColumn.Carbs,
        RecipeListColumn.Fat,
    ];

    public static string Title(RecipeListColumn column) => column switch
    {
        RecipeListColumn.Name => "Name",
        RecipeListColumn.CookingTime => "Cooking Time",
        RecipeListColumn.Servings => "Servings",
        RecipeListColumn.Source => "Source",
        RecipeListColumn.DateAdded => "Date Added",
        RecipeListColumn.Calories => "Calories",
        RecipeListColumn.Protein => "Protein",
        RecipeListColumn.Carbs => "Carbs",
        RecipeListColumn.Fat => "Fat",
        _ => column.ToString(),
    };

    /// <summary>
    /// Fixed pixel width so the header and row cells line up. Name is widest; the rest are
    /// sized for their short values.
    /// </summary>
    public static double Width(RecipeListColumn column) => column switch
    {
        RecipeListColumn.Name => 220,
        RecipeListColumn.CookingTime => 100,
        RecipeListColumn.Servings => 80,
        RecipeListColumn.Source => 160,
        RecipeListColumn.DateAdded => 100,
        RecipeListColumn.Calories => 80,
        RecipeListColumn.Protein => 80,
        RecipeListColumn.Carbs => 80,
        RecipeListColumn.Fat => 80,
        _ => 100,
    };

    public static TextAlignment Alignment(RecipeListColumn column) =>
        column is RecipeListColumn.Calories
            or RecipeListColumn.Protein
            or RecipeListColumn.Carbs
            or RecipeListColumn.Fat
            or RecipeListColumn.CookingTime
            or RecipeListColumn.Servings
            ? TextAlignment.Right
            : TextAlignment.Left;

    /// <summary>Serializes columns as their enum names joined by commas (their order = display order).</summary>
    public static string Serialize(IEnumerable<RecipeListColumn> columns) =>
        string.Join(",", columns.Select(column => column.ToString()));

    /// <summary>
    /// Parses the stored comma-separated column list. Unknown/missing names are dropped and the
    /// result falls back to <see cref="DefaultOrder"/> when nothing valid is left.
    /// </summary>
    public static RecipeListColumn[] Parse(string? serialized)
    {
        if (string.IsNullOrWhiteSpace(serialized))
        {
            return DefaultOrder;
        }

        HashSet<string> known = DefaultOrder.Select(column => column.ToString()).ToHashSet(StringComparer.Ordinal);
        List<RecipeListColumn> parsed = serialized
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(name => known.Contains(name))
            .Select(name => Enum.Parse<RecipeListColumn>(name, ignoreCase: true))
            .ToList();

        return parsed.Count > 0 ? parsed.ToArray() : DefaultOrder;
    }
}
