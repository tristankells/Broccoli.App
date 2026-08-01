using Broccoli.Avalonia.Models;

namespace Broccoli.Avalonia.Slices.Settings;

public class ImportFoodPreviewItem
{
    public Food Incoming { get; init; } = null!;
    public Food? Matched { get; init; }
    public double MatchScore { get; init; }
    public bool IsSelected { get; set; } = true;
    public bool IsNew => Matched == null;
    public List<string> ChangedFields { get; init; } = new();
}
