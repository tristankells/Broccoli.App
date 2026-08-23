using System.Globalization;
using System.Text.RegularExpressions;
using Broccoli.Avalonia.Models;
using Broccoli.Avalonia.Shared;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Broccoli.Avalonia.Slices.Settings;

/// <summary>
/// Backs the food edit popup. Calories and kilojoules are two-way bound: editing either field
/// recomputes the other (1 kcal = 4.184 kJ) once the field loses focus, and unit-suffixed pastes
/// such as "739kJ" or "100 kcal" are trimmed to just the number. Kilojoules is a UI-only value
/// derived from calories and is never stored on the <see cref="Food"/> itself.
/// </summary>
public partial class FoodEditDialogViewModel : ViewModelBase
{
    private const double KilojoulesPerCalorie = 4.184;

    private static readonly Regex s_numberPrefix = new(
        @"^\s*(?<number>[-+]?\d+(?:[.,]\d+)?)",
        RegexOptions.Compiled);

    private bool _isSyncing;
    private Action<Food>? _saved;

    [ObservableProperty]
    private Food? _food;

    [ObservableProperty]
    private bool _isNew;

    [ObservableProperty]
    private string? _caloriesInput;

    [ObservableProperty]
    private string? _kilojoulesInput;

    [ObservableProperty]
    private string? _errorMessage;

    public Action? RequestClose { get; set; }

    public string Title => IsNew ? "Add Food" : $"Edit: {Food?.Name}";

    public void Open(Food food, bool isNew, Action<Food> saved)
    {
        Food = food;
        IsNew = isNew;
        _saved = saved;
        _isSyncing = true;
        CaloriesInput = FormatValue(food.CaloriesPer100g);
        KilojoulesInput = FormatValue(food.CaloriesPer100g * KilojoulesPerCalorie);
        _isSyncing = false;
        ErrorMessage = null;
    }

    partial void OnCaloriesInputChanged(string? value)
    {
        if (_isSyncing || Food is null)
        {
            return;
        }

        if (TryParseEnergy(value, out double calories))
        {
            _isSyncing = true;
            CaloriesInput = FormatValue(calories);
            KilojoulesInput = FormatValue(calories * KilojoulesPerCalorie);
            _isSyncing = false;
        }
    }

    partial void OnKilojoulesInputChanged(string? value)
    {
        if (_isSyncing || Food is null)
        {
            return;
        }

        if (TryParseEnergy(value, out double kilojoules))
        {
            _isSyncing = true;
            KilojoulesInput = FormatValue(kilojoules);
            CaloriesInput = FormatValue(kilojoules / KilojoulesPerCalorie);
            _isSyncing = false;
        }
    }

    partial void OnFoodChanged(Food? value) => OnPropertyChanged(nameof(Title));

    partial void OnIsNewChanged(bool value) => OnPropertyChanged(nameof(Title));

    [RelayCommand]
    private void Save()
    {
        if (Food is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(Food.Name))
        {
            ErrorMessage = "Name is required.";
            return;
        }

        Food.CaloriesPer100g = ParseEnergyOrDefault(CaloriesInput);
        _saved?.Invoke(Food);
        RequestClose?.Invoke();
    }

    [RelayCommand]
    private void Cancel() => RequestClose?.Invoke();

    /// <summary>Extracts the leading number from a value, ignoring trailing unit text like "kJ" or "kcal".</summary>
    private static bool TryParseEnergy(string? value, out double result)
    {
        result = 0;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        Match match = s_numberPrefix.Match(value);
        if (!match.Success)
        {
            return false;
        }

        string number = match.Groups["number"].Value.Replace(',', '.');
        return double.TryParse(number, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
    }

    private static double ParseEnergyOrDefault(string? value) =>
        TryParseEnergy(value, out double d) ? d : 0;

    private static string FormatValue(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
}
