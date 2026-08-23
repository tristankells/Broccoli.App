using Broccoli.Avalonia.Models;
using Broccoli.Avalonia.Shared;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Broccoli.Avalonia.Slices.Seasonality;

/// <summary>
/// A single produce item shown on the Seasonality page. Each month has a dropdown-selected
/// <see cref="SeasonalityState"/>; every field is bound directly to the row's controls and
/// persisted to the store via the save callback as it changes.
/// </summary>
public partial class ProduceItemRowViewModel : ViewModelBase
{
    private static readonly string[] s_typeOptions = ["fruit", "vegetable"];

    private static readonly SeasonalityState[] s_stateOptions =
        [SeasonalityState.OutOfSeason, SeasonalityState.PartiallyInSeason, SeasonalityState.InSeason];

    private readonly Action<ProduceItemRowViewModel>? _saveRequested;

    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private string _type;

    [ObservableProperty]
    private SeasonalityState _januaryState;

    [ObservableProperty]
    private SeasonalityState _februaryState;

    [ObservableProperty]
    private SeasonalityState _marchState;

    [ObservableProperty]
    private SeasonalityState _aprilState;

    [ObservableProperty]
    private SeasonalityState _mayState;

    [ObservableProperty]
    private SeasonalityState _juneState;

    [ObservableProperty]
    private SeasonalityState _julyState;

    [ObservableProperty]
    private SeasonalityState _augustState;

    [ObservableProperty]
    private SeasonalityState _septemberState;

    [ObservableProperty]
    private SeasonalityState _octoberState;

    [ObservableProperty]
    private SeasonalityState _novemberState;

    [ObservableProperty]
    private SeasonalityState _decemberState;

    /// <summary>Month (1..12) whose in-season state drives the row's accent indicator.</summary>
    [ObservableProperty]
    private int _currentMonth = 1;

    public ProduceItemRowViewModel(ProduceItem item, Action<ProduceItemRowViewModel>? saveRequested)
    {
        Item = item;
        _saveRequested = saveRequested;
        _name = item.Name;
        _type = item.Type;
        _januaryState = item.GetStateForMonth(1);
        _februaryState = item.GetStateForMonth(2);
        _marchState = item.GetStateForMonth(3);
        _aprilState = item.GetStateForMonth(4);
        _mayState = item.GetStateForMonth(5);
        _juneState = item.GetStateForMonth(6);
        _julyState = item.GetStateForMonth(7);
        _augustState = item.GetStateForMonth(8);
        _septemberState = item.GetStateForMonth(9);
        _octoberState = item.GetStateForMonth(10);
        _novemberState = item.GetStateForMonth(11);
        _decemberState = item.GetStateForMonth(12);
    }

    public ProduceItem Item { get; }

    /// <summary>Choices for the type column ("fruit" / "vegetable").</summary>
    public IReadOnlyList<string> TypeOptions => s_typeOptions;

    /// <summary>Choices for each month's state dropdown.</summary>
    public IReadOnlyList<SeasonalityState> StateOptions => s_stateOptions;

    /// <summary>Seasonality state for the month currently being viewed (drives the accent dot).</summary>
    public SeasonalityState CurrentState => Item.GetStateForMonth(CurrentMonth);

    /// <summary>Colour of the in-season accent for the current month.</summary>
    public string SeasonColor => StateColor(CurrentState);

    partial void OnCurrentMonthChanged(int value)
    {
        OnPropertyChanged(nameof(CurrentState));
        OnPropertyChanged(nameof(SeasonColor));
    }

    partial void OnNameChanged(string value)
    {
        Item.Name = value;
        RequestSave();
    }

    partial void OnTypeChanged(string value)
    {
        Item.Type = value;
        RequestSave();
    }

    partial void OnJanuaryStateChanged(SeasonalityState value) => SetMonth(1, value);

    partial void OnFebruaryStateChanged(SeasonalityState value) => SetMonth(2, value);

    partial void OnMarchStateChanged(SeasonalityState value) => SetMonth(3, value);

    partial void OnAprilStateChanged(SeasonalityState value) => SetMonth(4, value);

    partial void OnMayStateChanged(SeasonalityState value) => SetMonth(5, value);

    partial void OnJuneStateChanged(SeasonalityState value) => SetMonth(6, value);

    partial void OnJulyStateChanged(SeasonalityState value) => SetMonth(7, value);

    partial void OnAugustStateChanged(SeasonalityState value) => SetMonth(8, value);

    partial void OnSeptemberStateChanged(SeasonalityState value) => SetMonth(9, value);

    partial void OnOctoberStateChanged(SeasonalityState value) => SetMonth(10, value);

    partial void OnNovemberStateChanged(SeasonalityState value) => SetMonth(11, value);

    partial void OnDecemberStateChanged(SeasonalityState value) => SetMonth(12, value);

    private void SetMonth(int month, SeasonalityState value)
    {
        Item.SetStateForMonth(month, value);
        if (month == CurrentMonth)
        {
            OnPropertyChanged(nameof(CurrentState));
            OnPropertyChanged(nameof(SeasonColor));
        }

        RequestSave();
    }

    private void RequestSave() => _saveRequested?.Invoke(this);

    /// <summary>Colour used to render a seasonality state (green = in, orange = partial, gray = out).</summary>
    public static string StateColor(SeasonalityState state) => state switch
    {
        SeasonalityState.InSeason => "#2ECC71",
        SeasonalityState.PartiallyInSeason => "#F39C12",
        _ => "Gray",
    };
}
