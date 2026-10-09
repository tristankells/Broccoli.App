using Broccoli.Avalonia.Models;
using Broccoli.Avalonia.Shared;
using Broccoli.Avalonia.Slices.Planning;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Broccoli.Avalonia.Slices.Settings;

public partial class GroceriesSettingsViewModel : ViewModelBase
{
    private readonly IMacroTargetService _macroService;

    [ObservableProperty]
    private GroceryUnitNormalization _unitNormalization = GroceryUnitNormalization.Off;

    /// <summary>The normalization option currently selected in the ComboBox.</summary>
    [ObservableProperty]
    private UnitNormalizationOption? _selectedUnitNormalizationOption;

    [ObservableProperty]
    private string? _statusMessage;

    /// <summary>Choices for how grocery items are normalized when added to the list.</summary>
    public IReadOnlyList<UnitNormalizationOption> UnitNormalizationOptions { get; } =
    [
        new("Off", "Keep items exactly as the recipe specifies them.", GroceryUnitNormalization.Off),
        new("Grams", "Convert countable items to grams when a food match with a known weight exists, e.g. \"1 onion\" becomes \"110g Onion\" with a \"(~1 medium onion)\" hint.", GroceryUnitNormalization.Grams),
        new("Pounds", "Convert countable items to pounds when a food match with a known weight exists.", GroceryUnitNormalization.Pounds),
    ];

    public GroceriesSettingsViewModel()
        : this(new MacroTargetService())
    {
    }

    public GroceriesSettingsViewModel(IMacroTargetService macroService)
    {
        _macroService = macroService;
        Load();
    }

    private void Load()
    {
        MacroTargetSettings settings = _macroService.GetSettings();
        UnitNormalization = settings.GroceryUnitNormalization;
        SelectedUnitNormalizationOption = UnitNormalizationOptions.FirstOrDefault(o => o.Value == UnitNormalization)
            ?? UnitNormalizationOptions[0];
    }

    partial void OnSelectedUnitNormalizationOptionChanged(UnitNormalizationOption? value)
    {
        if (value is not null)
        {
            UnitNormalization = value.Value;
        }
    }

    [RelayCommand]
    private void Save()
    {
        StatusMessage = null;
        MacroTargetSettings settings = _macroService.GetSettings();
        settings.GroceryUnitNormalization = UnitNormalization;
        _macroService.SaveSettings(settings);
        StatusMessage = "Saved.";
    }
}

/// <summary>One selectable grocery normalization mode with a human-readable label and description.</summary>
public sealed record UnitNormalizationOption(string Label, string Description, GroceryUnitNormalization Value);
