using Broccoli.Avalonia.Models;
using Broccoli.Avalonia.Shared;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Broccoli.Avalonia.Slices.Seasonality;

/// <summary>
/// A single produce item shown on the Seasonality page. Holds both the read-only display state
/// (in-season highlight for the selected month) and the editable fields used by the inline editor.
/// </summary>
public partial class ProduceItemRowViewModel : ViewModelBase
{
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

    [ObservableProperty]
    private string? _notes;

    /// <summary>The season currently being viewed (lowercase: "spring", "summer", ...).</summary>
    [ObservableProperty]
    private string _season = string.Empty;

    public ProduceItemRowViewModel(ProduceItem item, bool isNewItem)
    {
        Item = item;
        IsNewItem = isNewItem;
        _name = item.Name;
        _type = item.Type;
        _yearRound = item.YearRound;
        _inSpring = item.Seasons.Contains("spring", StringComparer.OrdinalIgnoreCase);
        _inSummer = item.Seasons.Contains("summer", StringComparer.OrdinalIgnoreCase);
        _inAutumn = item.Seasons.Contains("autumn", StringComparer.OrdinalIgnoreCase);
        _inWinter = item.Seasons.Contains("winter", StringComparer.OrdinalIgnoreCase);
        _notes = item.Notes;
    }

    public ProduceItem Item { get; }

    /// <summary>True for a not-yet-persisted item being created through the editor.</summary>
    public bool IsNewItem { get; }

    public string DisplayName => Item.Name;

    public string TypeLabel => string.Equals(Item.Type, "fruit", StringComparison.OrdinalIgnoreCase) ? "Fruit" : "Vegetable";

    public bool IsInSeason => Item.YearRound || Item.Seasons.Contains(Season, StringComparer.OrdinalIgnoreCase);

    public string SeasonColor => IsInSeason ? "#2ECC71" : "Gray";

    public string SeasonStatus => IsInSeason ? "In season" : "Out of season";

    public string SeasonSummary
    {
        get
        {
            if (Item.YearRound)
            {
                return "Year-round";
            }

            if (Item.Seasons.Count == 0)
            {
                return "No seasons set";
            }

            return string.Join(", ", Item.Seasons.Select(Capitalise));
        }
    }

    /// <summary>Season pickers are only meaningful for seasonal (non-year-round) produce.</summary>
    public bool CanEditSeasons => !YearRound;

    partial void OnSeasonChanged(string value)
    {
        OnPropertyChanged(nameof(IsInSeason));
        OnPropertyChanged(nameof(SeasonColor));
        OnPropertyChanged(nameof(SeasonStatus));
    }

    partial void OnYearRoundChanged(bool value) => OnPropertyChanged(nameof(CanEditSeasons));

    /// <summary>Refreshes computed display properties after <see cref="Item"/> has been edited and saved.</summary>
    public void RefreshDisplay()
    {
        OnPropertyChanged(nameof(DisplayName));
        OnPropertyChanged(nameof(TypeLabel));
        OnPropertyChanged(nameof(IsInSeason));
        OnPropertyChanged(nameof(SeasonColor));
        OnPropertyChanged(nameof(SeasonStatus));
        OnPropertyChanged(nameof(SeasonSummary));
    }

    private static string Capitalise(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        return char.ToUpperInvariant(value[0]) + value[1..];
    }
}
