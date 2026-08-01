using Broccoli.Avalonia.Models;
using Broccoli.Avalonia.Shared;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Broccoli.Avalonia.Slices.Planning;

public partial class MacroTargetRowViewModel : ViewModelBase
{
    public MacroTarget Model { get; }

    private bool _suppressChanged;
    public Action<MacroTargetRowViewModel>? Changed { get; set; }

    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private GenderType _gender = GenderType.Male;
    [ObservableProperty] private double _weightDisplay;
    [ObservableProperty] private double _heightDisplay;
    [ObservableProperty] private int _age;
    [ObservableProperty] private ActivityLevel _activityLevel = ActivityLevel.ModeratelyActive;
    [ObservableProperty] private MacroGoal _goal = MacroGoal.Maintain;
    [ObservableProperty] private int _goalCalorieDelta;

    public int GenderIndex
    {
        get => (int)Gender;
        set => Gender = (GenderType)value;
    }

    public int ActivityIndex
    {
        get => (int)ActivityLevel;
        set => ActivityLevel = (ActivityLevel)value;
    }

    public int GoalIndex
    {
        get => (int)Goal;
        set => Goal = (MacroGoal)value;
    }

    private string _bmrText = "—";
    private string _tdeeText = "—";
    private string _caloriesText = "—";
    private string _proteinText = "—";
    private string _carbsText = "—";
    private string _fatText = "—";
    private string _mealCaloriesText = "—";
    private string _mealProteinText = "—";
    private string _mealCarbsText = "—";
    private string _mealFatText = "—";

    public string BmrText { get => _bmrText; set => SetProperty(ref _bmrText, value); }
    public string TdeeText { get => _tdeeText; set => SetProperty(ref _tdeeText, value); }
    public string CaloriesText { get => _caloriesText; set => SetProperty(ref _caloriesText, value); }
    public string ProteinText { get => _proteinText; set => SetProperty(ref _proteinText, value); }
    public string CarbsText { get => _carbsText; set => SetProperty(ref _carbsText, value); }
    public string FatText { get => _fatText; set => SetProperty(ref _fatText, value); }
    public string MealCaloriesText { get => _mealCaloriesText; set => SetProperty(ref _mealCaloriesText, value); }
    public string MealProteinText { get => _mealProteinText; set => SetProperty(ref _mealProteinText, value); }
    public string MealCarbsText { get => _mealCarbsText; set => SetProperty(ref _mealCarbsText, value); }
    public string MealFatText { get => _mealFatText; set => SetProperty(ref _mealFatText, value); }

    public MacroTargetRowViewModel(MacroTarget model) : this(model, null)
    {
    }

    private MacroTargetRowViewModel(MacroTarget model, Action<MacroTargetRowViewModel>? onChanged)
    {
        Model = model;
        Changed = onChanged;
        _suppressChanged = true;

        _name = model.Name;
        _gender = model.Gender;
        _weightDisplay = model.WeightKg;
        _heightDisplay = model.HeightCm;
        _age = model.Age;
        _activityLevel = model.ActivityLevel;
        _goal = model.Goal;
        _goalCalorieDelta = model.GoalCalorieDelta;

        _suppressChanged = false;
    }

    public static MacroTargetRowViewModel Create(MacroTarget model, MacroTargetSettings settings, Action<MacroTargetRowViewModel> onChanged)
    {
        var vm = new MacroTargetRowViewModel(model, onChanged);
        vm.LoadFromModel(settings);
        return vm;
    }

    public void SyncFromModel(MacroTargetSettings settings)
    {
        Model.Name = Name;
        Model.Gender = Gender;
        Model.Age = Age;
        Model.ActivityLevel = ActivityLevel;
        Model.Goal = Goal;
        Model.GoalCalorieDelta = GoalCalorieDelta;

        if (settings.UnitSystem == UnitSystem.Imperial)
        {
            Model.WeightKg = WeightDisplay / 2.20462;
            Model.HeightCm = HeightDisplay * 2.54;
        }
        else
        {
            Model.WeightKg = WeightDisplay;
            Model.HeightCm = HeightDisplay;
        }
    }

    public void LoadFromModel(MacroTargetSettings settings)
    {
        _suppressChanged = true;

        Name = Model.Name;
        Gender = Model.Gender;
        Age = Model.Age;
        ActivityLevel = Model.ActivityLevel;
        Goal = Model.Goal;
        GoalCalorieDelta = Model.GoalCalorieDelta;

        WeightDisplay = settings.UnitSystem == UnitSystem.Imperial
            ? Math.Round(Model.WeightKg * 2.20462, 1)
            : Model.WeightKg;

        HeightDisplay = settings.UnitSystem == UnitSystem.Imperial
            ? Math.Round(Model.HeightCm / 2.54, 1)
            : Model.HeightCm;

        OnPropertyChanged(nameof(GenderIndex));
        OnPropertyChanged(nameof(ActivityIndex));
        OnPropertyChanged(nameof(GoalIndex));

        _suppressChanged = false;
    }

    public void RefreshCalculatedDisplay()
    {
        BmrText = FormatCalc(Model.Bmr);
        TdeeText = FormatCalc(Model.Tdee);
        CaloriesText = FormatCalc(Model.RecommendedCalories);
        ProteinText = FormatCalc(Model.RecommendedProteinG);
        CarbsText = FormatCalc(Model.RecommendedCarbsG);
        FatText = FormatCalc(Model.RecommendedFatG);
        MealCaloriesText = FormatCalc(Model.RecommendedCalories / 3.0);
        MealProteinText = FormatCalc(Model.RecommendedProteinG / 3.0);
        MealCarbsText = FormatCalc(Model.RecommendedCarbsG / 3.0);
        MealFatText = FormatCalc(Model.RecommendedFatG / 3.0);
    }

    private static string FormatCalc(double value) =>
        value > 0 ? ((int)Math.Ceiling(value)).ToString("N0") : "—";

    partial void OnNameChanged(string value)            => NotifyChanged();
    partial void OnWeightDisplayChanged(double value)     => NotifyChanged();
    partial void OnHeightDisplayChanged(double value)     => NotifyChanged();
    partial void OnAgeChanged(int value)                 => NotifyChanged();
    partial void OnGoalCalorieDeltaChanged(int value)    => NotifyChanged();

    partial void OnGenderChanged(GenderType value)
    {
        OnPropertyChanged(nameof(GenderIndex));
        NotifyChanged();
    }

    partial void OnActivityLevelChanged(ActivityLevel value)
    {
        OnPropertyChanged(nameof(ActivityIndex));
        NotifyChanged();
    }

    partial void OnGoalChanged(MacroGoal value)
    {
        _suppressChanged = true;
        GoalCalorieDelta = value switch
        {
            MacroGoal.Lose => -500,
            MacroGoal.Gain => 250,
            _ => 0
        };
        _suppressChanged = false;

        OnPropertyChanged(nameof(GoalIndex));
        NotifyChanged();
    }

    private void NotifyChanged()
    {
        if (!_suppressChanged) Changed?.Invoke(this);
    }
}
