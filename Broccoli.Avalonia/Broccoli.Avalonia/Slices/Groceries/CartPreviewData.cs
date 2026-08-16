namespace Broccoli.Avalonia.Slices.Groceries;

public sealed class CartPreviewData
{
    public string DisplayName { get; init; } = string.Empty;

    public string FormattedLine { get; init; } = string.Empty;

    public string FoodName { get; init; } = string.Empty;

    public string OriginalLine { get; init; } = string.Empty;

    public bool IsMerge { get; init; }
}
