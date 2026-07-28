using Broccoli.Data.Models;

namespace Broccoli.App.Shared.Slices.Foods;

public class ImportFoodPreviewItem
{
    public Food Incoming { get; init; } = null!;
    public Food? Matched { get; init; }
    public double MatchScore { get; init; }
    public bool IsSelected { get; set; } = true;
    public bool IsNew => Matched == null;
    public List<string> ChangedFields { get; init; } = new();
}
