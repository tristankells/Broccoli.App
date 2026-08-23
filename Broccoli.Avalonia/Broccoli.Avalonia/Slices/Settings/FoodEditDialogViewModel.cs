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
        SetEnergyInputs(food.CaloriesPer100g);
        ErrorMessage = null;
    }

    partial void OnCaloriesInputChanged(string? value)
    {
        if (_isSyncing || Food is null)
        {
            return;
        }

        if (EnergyConversions.TryParse(value, out double calories))
        {
            _isSyncing = true;
            CaloriesInput = EnergyConversions.Format(calories);
            KilojoulesInput = EnergyConversions.Format(calories * EnergyConversions.KilojoulesPerCalorie);
            _isSyncing = false;
        }
    }

    partial void OnKilojoulesInputChanged(string? value)
    {
        if (_isSyncing || Food is null)
        {
            return;
        }

        if (EnergyConversions.TryParse(value, out double kilojoules))
        {
            _isSyncing = true;
            KilojoulesInput = EnergyConversions.Format(kilojoules);
            CaloriesInput = EnergyConversions.Format(kilojoules / EnergyConversions.KilojoulesPerCalorie);
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

        Food.CaloriesPer100g = EnergyConversions.ParseOrDefault(CaloriesInput);
        _saved?.Invoke(Food);
        RequestClose?.Invoke();
    }

    [RelayCommand]
    private void Cancel() => RequestClose?.Invoke();

    private void SetEnergyInputs(double calories)
    {
        _isSyncing = true;
        CaloriesInput = EnergyConversions.Format(calories);
        KilojoulesInput = EnergyConversions.Format(calories * EnergyConversions.KilojoulesPerCalorie);
        _isSyncing = false;
    }
}
