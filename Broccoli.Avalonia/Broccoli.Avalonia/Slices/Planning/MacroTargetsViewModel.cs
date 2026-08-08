using Broccoli.Avalonia.Models;
using Broccoli.Avalonia.Shared;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace Broccoli.Avalonia.Slices.Planning;

public partial class MacroTargetsViewModel : ViewModelBase
{
    private readonly IMacroTargetService _macroTargetService;
    private readonly MacroCalculatorService _calculator;

    public ObservableCollection<MacroTargetRowViewModel> Targets { get; } = new();

    [ObservableProperty] private MacroTargetSettings _settings = new();
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private bool _isSettingsOpen;

    [ObservableProperty] private UnitSystem _draftUnitSystem = UnitSystem.Metric;
    [ObservableProperty] private BmrFormula _draftBmrFormula = BmrFormula.MifflinStJeor;
    [ObservableProperty] private ProteinMethod _draftProteinMethod = ProteinMethod.RatioPercent;
    [ObservableProperty] private double _draftProteinPercent = 30;
    [ObservableProperty] private double _draftCarbPercent = 40;
    [ObservableProperty] private double _draftFatPercent = 30;
    [ObservableProperty] private double _draftProteinGramsPerKg = 1.8;

    public int DraftUnitSystemIndex
    {
        get => (int)DraftUnitSystem;
        set => DraftUnitSystem = (UnitSystem)value;
    }

    public int DraftBmrFormulaIndex
    {
        get => (int)DraftBmrFormula;
        set => DraftBmrFormula = (BmrFormula)value;
    }

    public int DraftProteinMethodIndex
    {
        get => (int)DraftProteinMethod;
        set => DraftProteinMethod = (ProteinMethod)value;
    }

    public string WeightUnit => Settings.UnitSystem == UnitSystem.Imperial ? "lbs" : "kg";
    public string HeightUnit => Settings.UnitSystem == UnitSystem.Imperial ? "in"  : "cm";

    public string ProteinPercentLabel =>
        Settings.ProteinMethod == ProteinMethod.GramsPerKg
            ? $"({Settings.ProteinGramsPerKg:0.#}g/kg)"
            : $"({Settings.ProteinPercent:0}%)";

    public string CarbPercentLabel =>
        Settings.ProteinMethod == ProteinMethod.GramsPerKg
            ? $"({Settings.CarbPercent:0}% of rem.)"
            : $"({Settings.CarbPercent:0}%)";

    public string FatPercentLabel =>
        Settings.ProteinMethod == ProteinMethod.GramsPerKg
            ? $"({Settings.FatPercent:0}% of rem.)"
            : $"({Settings.FatPercent:0}%)";

    public bool DraftIsRatioPercent => DraftProteinMethod == ProteinMethod.RatioPercent;
    public bool DraftIsGramsPerKg => DraftProteinMethod == ProteinMethod.GramsPerKg;

    public double DraftMacroSum => DraftProteinPercent + DraftCarbPercent + DraftFatPercent;
    public bool DraftMacroSumValid => Math.Abs(DraftMacroSum - 100) < 0.01;
    public bool DraftCanSave => DraftProteinMethod == ProteinMethod.GramsPerKg || DraftMacroSumValid;

    public MacroTargetsViewModel() : this(new MacroTargetService(), new MacroCalculatorService())
    {
    }

    public MacroTargetsViewModel(IMacroTargetService macroTargetService, MacroCalculatorService calculator)
    {
        _macroTargetService = macroTargetService;
        _calculator = calculator;
        LoadData();
    }

    public void LoadData()
    {
        ErrorMessage = null;

        try
        {
            Settings = _macroTargetService.GetSettings();
            List<MacroTarget> targets = _macroTargetService.GetAll();

            Targets.Clear();
            foreach (MacroTarget t in targets)
            {
                _calculator.Calculate(t, Settings);
                var row = MacroTargetRowViewModel.Create(t, Settings, OnRowChanged);
                row.RefreshCalculatedDisplay();
                Targets.Add(row);
            }

            RefreshLabels();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load: {ex.Message}";
        }
    }

    [RelayCommand]
    private void AddPerson()
    {
        ErrorMessage = null;

        try
        {
            var target = new MacroTarget
            {
                Name = "New Person",
                Gender = GenderType.Male,
                WeightKg = 80,
                HeightCm = 175,
                Age = 30,
            };
            _calculator.Calculate(target, Settings);
            target = _macroTargetService.Add(target);

            var row = MacroTargetRowViewModel.Create(target, Settings, OnRowChanged);
            row.RefreshCalculatedDisplay();
            Targets.Add(row);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to add: {ex.Message}";
        }
    }

    [RelayCommand]
    private void DeletePerson(MacroTargetRowViewModel row)
    {
        ErrorMessage = null;
        Targets.Remove(row);

        try
        {
            _macroTargetService.Delete(row.Model.Id);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to delete: {ex.Message}";
            Targets.Add(row);
        }
    }

    private void OnRowChanged(MacroTargetRowViewModel row)
    {
        ErrorMessage = null;

        try
        {
            row.SyncFromModel(Settings);
            _calculator.Calculate(row.Model, Settings);
            row.RefreshCalculatedDisplay();
            _macroTargetService.Update(row.Model);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to save: {ex.Message}";
        }
    }

    [RelayCommand]
    private void OpenSettings()
    {
        DraftUnitSystem = Settings.UnitSystem;
        DraftBmrFormula = Settings.BmrFormula;
        DraftProteinMethod = Settings.ProteinMethod;
        DraftProteinPercent = Settings.ProteinPercent;
        DraftCarbPercent = Settings.CarbPercent;
        DraftFatPercent = Settings.FatPercent;
        DraftProteinGramsPerKg = Settings.ProteinGramsPerKg;
        IsSettingsOpen = true;
        RefreshDraftValidation();
        OnPropertyChanged(nameof(DraftUnitSystemIndex));
        OnPropertyChanged(nameof(DraftBmrFormulaIndex));
        OnPropertyChanged(nameof(DraftProteinMethodIndex));
    }

    [RelayCommand]
    private void CancelSettings()
    {
        IsSettingsOpen = false;
    }

    [RelayCommand]
    private void SaveSettings()
    {
        if (!DraftCanSave)
        {
            return;
        }

        ErrorMessage = null;

        try
        {
            var updated = new MacroTargetSettings
            {
                Id = Settings.Id,
                UnitSystem = DraftUnitSystem,
                BmrFormula = DraftBmrFormula,
                ProteinMethod = DraftProteinMethod,
                ProteinPercent = DraftProteinPercent,
                CarbPercent = DraftCarbPercent,
                FatPercent = DraftFatPercent,
                ProteinGramsPerKg = DraftProteinGramsPerKg,
                RecipeMealComparisonEnabled = Settings.RecipeMealComparisonEnabled,
                RecipeMealComparisonPersonId = Settings.RecipeMealComparisonPersonId,
            };

            Settings = _macroTargetService.SaveSettings(updated);
            IsSettingsOpen = false;
            RefreshLabels();

            foreach (MacroTargetRowViewModel row in Targets)
            {
                row.LoadFromModel(Settings);
                row.SyncFromModel(Settings);
                _calculator.Calculate(row.Model, Settings);
                row.RefreshCalculatedDisplay();
                _macroTargetService.Update(row.Model);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to save settings: {ex.Message}";
        }
    }

    partial void OnDraftUnitSystemChanged(UnitSystem value) => OnPropertyChanged(nameof(DraftUnitSystemIndex));
    partial void OnDraftBmrFormulaChanged(BmrFormula value) => OnPropertyChanged(nameof(DraftBmrFormulaIndex));
    partial void OnDraftProteinMethodChanged(ProteinMethod value) { OnPropertyChanged(nameof(DraftProteinMethodIndex)); RefreshDraftValidation(); }
    partial void OnDraftProteinPercentChanged(double value) => RefreshDraftValidation();
    partial void OnDraftCarbPercentChanged(double value) => RefreshDraftValidation();
    partial void OnDraftFatPercentChanged(double value) => RefreshDraftValidation();

    private void RefreshDraftValidation()
    {
        OnPropertyChanged(nameof(DraftIsRatioPercent));
        OnPropertyChanged(nameof(DraftIsGramsPerKg));
        OnPropertyChanged(nameof(DraftMacroSum));
        OnPropertyChanged(nameof(DraftMacroSumValid));
        OnPropertyChanged(nameof(DraftCanSave));
    }

    private void RefreshLabels()
    {
        OnPropertyChanged(nameof(WeightUnit));
        OnPropertyChanged(nameof(HeightUnit));
        OnPropertyChanged(nameof(ProteinPercentLabel));
        OnPropertyChanged(nameof(CarbPercentLabel));
        OnPropertyChanged(nameof(FatPercentLabel));
    }
}
