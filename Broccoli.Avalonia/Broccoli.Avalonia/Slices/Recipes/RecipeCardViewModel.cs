using Broccoli.Avalonia.Models;
using Broccoli.Avalonia.Shared;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Broccoli.Avalonia.Slices.Recipes;

public partial class RecipeCardViewModel : ViewModelBase
{
    [ObservableProperty] private Recipe _recipe = null!;
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private bool _isFavorite;
    [ObservableProperty] private string _imagePath = string.Empty;
    [ObservableProperty] private bool _hasImage;
    [ObservableProperty] private string _tags = string.Empty;
    [ObservableProperty] private bool _hasTags;
    [ObservableProperty] private string _caloriesText = string.Empty;
    [ObservableProperty] private string _proteinText = string.Empty;
    [ObservableProperty] private string _carbsText = string.Empty;
    [ObservableProperty] private string _fatText = string.Empty;
    [ObservableProperty] private bool _hasNutrition;
    [ObservableProperty] private string _seasonScore = string.Empty;
    [ObservableProperty] private string _seasonLabel = string.Empty;
    [ObservableProperty] private string _seasonColor = "Gray";
    [ObservableProperty] private bool _hasSeasonality;

    public static RecipeCardViewModel FromRecipe(Recipe recipe, string? imagePath,
        double calPerServing, double proPerServing, double carbPerServing, double fatPerServing,
        SeasonalityResult? seasonality)
    {
        var hasImage = !string.IsNullOrEmpty(imagePath);
        var hasTags = recipe.Tags.Count > 0;
        var hasNutrition = calPerServing > 0 || proPerServing > 0 || carbPerServing > 0 || fatPerServing > 0;
        var hasSeason = seasonality?.Label != SeasonalityLabel.Unavailable && seasonality?.Score != null;

        return new RecipeCardViewModel
        {
            Recipe = recipe,
            Name = recipe.Name,
            IsFavorite = recipe.IsFavorite,
            ImagePath = imagePath ?? string.Empty,
            HasImage = hasImage,
            Tags = hasTags ? string.Join(", ", recipe.Tags) : string.Empty,
            HasTags = hasTags,
            CaloriesText = $"{calPerServing:0} kcal",
            ProteinText = $"P:{proPerServing:0.0}g",
            CarbsText = $"C:{carbPerServing:0.0}g",
            FatText = $"F:{fatPerServing:0.0}g",
            HasNutrition = hasNutrition,
            SeasonScore = seasonality?.Score is double s ? $"{s:0}" : string.Empty,
            SeasonLabel = seasonality?.Label.ToString() ?? string.Empty,
            SeasonColor = seasonality?.Label switch
            {
                SeasonalityLabel.PeakSeason => "#2ECC71",
                SeasonalityLabel.PartiallyInSeason => "#F39C12",
                SeasonalityLabel.OffSeason => "#E74C3C",
                _ => "Gray"
            },
            HasSeasonality = hasSeason
        };
    }
}
