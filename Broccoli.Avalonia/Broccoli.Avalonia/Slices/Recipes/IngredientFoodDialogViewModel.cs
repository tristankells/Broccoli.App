using System.Collections.ObjectModel;
using Broccoli.Avalonia.IngredientParsing;
using Broccoli.Avalonia.Models;
using Broccoli.Avalonia.Shared;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Broccoli.Avalonia.Slices.Recipes;

public partial class IngredientFoodDialogViewModel : ViewModelBase
{
    private readonly IFoodService _foodService;
    private bool _isNewFood;

    public IngredientFoodDialogViewModel(
        IFoodService foodService,
        string foodDescription,
        IReadOnlyList<FoodMatchResult> matches,
        Food? currentFood)
    {
        _foodService = foodService;
        FoodDescription = foodDescription;

        foreach (FoodMatchResult match in matches)
        {
            if (match.Food is null)
            {
                continue;
            }

            bool isCurrent = string.Equals(
                match.Food.Name, currentFood?.Name, StringComparison.OrdinalIgnoreCase);

            Candidates.Add(new ResolveMatchCandidate(match.Food, match.Score, match.Method, isCurrent));
        }

        SelectFood(currentFood, isNew: false);
    }

    public string FoodDescription { get; }

    public ObservableCollection<ResolveMatchCandidate> Candidates { get; } = new();

    public Action<Food>? FoodSelected { get; set; }

    public Action? RequestClose { get; set; }

    public bool HasSelectedFood => SelectedFood is not null;

    [ObservableProperty]
    private Food? _selectedFood;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _measure = string.Empty;

    [ObservableProperty]
    private double _gramsPerMeasure;

    [ObservableProperty]
    private double _caloriesPer100g;

    [ObservableProperty]
    private double _fatPer100g;

    [ObservableProperty]
    private double _proteinPer100g;

    [ObservableProperty]
    private double _carbohydratesPer100g;

    [ObservableProperty]
    private double _saturatedFatPer100g;

    [ObservableProperty]
    private double _dietaryFiberPer100g;

    [ObservableProperty]
    private double _sugarsPer100g;

    [ObservableProperty]
    private double _sodiumMgPer100g;

    [ObservableProperty]
    private string _matchQualityLabel = string.Empty;

    [ObservableProperty]
    private string _matchQualityColor = "Gray";

    [ObservableProperty]
    private string? _errorMessage;

    partial void OnSelectedFoodChanged(Food? value) => OnPropertyChanged(nameof(HasSelectedFood));

    partial void OnNameChanged(string value)
    {
        if (SelectedFood is not null)
        {
            SelectedFood.Name = value;
        }

        UpdateMatchQuality();
    }

    [RelayCommand]
    private void Select(ResolveMatchCandidate candidate) => SelectFood(candidate.Food, isNew: false);

    [RelayCommand]
    private void CreateNewFood()
    {
        var food = new Food
        {
            Name = FoodDescription,
            Measure = "100g",
            GramsPerMeasure = 100,
            IsCustom = true,
        };
        SelectFood(food, isNew: true);
    }

    [RelayCommand]
    private void SaveAndUse()
    {
        if (SelectedFood is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(Name))
        {
            ErrorMessage = "Name is required.";
            return;
        }

        if (GramsPerMeasure <= 0)
        {
            ErrorMessage = "Grams per measure must be greater than zero.";
            return;
        }

        ApplyFormToSelectedFood();

        try
        {
            if (_isNewFood)
            {
                _foodService.Add(SelectedFood);
            }
            else
            {
                _foodService.Update(SelectedFood);
            }

            FoodSelected?.Invoke(SelectedFood);
            RequestClose?.Invoke();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to save: {ex.Message}";
        }
    }

    [RelayCommand]
    private void Cancel() => RequestClose?.Invoke();

    [RelayCommand]
    private void SearchUsdaForFood()
    {

    }

    private void SelectFood(Food? food, bool isNew)
    {
        _isNewFood = isNew;
        SelectedFood = food is null ? null : Clone(food);
        ErrorMessage = null;

        if (SelectedFood is null)
        {
            Name = string.Empty;
            Measure = string.Empty;
            GramsPerMeasure = 0;
            CaloriesPer100g = 0;
            FatPer100g = 0;
            ProteinPer100g = 0;
            CarbohydratesPer100g = 0;
            SaturatedFatPer100g = 0;
            DietaryFiberPer100g = 0;
            SugarsPer100g = 0;
            SodiumMgPer100g = 0;
            return;
        }

        Name = SelectedFood.Name;
        Measure = SelectedFood.Measure;
        GramsPerMeasure = SelectedFood.GramsPerMeasure;
        CaloriesPer100g = SelectedFood.CaloriesPer100g;
        FatPer100g = SelectedFood.FatPer100g;
        ProteinPer100g = SelectedFood.ProteinPer100g;
        CarbohydratesPer100g = SelectedFood.CarbohydratesPer100g;
        SaturatedFatPer100g = SelectedFood.SaturatedFatPer100g;
        DietaryFiberPer100g = SelectedFood.DietaryFiberPer100g;
        SugarsPer100g = SelectedFood.SugarsPer100g;
        SodiumMgPer100g = SelectedFood.SodiumMgPer100g;
    }

    private static Food Clone(Food source) => new()
    {
        Id = source.Id,
        Name = source.Name,
        Measure = source.Measure,
        GramsPerMeasure = source.GramsPerMeasure,
        Notes = source.Notes,
        IsCustom = source.IsCustom,
        CaloriesPer100g = source.CaloriesPer100g,
        FatPer100g = source.FatPer100g,
        SaturatedFatPer100g = source.SaturatedFatPer100g,
        CarbohydratesPer100g = source.CarbohydratesPer100g,
        DietaryFiberPer100g = source.DietaryFiberPer100g,
        SugarsPer100g = source.SugarsPer100g,
        ProteinPer100g = source.ProteinPer100g,
        SodiumMgPer100g = source.SodiumMgPer100g,
    };

    private void ApplyFormToSelectedFood()
    {
        SelectedFood!.Name = Name.Trim();
        SelectedFood.Measure = Measure;
        SelectedFood.GramsPerMeasure = GramsPerMeasure;
        SelectedFood.CaloriesPer100g = CaloriesPer100g;
        SelectedFood.FatPer100g = FatPer100g;
        SelectedFood.ProteinPer100g = ProteinPer100g;
        SelectedFood.CarbohydratesPer100g = CarbohydratesPer100g;
        SelectedFood.SaturatedFatPer100g = SaturatedFatPer100g;
        SelectedFood.DietaryFiberPer100g = DietaryFiberPer100g;
        SelectedFood.SugarsPer100g = SugarsPer100g;
        SelectedFood.SodiumMgPer100g = SodiumMgPer100g;
    }

    private void UpdateMatchQuality()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            MatchQualityLabel = string.Empty;
            MatchQualityColor = "Gray";
            return;
        }

        FoodMatchResult result = _foodService.ScoreMatch(FoodDescription, Name);
        MatchQualityLabel = result.Method == "Exact"
            ? "Exact match"
            : $"{result.Method} {result.Score * 100:0}%";
        MatchQualityColor = result.Score >= 0.6
            ? "#2ECC71"
            : result.Score >= 0.4
                ? "#F39C12"
                : "#E74C3C";
    }
}

public class ResolveMatchCandidate
{
    public ResolveMatchCandidate(Food food, double score, string method, bool isCurrent)
    {
        Food = food;
        Name = food.Name;
        ScoreText = $"{score * 100:0}%";
        Method = method;
        IsCurrent = isCurrent;
    }

    public Food Food { get; }

    public string Name { get; }

    public string ScoreText { get; }

    public string Method { get; }

    public bool IsCurrent { get; }
}
