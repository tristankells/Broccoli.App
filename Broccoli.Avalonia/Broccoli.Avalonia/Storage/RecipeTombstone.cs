namespace Broccoli.Avalonia.Storage;

/// <summary>A single deleted recipe, so deletions propagate between devices instead of being silently resurrected.</summary>
public class RecipeTombstone
{
    public string RecipeId { get; set; } = string.Empty;

    public DateTime DeletedAtUtc { get; set; }
}
