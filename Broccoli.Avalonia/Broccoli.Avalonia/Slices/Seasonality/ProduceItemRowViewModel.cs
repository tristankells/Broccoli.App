using Broccoli.Avalonia.Models;
using Broccoli.Avalonia.Shared;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Broccoli.Avalonia.Slices.Seasonality;

/// <summary>
/// A single produce item shown on the Seasonality page. Every editable field (name, type, seasons,
/// year-round) is bound directly to the row's controls and persisted to the store via the save
/// callback as it changes, so no separate edit mode is needed.
/// </summary>
public partial class ProduceItemRowViewModel : ViewModelBase
{
    private static readonly string[] s_typeOptions = ["fruit", "vegetable"];

    private readonly Action<ProduceItemRowViewModel>? _saveRequested;

    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private string _type;

    [ObservableProperty]
    private bool _yearRound;

    [ObservableProperty]
    private bool _inSpring;

    [ObservableProperty]
    private bool _inSummer;

    [ObservableProperty]
    private bool _inAutumn;

    [ObservableProperty]
    private bool _inWinter;

    /// <summary>The season currently being viewed (lowercase: "spring", "summer", ...).</summary>
    [ObservableProperty]
    private string _season = string.Empty;

    public ProduceItemRowViewModel(ProduceItem item, Action<ProduceItemRowViewModel>? saveRequested)
    {
        Item = item;
        _saveRequested = saveRequested;
        _name = item.Name;
        _type = item.Type;
        _yearRound = item.YearRound;
        _inSpring = item.Seasons.Contains("spring", StringComparer.OrdinalIgnoreCase);
        _inSummer = item.Seasons.Contains("summer", StringComparer.OrdinalIgnoreCase);
        _inAutumn = item.Seasons.Contains("autumn", StringComparer.OrdinalIgnoreCase);
        _inWinter = item.Seasons.Contains("winter", StringComparer.OrdinalIgnoreCase);
    }

    public ProduceItem Item { get; }

    /// <summary>Choices for the type column ("fruit" / "vegetable").</summary>
    public IReadOnlyList<string> TypeOptions => s_typeOptions;

    public bool IsInSeason => Item.YearRound || Item.Seasons.Contains(Season, StringComparer.OrdinalIgnoreCase);

    public string SeasonColor => IsInSeason ? "#2ECC71" : "Gray";

    /// <summary>Season pickers are only meaningful for seasonal (non-year-round) produce.</summary>
    public bool CanEditSeasons => !YearRound;

    partial void OnSeasonChanged(string value)
    {
        OnPropertyChanged(nameof(IsInSeason));
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

    partial void OnYearRoundChanged(bool value)
    {
        Item.YearRound = value;
        OnPropertyChanged(nameof(CanEditSeasons));
        RaiseSeasonDisplayChanged();
        RequestSave();
    }

    partial void OnInSpringChanged(bool value) => ApplySeasons();

    partial void OnInSummerChanged(bool value) => ApplySeasons();

    partial void OnInAutumnChanged(bool value) => ApplySeasons();

    partial void OnInWinterChanged(bool value) => ApplySeasons();

    private void ApplySeasons()
    {
        Item.Seasons = new List<string>();
        if (InSpring)
        {
            Item.Seasons.Add("spring");
        }

        if (InSummer)
        {
            Item.Seasons.Add("summer");
        }

        if (InAutumn)
        {
            Item.Seasons.Add("autumn");
        }

        if (InWinter)
        {
            Item.Seasons.Add("winter");
        }

        RaiseSeasonDisplayChanged();
        RequestSave();
    }

    private void RaiseSeasonDisplayChanged()
    {
        OnPropertyChanged(nameof(IsInSeason));
        OnPropertyChanged(nameof(SeasonColor));
    }

    private void RequestSave() => _saveRequested?.Invoke(this);
}
