using System.Globalization;
using Broccoli.Avalonia.Models;
using Broccoli.Avalonia.Shared;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Broccoli.Avalonia.Slices.Recipes;

internal partial class RecipeCardViewModel : ViewModelBase
{
    [ObservableProperty]
    private Recipe _recipe = null!;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private bool _isFavorite;

    [ObservableProperty]
    private string _imagePath = string.Empty;

    [ObservableProperty]
    private bool _hasImage;

    [ObservableProperty]
    private string _tags = string.Empty;

    [ObservableProperty]
    private bool _hasTags;

    [ObservableProperty]
    private string _caloriesText = string.Empty;

    [ObservableProperty]
    private string _proteinText = string.Empty;

    [ObservableProperty]
    private string _carbsText = string.Empty;

    [ObservableProperty]
    private string _fatText = string.Empty;

    [ObservableProperty]
    private bool _hasNutrition;

    [ObservableProperty]
    private string _seasonScore = string.Empty;

    [ObservableProperty]
    private string _seasonLabel = string.Empty;

    [ObservableProperty]
    private string _seasonColor = "Gray";

    [ObservableProperty]
    private bool _hasSeasonality;

    [ObservableProperty]
    private bool _hasCalorieMatch;

    [ObservableProperty]
    private bool _calorieMatchInRange;

    [ObservableProperty]
    private string _calorieMatchText = string.Empty;

    [ObservableProperty]
    private HashSet<SearchWord> _searchWords = [];

    public Action<RecipeCardViewModel>? AddToCartRequested { get; set; }

    public static RecipeCardViewModel FromRecipe(
        Recipe recipe,
        string? imagePath,
        double calPerServing,
        double proPerServing,
        double carbPerServing,
        double fatPerServing,
        SeasonalityResult? seasonality,
        double? targetMealCalories = null,
        double tolerancePercent = 15,
        bool showImages = true,
        bool showTags = true,
        bool showSeasonality = true,
        bool showNutrition = true)
    {
        bool hasImage = showImages && !string.IsNullOrEmpty(imagePath);
        bool hasTags = showTags && recipe.Tags.Count > 0;
        bool hasNutrition = showNutrition && (calPerServing > 0 || proPerServing > 0 || carbPerServing > 0 || fatPerServing > 0);
        bool hasSeason = showSeasonality && seasonality?.Label != SeasonalityLabel.Unavailable && seasonality?.Score != null;

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
            SeasonScore = seasonality?.Score is double score ? $"{score:0}" : string.Empty,
            SeasonLabel = seasonality?.Label.ToString() ?? string.Empty,
            SeasonColor = seasonality?.Label switch
            {
                SeasonalityLabel.PeakSeason => "#2ECC71",
                SeasonalityLabel.PartiallyInSeason => "#F39C12",
                SeasonalityLabel.OffSeason => "#E74C3C",
                _ => "Gray",
            },
            HasSeasonality = hasSeason,
            HasCalorieMatch = targetMealCalories.HasValue && targetMealCalories > 0 && hasNutrition,
            CalorieMatchInRange = targetMealCalories.HasValue && targetMealCalories > 0 && hasNutrition
                && Math.Abs(calPerServing - targetMealCalories.Value) / targetMealCalories.Value * 100 <= tolerancePercent,
            CalorieMatchText = targetMealCalories.HasValue && targetMealCalories > 0
                ? Math.Abs(calPerServing - targetMealCalories.Value) / targetMealCalories.Value * 100 <= tolerancePercent ? "=" : "!"
                : string.Empty,
            SearchWords = RetrieveSearchWords(recipe),
        };
    }

    private static HashSet<SearchWord> RetrieveSearchWords(Recipe recipe)
    {
        HashSet<SearchWord> searchWords = [];

        // Split tag into single words
        IEnumerable<SearchWord> tagWords = recipe
            .Tags
            .SelectMany(tag => tag.Split([",", " ", "/", "\\"], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Select(tagWord => tagWord.ToLower(CultureInfo.CurrentCulture))
            .Select(tagWord => new SearchWord(tagWord, SearchWordSource.Tags));

        foreach (SearchWord tagWord in tagWords)
        {
            searchWords.Add(tagWord);
        }

        // Split ingredients into single words
        IEnumerable<SearchWord> ingredientWords = recipe
            .Ingredients
            .Split([",", " ", "/", "\\"], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(tagWord => tagWord.ToLower(CultureInfo.CurrentCulture))
            .Select(tagWord => new SearchWord(tagWord, SearchWordSource.Ingredients));

        foreach (SearchWord tagWord in ingredientWords)
        {
            searchWords.Add(tagWord);
        }

        // Split ingredients into single words
        IEnumerable<SearchWord> titleWords = recipe
            .Name
            .Split([",", " ", "/", "\\"], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(tagWord => tagWord.ToLower(CultureInfo.CurrentCulture))
            .Select(tagWord => new SearchWord(tagWord, SearchWordSource.Title));

        foreach (SearchWord tagWord in titleWords)
        {
            searchWords.Add(tagWord);
        }

        return searchWords;
    }
}
