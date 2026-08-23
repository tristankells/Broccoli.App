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
    private bool _suppressReload;

    public static IReadOnlyList<string> MonthNames { get; } =
    [
        "January", "February", "March", "April", "May", "June",
        "July", "August", "September", "October", "November", "December",
    ];

    public static IReadOnlyList<string> TypeFilters { get; } = ["All", "Fruit", "Vegetable"];

    public ObservableCollection<ProduceItemRowViewModel> Items { get; } = new();

    private ObservableCollection<ProduceItemRowViewModel> _filteredItems = new();

    public ObservableCollection<ProduceItemRowViewModel> FilteredItems
    {
        get => _filteredItems;
        private set
        {
            if (ReferenceEquals(_filteredItems, value))
            {
                return;
            }

            _filteredItems = value;
            OnPropertyChanged();
        }
    }

    [ObservableProperty]
    private int _selectedMonthIndex;

    [ObservableProperty]
    private string? _searchText;

    [ObservableProperty]
    private int _selectedTypeFilterIndex;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private int _inSeasonCount;

    [ObservableProperty]
    private int _partiallyInSeasonCount;

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
        WeakReferenceMessenger.Default.Register<SeasonalityDataChangedMessage>(this, (_, _) =>
        {
            if (!_suppressReload)
            {
                Load();
            }
        });
    }

    public string CurrentMonthName => MonthNames[SelectedMonthIndex];

    public string CurrentSeasonName => Capitalise(SeasonHelper.GetCurrentSeason(SelectedMonthDate));

    public string SeasonBannerText => $"{CurrentMonthName} → {CurrentSeasonName}";

    private DateTime SelectedMonthDate => new(2000, SelectedMonthIndex + 1, 1);

    partial void OnSelectedMonthIndexChanged(int value)
    {
        OnPropertyChanged(nameof(CurrentMonthName));
        OnPropertyChanged(nameof(CurrentSeasonName));
        OnPropertyChanged(nameof(SeasonBannerText));
        RefreshSeason();
    }

    partial void OnSearchTextChanged(string? value) => ApplyFilters();

    partial void OnSelectedTypeFilterIndexChanged(int value) => ApplyFilters();

    [RelayCommand]
    private void AddItem()
    {
        var item = new ProduceItem { Id = GenerateId("new-item"), Name = "New item", Type = "fruit" };
        try
        {
            RunSuppressed(() => _store.Add(item));
            ErrorMessage = null;
            Load();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to add: {ex.Message}";
        }
    }

    [RelayCommand]
    private void DeleteItem(ProduceItemRowViewModel? row)
    {
        if (row is null)
        {
            return;
        }

        try
        {
            RunSuppressed(() => _store.Delete(row.Item.Id));
            ErrorMessage = null;
            Load();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to delete: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ResetData()
    {
        try
        {
            RunSuppressed(() => _store.Reset());
            ErrorMessage = null;
            Load();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to reset: {ex.Message}";
        }
    }

    private void SaveRow(ProduceItemRowViewModel row)
    {
        try
        {
            RunSuppressed(() => _store.Update(row.Item));
            ErrorMessage = null;
            RefreshSeason();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to save: {ex.Message}";
        }
    }

    private void RunSuppressed(Action action)
    {
        _suppressReload = true;
        try
        {
            action();
        }
        finally
        {
            _suppressReload = false;
        }
    }

    private void Load()
    {
        Items.Clear();
        foreach (ProduceItem item in _store.GetAll())
        {
            Items.Add(new ProduceItemRowViewModel(item, SaveRow));
        }

        RefreshSeason();
        ApplyFilters();
    }

    private void RefreshSeason()
    {
        int month = SelectedMonthIndex + 1;
        int inSeason = 0, partial = 0;
        foreach (ProduceItemRowViewModel row in Items)
        {
            row.CurrentMonth = month;
            switch (row.CurrentState)
            {
                case SeasonalityState.InSeason:
                    inSeason++;
                    break;
                case SeasonalityState.PartiallyInSeason:
                    partial++;
                    break;
            }
        }

        InSeasonCount = inSeason;
        PartiallyInSeasonCount = partial;
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
            query = query.Where(r => r.Name.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        // Replacing the collection (single Reset) is far cheaper for the list control than
        // clearing and re-adding items one at a time.
        FilteredItems = new ObservableCollection<ProduceItemRowViewModel>(query);
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
