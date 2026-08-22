using System.Collections.ObjectModel;
using Broccoli.Avalonia.Models;
using Broccoli.Avalonia.Seasonality;
using Broccoli.Avalonia.Shared;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

namespace Broccoli.Avalonia.Slices.Seasonality;

public partial class SeasonalityViewModel : ViewModelBase
{
    private readonly ISeasonalityDataStore _store;

    public static IReadOnlyList<string> MonthNames { get; } =
    [
        "January", "February", "March", "April", "May", "June",
        "July", "August", "September", "October", "November", "December",
    ];

    public static IReadOnlyList<string> TypeFilters { get; } = ["All", "Fruit", "Vegetable"];

    public static IReadOnlyList<string> Types { get; } = ["fruit", "vegetable"];

    public ObservableCollection<ProduceItemRowViewModel> Items { get; } = new();

    public ObservableCollection<ProduceItemRowViewModel> FilteredItems { get; } = new();

    [ObservableProperty]
    private int _selectedMonthIndex;

    [ObservableProperty]
    private string? _searchText;

    [ObservableProperty]
    private int _selectedTypeFilterIndex;

    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    private ProduceItemRowViewModel? _editingRow;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private int _inSeasonCount;

    [ObservableProperty]
    private int _totalCount;

    public SeasonalityViewModel()
        : this(new SeasonalityDataStore())
    {
    }

    public SeasonalityViewModel(ISeasonalityDataStore store)
    {
        _store = store;
        SelectedMonthIndex = DateTime.Today.Month - 1;
        Load();
        WeakReferenceMessenger.Default.Register<SeasonalityDataChangedMessage>(this, (_, _) => Load());
    }

    public string EditorTitle => EditingRow?.IsNewItem == true ? "Add produce item" : "Edit produce item";

    public string CurrentMonthName => MonthNames[SelectedMonthIndex];

    public string CurrentSeasonName => Capitalise(SeasonHelper.GetCurrentSeason(SelectedMonthDate));

    public string SeasonBannerText => $"{CurrentMonthName} → {CurrentSeasonName}";

    private DateTime SelectedMonthDate => new(2000, SelectedMonthIndex + 1, 1);

    private string CurrentSeason => SeasonHelper.GetCurrentSeason(SelectedMonthDate);

    partial void OnSelectedMonthIndexChanged(int value)
    {
        OnPropertyChanged(nameof(CurrentMonthName));
        OnPropertyChanged(nameof(CurrentSeasonName));
        OnPropertyChanged(nameof(SeasonBannerText));
        RefreshSeason();
    }

    partial void OnSearchTextChanged(string? value) => ApplyFilters();

    partial void OnSelectedTypeFilterIndexChanged(int value) => ApplyFilters();

    partial void OnEditingRowChanged(ProduceItemRowViewModel? value) => OnPropertyChanged(nameof(EditorTitle));

    [RelayCommand]
    private void AddItem()
    {
        var item = new ProduceItem { Type = "fruit" };
        EditingRow = new ProduceItemRowViewModel(item, isNewItem: true);
        IsEditing = true;
        ErrorMessage = null;
    }

    [RelayCommand]
    private void StartEdit(ProduceItemRowViewModel? row)
    {
        if (row is null)
        {
            return;
        }

        EditingRow = row;
        IsEditing = true;
        ErrorMessage = null;
    }

    [RelayCommand]
    private void CancelEdit()
    {
        IsEditing = false;
        EditingRow = null;
        ErrorMessage = null;
    }

    [RelayCommand]
    private void SaveEdit()
    {
        if (EditingRow is null)
        {
            return;
        }

        ProduceItemRowViewModel row = EditingRow;
        if (string.IsNullOrWhiteSpace(row.Name))
        {
            ErrorMessage = "Name is required.";
            return;
        }

        ErrorMessage = null;

        row.Item.Name = row.Name.Trim();
        row.Item.Type = string.Equals(row.Type, "vegetable", StringComparison.OrdinalIgnoreCase) ? "vegetable" : "fruit";
        row.Item.YearRound = row.YearRound;
        row.Item.Notes = string.IsNullOrWhiteSpace(row.Notes) ? null : row.Notes.Trim();
        row.Item.Seasons = new List<string>();
        if (row.InSpring)
        {
            row.Item.Seasons.Add("spring");
        }

        if (row.InSummer)
        {
            row.Item.Seasons.Add("summer");
        }

        if (row.InAutumn)
        {
            row.Item.Seasons.Add("autumn");
        }

        if (row.InWinter)
        {
            row.Item.Seasons.Add("winter");
        }

        if (row.IsNewItem)
        {
            row.Item.Id = GenerateId(row.Item.Name);
            _store.Add(row.Item);
        }
        else
        {
            _store.Update(row.Item);
        }

        IsEditing = false;
        EditingRow = null;
        Load();
    }

    [RelayCommand]
    private void DeleteItem(ProduceItemRowViewModel? row)
    {
        if (row is null)
        {
            return;
        }

        _store.Delete(row.Item.Id);
        Load();
    }

    [RelayCommand]
    private void ResetData()
    {
        _store.Reset();
        Load();
    }

    private void Load()
    {
        Items.Clear();
        foreach (ProduceItem item in _store.GetAll())
        {
            Items.Add(new ProduceItemRowViewModel(item, isNewItem: false));
        }

        RefreshSeason();
        ApplyFilters();
    }

    private void RefreshSeason()
    {
        string season = CurrentSeason;
        int inSeason = 0;
        foreach (ProduceItemRowViewModel row in Items)
        {
            row.Season = season;
            if (row.IsInSeason)
            {
                inSeason++;
            }
        }

        InSeasonCount = inSeason;
        TotalCount = Items.Count;
    }

    private void ApplyFilters()
    {
        IEnumerable<ProduceItemRowViewModel> query = Items;

        string typeFilter = TypeFilters[SelectedTypeFilterIndex];
        if (!string.Equals(typeFilter, "All", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(r => string.Equals(r.Type, typeFilter, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            string search = SearchText.Trim();
            query = query.Where(r => r.DisplayName.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        FilteredItems.Clear();
        foreach (ProduceItemRowViewModel row in query)
        {
            FilteredItems.Add(row);
        }
    }

    private static string GenerateId(string name)
    {
        string slug = name.ToLowerInvariant().Trim().Replace(' ', '-');
        slug = new string(slug.Where(c => char.IsLetterOrDigit(c) || c == '-').ToArray());
        if (string.IsNullOrEmpty(slug))
        {
            slug = "produce";
        }

        return $"{slug}-{Guid.NewGuid():N}"[..Math.Min(slug.Length + 9, 40)];
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
