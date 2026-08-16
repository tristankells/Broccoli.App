using Broccoli.Avalonia.Models;
using Broccoli.Avalonia.Shared;
using Broccoli.Avalonia.Slices.Planning;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

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

    [ObservableProperty] private bool _showCardCalorieMatch;
    [ObservableProperty] private double _calorieMatchTolerancePercent = 15;

    public List<MacroTarget> AvailableTargets { get; } = new();
    public int SelectedTargetIndex
    {
        get
        {
            int idx = AvailableTargets.FindIndex(t => t.Id == ComparisonPersonId);
            return idx >= 0 ? idx : 0;
        }
        set
        {
            if (value >= 0 && value < AvailableTargets.Count)
            {
                ComparisonPersonId = AvailableTargets[value].Id;
            }
        }
    }

    public RecipeSettingsViewModel() : this(new MacroTargetService())
    {
    }

    public RecipeSettingsViewModel(IMacroTargetService macroService)
    {
        _macroService = macroService;
        Load();
        WeakReferenceMessenger.Default.Register<MacroTargetsChangedMessage>(this, (_, _) => Load());
    }

    private void Load()
    {
        MacroTargetSettings settings = _macroService.GetSettings();
        ComparisonEnabled = settings.RecipeMealComparisonEnabled;
        ComparisonPersonId = settings.RecipeMealComparisonPersonId;
        ShowCardImage = settings.ShowCardImage;
        ShowCardTags = settings.ShowCardTags;
        ShowCardSeasonality = settings.ShowCardSeasonality;
        ShowCardNutrition = settings.ShowCardNutrition;
        ShowCardCalorieMatch = settings.ShowCardCalorieMatch;
        CalorieMatchTolerancePercent = settings.CalorieMatchTolerancePercent;

        List<MacroTarget> targets = _macroService.GetAll();
        AvailableTargets.Clear();
        AvailableTargets.AddRange(targets);

        if (AvailableTargets.Count > 0 && string.IsNullOrWhiteSpace(ComparisonPersonId))
        {
            ComparisonPersonId = AvailableTargets[0].Id;
        }

        OnPropertyChanged(nameof(SelectedTargetIndex));
    }

    [RelayCommand]
    private void Save()
    {
        StatusMessage = null;
        MacroTargetSettings settings = _macroService.GetSettings();
        settings.RecipeMealComparisonEnabled = ComparisonEnabled;
        settings.RecipeMealComparisonPersonId = ComparisonPersonId;
        settings.ShowCardImage = ShowCardImage;
        settings.ShowCardTags = ShowCardTags;
        settings.ShowCardSeasonality = ShowCardSeasonality;
        settings.ShowCardNutrition = ShowCardNutrition;
        settings.ShowCardCalorieMatch = ShowCardCalorieMatch;
        settings.CalorieMatchTolerancePercent = CalorieMatchTolerancePercent;
        _macroService.SaveSettings(settings);
        StatusMessage = "Saved.";
        WeakReferenceMessenger.Default.Send(new CardSettingsChangedMessage());
    }
}
