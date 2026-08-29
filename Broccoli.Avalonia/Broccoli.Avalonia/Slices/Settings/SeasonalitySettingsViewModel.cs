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
    private SeasonalityInfluenceMode _influenceMode = SeasonalityInfluenceMode.Gentle;

    /// <summary>The influence-mode option currently selected in the ComboBox.</summary>
    [ObservableProperty]
    private InfluenceModeOption? _selectedInfluenceOption;

    /// <summary>Percent of the library above which a score ranks as Green (default 67).</summary>
    [ObservableProperty]
    private double _topThirdPercent = 67;

    /// <summary>Percent of the library below which a score ranks as Red (default 33).</summary>
    [ObservableProperty]
    private double _bottomThirdPercent = 33;

    [ObservableProperty]
    private string? _statusMessage;

    /// <summary>Choices for the scarcity influence mode, shown in the settings ComboBox.</summary>
    public IReadOnlyList<InfluenceModeOption> InfluenceModeOptions { get; } =
    [
        new InfluenceModeOption("Off", "Every ingredient counts by its weight only - no scarcity weighting.", SeasonalityInfluenceMode.None),
        new InfluenceModeOption("Gentle", "A short-season item counts ~2x a year-round staple (default).", SeasonalityInfluenceMode.Gentle),
        new InfluenceModeOption("Moderate", "A short-season item counts ~4x a year-round staple.", SeasonalityInfluenceMode.Moderate),
        new InfluenceModeOption("Strong", "A short-season item counts up to ~8x - out-of-season rare produce dominates.", SeasonalityInfluenceMode.Strong),
    ];

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
        SelectedInfluenceOption = InfluenceModeOptions.FirstOrDefault(o => o.Value == settings.SeasonalityInfluenceMode)
            ?? InfluenceModeOptions[1];
        TopThirdPercent = Math.Round(settings.SeasonalityTopPercentile * 100);
        BottomThirdPercent = Math.Round(settings.SeasonalityBottomPercentile * 100);
    }

    partial void OnSelectedInfluenceOptionChanged(InfluenceModeOption? value)
    {
        if (value is not null)
        {
            InfluenceMode = value.Value;
        }
    }

    [RelayCommand]
    private void Save()
    {
        StatusMessage = null;
        if (GreenAlwaysHigh < GreenFloor || GreenFloor <= RedCeiling || RedCeiling < RedAlwaysLow)
        {
            StatusMessage = "Score thresholds must be ordered: Always Green ≥ Green floor > Red ceiling ≥ Always Red.";
            return;
        }

        if (TopThirdPercent <= BottomThirdPercent || TopThirdPercent > 99 || BottomThirdPercent < 1)
        {
            StatusMessage = "The Green ranking % must be greater than the Red ranking %, and both must be between 1 and 99.";
            return;
        }

        MacroTargetSettings settings = _macroService.GetSettings();
        settings.ShowSeasonalityNavItem = ShowSeasonalityNavItem;
        settings.SeasonalityGreenAlwaysHigh = GreenAlwaysHigh;
        settings.SeasonalityGreenFloor = GreenFloor;
        settings.SeasonalityRedCeiling = RedCeiling;
        settings.SeasonalityRedAlwaysLow = RedAlwaysLow;
        settings.SeasonalityInfluenceMode = InfluenceMode;
        settings.SeasonalityTopPercentile = TopThirdPercent / 100.0;
        settings.SeasonalityBottomPercentile = BottomThirdPercent / 100.0;
        _macroService.SaveSettings(settings);
        StatusMessage = "Saved.";
        WeakReferenceMessenger.Default.Send(new NavVisibilityChangedMessage(ShowSeasonalityNavItem));
        // The recipe list re-scores and re-classifies against the new settings.
        WeakReferenceMessenger.Default.Send(new CardSettingsChangedMessage());
    }
}

/// <summary>One selectable scarcity-influence mode with a human-readable label and description.</summary>
public sealed record InfluenceModeOption(string Label, string Description, SeasonalityInfluenceMode Value);
