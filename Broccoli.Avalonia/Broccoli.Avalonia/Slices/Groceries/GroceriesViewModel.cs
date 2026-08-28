using System.Collections.ObjectModel;
using Broccoli.Avalonia.IngredientParsing;
using Broccoli.Avalonia.Models;
using Broccoli.Avalonia.Shared;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FuzzySharp;

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
            ParsedIngredientMatch? match = FindQuantityHintMatch(name);
            string? hint = match?.GetQuantityHint();

            var item = new GroceryListItem
            {
                Name = name,
                IsChecked = false,
                QuantityHint = hint ?? string.Empty,
                MatchedFoodInfo = hint is not null ? FormatMatchInfo(match!) : null,
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

        ParsedIngredientMatch? match = FindQuantityHintMatch(item.Name);
        string? hint = match?.GetQuantityHint();
        item.QuantityHint = hint ?? string.Empty;
        item.MatchedFoodInfo = hint is not null ? FormatMatchInfo(match!) : null;

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

    private ParsedIngredientMatch? FindQuantityHintMatch(string itemName)
    {
        if (_parser is null)
        {
            return null;
        }

        List<ParsedIngredientMatch> matches = _parser.ParseAndMatchIngredients(itemName);

        // Only offer hints when the food's unit of measure relates to the item the user typed,
        // e.g. "Apple" ↔ "Medium Apple", "Potato" ↔ "Potatoes", or "2 cups flour" ↔ "cup".
        // This prevents over-eager hints for foods measured purely by weight.
        return matches
            .Where(match => match.MatchedFood is not null && MeasureRelatesToItem(match))
            .FirstOrDefault();
    }

    private static string FormatMatchInfo(ParsedIngredientMatch match)
    {
        string matchPercent = $"{match.MatchScore * 100:0}";
        return $"{match.MatchedFood!.Name} ({matchPercent}% match, {match.MatchMethod})";
    }

    private static bool MeasureRelatesToItem(ParsedIngredientMatch match)
    {
        Food food = match.MatchedFood!;
        string measure = food.Measure;

        // The unit the user typed lines up with the measure, e.g. "2 cups flour" ↔ "cup".
        if (!string.IsNullOrEmpty(match.ParsedIngredient.CanonicalUnit) &&
            SharesComponent(measure, match.ParsedIngredient.CanonicalUnit))
        {
            return true;
        }

        // A single fuzzy pass over the measure tolerates plurals and word order,
        // e.g. "Potato" ↔ "Potatoes" or "Medium Apple" ↔ "Apple".
        return MeasureFuzzyMatches(measure, food.Name)
            || MeasureFuzzyMatches(measure, match.ParsedIngredient.FoodDescription);
    }

    private static bool MeasureFuzzyMatches(string measure, string itemText)
    {
        if (string.IsNullOrWhiteSpace(measure) || string.IsNullOrWhiteSpace(itemText))
        {
            return false;
        }

        return measure.Split(" ").Any(measureToken =>
            itemText.Split(" ").Any(itemToken =>
                Fuzz.Ratio(measureToken, itemToken) >= 60));
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
