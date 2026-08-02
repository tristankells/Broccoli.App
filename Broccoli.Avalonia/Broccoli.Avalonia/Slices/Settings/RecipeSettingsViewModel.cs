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

    [ObservableProperty] private bool _showCardImage = true;
    [ObservableProperty] private bool _showCardTags = true;
    [ObservableProperty] private bool _showCardSeasonality = true;
    [ObservableProperty] private bool _showCardNutrition = true;

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
            ShowCardImage = settings.ShowCardImage;
            ShowCardTags = settings.ShowCardTags;
            ShowCardSeasonality = settings.ShowCardSeasonality;
            ShowCardNutrition = settings.ShowCardNutrition;

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
            settings.ShowCardImage = ShowCardImage;
            settings.ShowCardTags = ShowCardTags;
            settings.ShowCardSeasonality = ShowCardSeasonality;
            settings.ShowCardNutrition = ShowCardNutrition;
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
}
