using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using Broccoli.Avalonia.IngredientParsing;
using Broccoli.Avalonia.Models;
using Broccoli.Avalonia.Shared;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

namespace Broccoli.Avalonia.Slices.Groceries;

public partial class GroceriesViewModel : ViewModelBase
{
    private readonly IGroceryListService _groceryListService;
    private readonly IngredientParserService? _parser;

    [ObservableProperty]
    private string _newItemText = string.Empty;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _isLoading;

    public GroceriesViewModel()
        : this(new GroceryListService(), null!)
    {
    }

    public GroceriesViewModel(IGroceryListService groceryListService, IngredientParserService? parser = null)
    {
        _groceryListService = groceryListService;
        _parser = parser;
        WeakReferenceMessenger.Default.Register<GroceryListChangedMessage>(this, (_, _) => LoadItems());
        LoadItems();
    }

    public Func<string, Task>? SetClipboardTextAsync { get; set; }

    public ObservableCollection<GroceryListItem> Items { get; } = new();

    public string StatusText
    {
        get
        {
            int checkedCount = Items.Count(i => i.IsChecked);
            return $"{checkedCount} of {Items.Count} checked";
        }
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
        catch (Exception exception)
        {
            ErrorMessage = $"Error loading grocery list: {exception.Message}";
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

                // Only offer hints when the food's unit of measure relates to the item the user typed,
                // e.g. "Apple" ↔ "Medium Apple" or "2 cups flour" ↔ "cup". This prevents over-eager
                // hints for foods measured purely by weight.
                IEnumerable<ParsedIngredientMatch> filteredByMeasureMatches = matches
                    .Where(match => match.MatchedFood is not null &&
                        (
                            SharesComponent(match.MatchedFood.Measure, match.MatchedFood.Name) ||
                            SharesComponent(match.MatchedFood.Measure, match.ParsedIngredient.CanonicalUnit))
                        );

                hint = filteredByMeasureMatches.FirstOrDefault()?.GetQuantityHint();
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
        catch (Exception exception)
        {
            ErrorMessage = $"Error adding item: {exception.Message}";
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
        catch (Exception exception)
        {
            item.IsChecked = !item.IsChecked;
            ErrorMessage = $"Error updating item: {exception.Message}";
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
        catch (Exception exception)
        {
            ErrorMessage = $"Error deleting item: {exception.Message}";
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
    private void StartEdit(GroceryListItem item)
    {
        item.EditText = item.Name;
        item.IsEditing = true;
    }

    [RelayCommand]
    private void CommitEdit(GroceryListItem item)
    {
        if (!item.IsEditing)
        {
            return;
        }

        string newName = item.EditText.Trim();
        if (string.IsNullOrWhiteSpace(newName))
        {
            item.IsEditing = false;
            return;
        }

        item.Name = newName;
        item.IsEditing = false;

        if (_parser is not null)
        {
            List<ParsedIngredientMatch> matches = _parser.ParseAndMatchIngredients(item.Name);
            item.QuantityHint = matches.FirstOrDefault()?.GetQuantityHint();
        }

        try
        {
            _groceryListService.Update(item);
        }
        catch (Exception exception)
        {
            ErrorMessage = $"Error updating item: {exception.Message}";
        }
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
        catch (Exception exception)
        {
            ErrorMessage = $"Error resetting list: {exception.Message}";
        }
    }

    private static bool SharesComponent(string first, string second)
    {
        if (first is null || second is null)
        {
            return false;
        }

        return first.Split(" ").Intersect(second.Split(" "), StringComparer.InvariantCultureIgnoreCase).Any();
    }
}
