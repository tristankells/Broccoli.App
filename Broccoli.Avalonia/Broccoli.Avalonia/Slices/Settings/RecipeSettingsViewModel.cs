using Broccoli.Avalonia.Models;
using Broccoli.Avalonia.Shared;
using Broccoli.Avalonia.Slices.Planning;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Broccoli.Avalonia.Slices.Settings;

public partial class RecipeSettingsViewModel : ViewModelBase
{
    private readonly IMacroTargetService _macroService;

    [ObservableProperty] private bool _comparisonEnabled;
    [ObservableProperty] private string _comparisonPersonId = string.Empty;
    [ObservableProperty] private string? _statusMessage;

    public List<MacroTarget> AvailableTargets { get; } = new();
    public int SelectedTargetIndex
    {
        get
        {
            var idx = AvailableTargets.FindIndex(t => t.Id == ComparisonPersonId);
            return idx >= 0 ? idx : 0;
        }
        set
        {
            if (value >= 0 && value < AvailableTargets.Count)
                ComparisonPersonId = AvailableTargets[value].Id;
        }
    }

    public RecipeSettingsViewModel() : this(new MacroTargetService())
    {
    }

    public RecipeSettingsViewModel(IMacroTargetService macroService)
    {
        _macroService = macroService;
        Load();
    }

    private void Load()
    {
        try
        {
            var settings = _macroService.GetSettings();
            ComparisonEnabled = settings.RecipeMealComparisonEnabled;
            ComparisonPersonId = settings.RecipeMealComparisonPersonId;

            var targets = _macroService.GetAll();
            AvailableTargets.Clear();
            AvailableTargets.AddRange(targets);
            OnPropertyChanged(nameof(SelectedTargetIndex));
        }
        catch { StatusMessage = "Failed to load settings."; }
    }

    [RelayCommand]
    private void Save()
    {
        StatusMessage = null;
        try
        {
            var settings = _macroService.GetSettings();
            settings.RecipeMealComparisonEnabled = ComparisonEnabled;
            settings.RecipeMealComparisonPersonId = ComparisonPersonId;
            _macroService.SaveSettings(settings);
            StatusMessage = "Saved.";
        }
        catch { StatusMessage = "Failed to save."; }
    }

    [RelayCommand]
    private void RefreshTargets()
    {
        Load();
    }

    partial void OnComparisonEnabledChanged(bool value) => OnPropertyChanged(nameof(SelectedTargetIndex));
}
