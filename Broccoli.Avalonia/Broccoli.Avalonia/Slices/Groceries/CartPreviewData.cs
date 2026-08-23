namespace Broccoli.Avalonia.Slices.Groceries;

public sealed class CartPreviewData
{
    public string DisplayName { get; init; } = string.Empty;

    public string FormattedLine { get; init; } = string.Empty;

    public string FoodName { get; init; } = string.Empty;

    public string OriginalLine { get; init; } = string.Empty;

    /// <summary>
    /// Food-match hint shown in brackets for ingredients that matched a food database entry,
    /// e.g. "(~122g Carrot)". Null when the ingredient did not match a food.
    /// </summary>
    public string? FoodMatchHint { get; init; }

    public bool IsMerge { get; init; }
}
