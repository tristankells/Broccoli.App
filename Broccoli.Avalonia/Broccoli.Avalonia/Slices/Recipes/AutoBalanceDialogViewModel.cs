using System.Collections.ObjectModel;
using System.Globalization;
using Broccoli.Avalonia.Models;
using Broccoli.Avalonia.Shared;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Broccoli.Avalonia.Slices.Recipes;

public partial class AutoBalanceDialogViewModel : ViewModelBase
{
    private readonly IReadOnlyList<AutoBalanceIngredient> _ingredients;
    private readonly AutoBalanceTargets _targets;
    private readonly AutoBalanceStrategy _strategy;
    private readonly double _tolerancePercent;
    private readonly double _servings;
    private AutoBalancePreview _preview;

    public AutoBalanceDialogViewModel(
        IReadOnlyList<AutoBalanceIngredient> ingredients,
        AutoBalanceTargets targets,
        double servings,
        string personName,
        AutoBalanceStrategy strategy,
        double tolerancePercent)
    {
        _ingredients = ingredients;
        _targets = targets;
        _servings = servings;
        PersonName = personName;
        _strategy = strategy;
        _tolerancePercent = tolerancePercent;
        _preview = AutoBalanceCalculator.Calculate(ingredients, targets, AllTargets, strategy, tolerancePercent);
        RebuildPreviewRows();
    }

    public string PersonName { get; }

    public ObservableCollection<AutoBalanceAdjustmentRow> Adjustments { get; } = new();

    public ObservableCollection<AutoBalanceTargetRow> TargetRows { get; } = new();

    public Action<IReadOnlyList<AutoBalanceAdjustment>>? ApplyRequested { get; set; }

    public Action? RequestClose { get; set; }

    [ObservableProperty]
    private bool _adjustCalories = true;

    [ObservableProperty]
    private bool _adjustProtein = true;

    [ObservableProperty]
    private bool _adjustCarbs = true;

    [ObservableProperty]
    private bool _adjustFat = true;

    public string BeforeSummaryText => BuildSummary(_preview.Before);

    public string AfterSummaryText => BuildSummary(_preview.After);

    public string TargetSummaryText =>
        $"Target ({PersonName}): {FormatCalories(_targets.Calories)} kcal | P {FormatGrams(_targets.ProteinG)} | C {FormatGrams(_targets.CarbsG)} | F {FormatGrams(_targets.FatG)}";

    public bool HasChanges => _preview.HasChanges;

    public bool HasAdjustments => _preview.Adjustments.Count > 0;

    public bool ShowFallbackWarning => _strategy == AutoBalanceStrategy.LinearSolve && _preview.UsedFallback;

    private HashSet<AutoBalanceNutrient> AllTargets => new()
    {
        AutoBalanceNutrient.Calories,
        AutoBalanceNutrient.Protein,
        AutoBalanceNutrient.Carbs,
        AutoBalanceNutrient.Fat,
    };

    partial void OnAdjustCaloriesChanged(bool value) => RecomputePreview();

    partial void OnAdjustProteinChanged(bool value) => RecomputePreview();

    partial void OnAdjustCarbsChanged(bool value) => RecomputePreview();

    partial void OnAdjustFatChanged(bool value) => RecomputePreview();

    private void RecomputePreview()
    {
        var selected = new HashSet<AutoBalanceNutrient>();
        if (AdjustCalories)
        {
            selected.Add(AutoBalanceNutrient.Calories);
        }

        if (AdjustProtein)
        {
            selected.Add(AutoBalanceNutrient.Protein);
        }

        if (AdjustCarbs)
        {
            selected.Add(AutoBalanceNutrient.Carbs);
        }

        if (AdjustFat)
        {
            selected.Add(AutoBalanceNutrient.Fat);
        }

        _preview = AutoBalanceCalculator.Calculate(_ingredients, _targets, selected, _strategy, _tolerancePercent);
        RebuildPreviewRows();
        ApplyCommand.NotifyCanExecuteChanged();
    }

    private void RebuildPreviewRows()
    {
        Adjustments.Clear();
        foreach (AutoBalanceAdjustment adjustment in _preview.Adjustments)
        {
            Adjustments.Add(new AutoBalanceAdjustmentRow(adjustment));
        }

        TargetRows.Clear();
        TargetRows.Add(new AutoBalanceTargetRow("Calories", _targets.Calories, _preview.Before.Calories, _preview.After.Calories, _servings, "0"));
        TargetRows.Add(new AutoBalanceTargetRow("Protein", _targets.ProteinG, _preview.Before.ProteinG, _preview.After.ProteinG, _servings, "0.0"));
        TargetRows.Add(new AutoBalanceTargetRow("Carbs", _targets.CarbsG, _preview.Before.CarbsG, _preview.After.CarbsG, _servings, "0.0"));
        TargetRows.Add(new AutoBalanceTargetRow("Fat", _targets.FatG, _preview.Before.FatG, _preview.After.FatG, _servings, "0.0"));

        OnPropertyChanged(nameof(BeforeSummaryText));
        OnPropertyChanged(nameof(AfterSummaryText));
        OnPropertyChanged(nameof(HasChanges));
        OnPropertyChanged(nameof(HasAdjustments));
        OnPropertyChanged(nameof(ShowFallbackWarning));
    }

    private string BuildSummary(AutoBalanceTotals totals) =>
        $"{FormatCalories(totals.Calories)} kcal | P {FormatGrams(totals.ProteinG)} | C {FormatGrams(totals.CarbsG)} | F {FormatGrams(totals.FatG)}";

    private static string FormatCalories(double value) => $"{value:0}";

    private static string FormatGrams(double value) => $"{value:0.0}g";

    [RelayCommand(CanExecute = nameof(HasChanges))]
    private void Apply()
    {
        if (!_preview.HasChanges)
        {
            return;
        }

        ApplyRequested?.Invoke(_preview.Adjustments);
        RequestClose?.Invoke();
    }

    [RelayCommand]
    private void Cancel() => RequestClose?.Invoke();
}

public sealed class AutoBalanceAdjustmentRow
{
    public AutoBalanceAdjustmentRow(AutoBalanceAdjustment adjustment)
    {
        FoodName = adjustment.Ingredient.FoodName;
        BeforeText = $"{adjustment.BeforeGrams:0.#} g";
        AfterText = $"{adjustment.AfterGrams:0.#} g";
        DeltaText = $"{adjustment.DeltaGrams:+0.#;-0.#;0} g";
    }

    public string FoodName { get; }

    public string BeforeText { get; }

    public string AfterText { get; }

    public string DeltaText { get; }
}

public sealed class AutoBalanceTargetRow
{
    public AutoBalanceTargetRow(string name, double target, double before, double after, double servings, string format)
    {
        double targetPerServing = servings > 0 ? target / servings : target;
        double beforePerServing = servings > 0 ? before / servings : before;
        double afterPerServing = servings > 0 ? after / servings : after;

        Name = name;
        TargetText = targetPerServing.ToString(format, CultureInfo.InvariantCulture);
        BeforeText = beforePerServing.ToString(format, CultureInfo.InvariantCulture);
        AfterText = afterPerServing.ToString(format, CultureInfo.InvariantCulture);
        DeltaText = $"{after - before:+0.0;-0.0;0}";
    }

    public string Name { get; }

    public string TargetText { get; }

    public string BeforeText { get; }

    public string AfterText { get; }

    public string DeltaText { get; }
}
