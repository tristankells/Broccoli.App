using Broccoli.Avalonia.Models;
using Broccoli.Avalonia.Shared;
using Broccoli.Avalonia.Slices.Planning;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

namespace Broccoli.Avalonia.Slices.Settings;

public partial class SeasonalitySettingsViewModel : ViewModelBase
{
    private readonly IMacroTargetService _macroService;

    [ObservableProperty]
    private bool _showSeasonalityNavItem = true;

    [ObservableProperty]
    private double _greenAlwaysHigh = 80;

    [ObservableProperty]
    private double _greenFloor = 55;

    [ObservableProperty]
    private double _redCeiling = 45;

    [ObservableProperty]
    private double _redAlwaysLow = 25;

    [ObservableProperty]
    private string? _statusMessage;

    public SeasonalitySettingsViewModel()
        : this(new MacroTargetService())
    {
    }

    public SeasonalitySettingsViewModel(IMacroTargetService macroService)
    {
        _macroService = macroService;
        Load();
    }

    private void Load()
    {
        MacroTargetSettings settings = _macroService.GetSettings();
        ShowSeasonalityNavItem = settings.ShowSeasonalityNavItem;
        GreenAlwaysHigh = settings.SeasonalityGreenAlwaysHigh;
        GreenFloor = settings.SeasonalityGreenFloor;
        RedCeiling = settings.SeasonalityRedCeiling;
        RedAlwaysLow = settings.SeasonalityRedAlwaysLow;
    }

    [RelayCommand]
    private void Save()
    {
        StatusMessage = null;
        if (GreenAlwaysHigh < GreenFloor || GreenFloor <= RedCeiling || RedCeiling < RedAlwaysLow)
        {
            StatusMessage = "Thresholds must be ordered: Always Green ≥ Green floor > Red ceiling ≥ Always Red.";
            return;
        }

        MacroTargetSettings settings = _macroService.GetSettings();
        settings.ShowSeasonalityNavItem = ShowSeasonalityNavItem;
        settings.SeasonalityGreenAlwaysHigh = GreenAlwaysHigh;
        settings.SeasonalityGreenFloor = GreenFloor;
        settings.SeasonalityRedCeiling = RedCeiling;
        settings.SeasonalityRedAlwaysLow = RedAlwaysLow;
        _macroService.SaveSettings(settings);
        StatusMessage = "Saved.";
        WeakReferenceMessenger.Default.Send(new NavVisibilityChangedMessage(ShowSeasonalityNavItem));
        // The recipe list re-scores and re-classifies against the new thresholds.
        WeakReferenceMessenger.Default.Send(new CardSettingsChangedMessage());
    }
}
