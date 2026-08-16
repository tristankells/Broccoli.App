using Broccoli.Avalonia.IngredientParsing;
using Broccoli.Avalonia.Shared;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Broccoli.Avalonia.Slices.Recipes;

public partial class ParsedIngredientRow : ViewModelBase
{
    [ObservableProperty]
    private bool _isMatched;
    [ObservableProperty]
    private string _foodName = string.Empty;
    [ObservableProperty]
    private string _quantityDisplay = string.Empty;
    [ObservableProperty]
    private string _caloriesText = string.Empty;
    [ObservableProperty]
    private string _proteinText = string.Empty;
    [ObservableProperty]
    private string _carbsText = string.Empty;
    [ObservableProperty]
    private string _fatText = string.Empty;

    [ObservableProperty]
    private bool _isTopCalories;
    [ObservableProperty]
    private bool _isTopProtein;
    [ObservableProperty]
    private bool _isTopCarbs;
    [ObservableProperty]
    private bool _isTopFat;

    public static ParsedIngredientRow FromMatch(ParsedIngredientMatch match)
    {
        return new ParsedIngredientRow
        {
            IsMatched = match.IsMatched,
            FoodName = match.IsMatched
                ? match.MatchedFood!.Name
                : match.ParsedIngredient.FoodDescription,
            QuantityDisplay = match.GetQuantityDisplay(),
            CaloriesText = match.IsMatched ? $"{match.GetCalories():0} kcal" : "—",
            ProteinText = match.IsMatched ? $"P:{match.GetProtein():0.0}g" : "—",
            CarbsText = match.IsMatched ? $"C:{match.GetCarbohydrates():0.0}g" : "—",
            FatText = match.IsMatched ? $"F:{match.GetFat():0.0}g" : "—",
        };
    }
}
