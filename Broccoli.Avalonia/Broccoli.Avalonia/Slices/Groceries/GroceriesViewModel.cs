using Broccoli.Avalonia.IngredientParsing;
using Broccoli.Avalonia.Models;
using Broccoli.Avalonia.Shared;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System.Collections.ObjectModel;

namespace Broccoli.Avalonia.Slices.Groceries;

public partial class GroceriesViewModel : ViewModelBase
{
    private readonly IGroceryListService _groceryListService;
    private readonly IngredientParserService? _parser;

    public Func<string, Task>? SetClipboardTextAsync { get; set; }

    public ObservableCollection<GroceryListItem> Items { get; } = new();

    [ObservableProperty] private string _newItemText = string.Empty;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private bool _isLoading;

    public string StatusText
    {
        get
        {
            int checkedCount = Items.Count(i => i.IsChecked);
            return $"{checkedCount} of {Items.Count} checked";
        }
    }

    public GroceriesViewModel() : this(new GroceryListService(), null!)
    {
    }

    public GroceriesViewModel(IGroceryListService groceryListService, IngredientParserService? parser = null)
    {
        _groceryListService = groceryListService;
        _parser = parser;
        WeakReferenceMessenger.Default.Register<GroceryListChangedMessage>(this, (_, _) => LoadItems());
        LoadItems();
    }

    private void LoadItems()
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            List<GroceryListItem> items = _groceryListService.GetAll();
            Items.Clear();
            foreach (GroceryListItem item in items)
            {
                Items.Add(item);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error loading grocery list: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(StatusText));
        }
    }

    [RelayCommand]
    private void AddItem()
    {
        string name = NewItemText.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        ErrorMessage = null;

        try
        {
            string? hint = null;
            if (_parser is not null)
            {
                List<ParsedIngredientMatch> matches = _parser.ParseAndMatchIngredients(name);
                hint = matches.FirstOrDefault()?.GetQuantityHint();
            }

            var item = new GroceryListItem
            {
                Name = name,
                IsChecked = false,
                QuantityHint = hint,
            };

            GroceryListItem created = _groceryListService.Add(item);
            Items.Insert(0, created);
            NewItemText = string.Empty;
            OnPropertyChanged(nameof(StatusText));
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error adding item: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ToggleItem(GroceryListItem item)
    {
        item.IsChecked = !item.IsChecked;
        OnPropertyChanged(nameof(StatusText));

        try
        {
            _groceryListService.Update(item);
        }
        catch (Exception ex)
        {
            item.IsChecked = !item.IsChecked;
            ErrorMessage = $"Error updating item: {ex.Message}";
            OnPropertyChanged(nameof(StatusText));
        }
    }

    [RelayCommand]
    private void DeleteItem(GroceryListItem item)
    {
        Items.Remove(item);
        OnPropertyChanged(nameof(StatusText));

        try
        {
            _groceryListService.Delete(item.Id);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error deleting item: {ex.Message}";
            Items.Add(item);
            OnPropertyChanged(nameof(StatusText));
        }
    }

    [RelayCommand]
    private async Task CopyToClipboard()
    {
        if (SetClipboardTextAsync is null)
        {
            return;
        }

        List<GroceryListItem> uncheckedItems = Items
            .Where(item => !item.IsChecked)
            .ToList();

        if (uncheckedItems.Count == 0)
        {
            return;
        }

        var lines = new List<string>(uncheckedItems.Count);
        foreach (GroceryListItem item in uncheckedItems)
        {
            if (item.QuantityHint is not null)
            {
                lines.Add(item.Name + " " + item.QuantityHint);
            }
            else
            {
                lines.Add(item.Name);
            }
        }

        await SetClipboardTextAsync(string.Join(Environment.NewLine, lines));
    }

    [RelayCommand]
    private void ResetList()
    {
        try
        {
            _groceryListService.Reset();
            Items.Clear();
            OnPropertyChanged(nameof(StatusText));
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error resetting list: {ex.Message}";
        }
    }
}
