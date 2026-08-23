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
    }

    [RelayCommand]
    private void Save()
    {
        StatusMessage = null;
        MacroTargetSettings settings = _macroService.GetSettings();
        settings.ShowSeasonalityNavItem = ShowSeasonalityNavItem;
        _macroService.SaveSettings(settings);
        StatusMessage = "Saved.";
        WeakReferenceMessenger.Default.Send(new NavVisibilityChangedMessage(ShowSeasonalityNavItem));
    }
}
