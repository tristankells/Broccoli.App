using System.Collections.ObjectModel;
using Broccoli.Avalonia.Models;
using Broccoli.Avalonia.Shared;
using Broccoli.Avalonia.Slices.Pantry;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

namespace Broccoli.Avalonia.Slices.Groceries;

public partial class CartPreviewItem : ObservableObject
{
    public string DisplayName { get; init; } = string.Empty;
    public string FormattedLine { get; init; } = string.Empty;
    public string OriginalLine { get; init; } = string.Empty;
    public string FoodName { get; init; } = string.Empty;
    public bool IsMerge { get; init; }

    [ObservableProperty]
    private bool _isChecked = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowAddToPantry))]
    [NotifyPropertyChangedFor(nameof(ShowRemoveFromPantry))]
    [NotifyPropertyChangedFor(nameof(ShowPantryLabel))]
    private bool _isInPantry;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowAddToPantry))]
    private bool _addedToPantry;

    [ObservableProperty]
    private string _pantryCategoryLabel = string.Empty;

    [ObservableProperty]
    private string _pantryItemId = string.Empty;

    public bool ShowAddToPantry => !IsInPantry && !AddedToPantry;
    public bool ShowRemoveFromPantry => IsInPantry;
    public bool ShowPantryLabel => IsInPantry;

    public Action<CartPreviewItem>? AddToPantryRequested { get; set; }
    public Action<CartPreviewItem>? RemoveFromPantryRequested { get; set; }

    [RelayCommand]
    private void AddToPantry()
    {
        AddToPantryRequested?.Invoke(this);
    }

    [RelayCommand]
    private void RemoveFromPantry()
    {
        RemoveFromPantryRequested?.Invoke(this);
    }
}

public partial class AddToCartDialogViewModel : ViewModelBase
{
    private readonly IngredientCartService _cartService;
    private readonly IPantryService _pantryService;
    private readonly List<string> _ingredientLines;

    [ObservableProperty]
    private string _recipeName = string.Empty;

    public ObservableCollection<CartPreviewItem> PreviewItems { get; } = [];

    public Action? RequestClose { get; set; }

    public bool HasItems => PreviewItems.Count > 0;

    public AddToCartDialogViewModel(
        IngredientCartService cartService,
        IPantryService pantryService,
        string recipeName,
        List<string> ingredientLines)
    {
        _cartService = cartService;
        _pantryService = pantryService;
        _recipeName = recipeName;
        _ingredientLines = ingredientLines;

        List<CartPreviewData> previewData = _cartService.PreviewAddToCart(ingredientLines);
        List<CartPreviewItem> items = [];

        foreach (CartPreviewData data in previewData)
        {
            PantryItem? pantryItem = _pantryService.FindByName(data.FoodName);
            bool inPantry = pantryItem is not null;
            string categoryLabel = pantryItem?.Category switch
            {
                PantryCategory.AlwaysHave => "Always have",
                PantryCategory.CheckIfHave => "Check if have",
                _ => string.Empty,
            };

            var item = new CartPreviewItem
            {
                DisplayName = data.DisplayName,
                FormattedLine = data.FormattedLine,
                OriginalLine = data.OriginalLine,
                FoodName = data.FoodName,
                IsMerge = data.IsMerge,
                IsChecked = !inPantry,
                IsInPantry = inPantry,
                PantryCategoryLabel = categoryLabel,
                PantryItemId = pantryItem?.Id ?? string.Empty,
            };
            item.AddToPantryRequested = HandleAddToPantry;
            item.RemoveFromPantryRequested = HandleRemoveFromPantry;
            items.Add(item);
        }

        foreach (CartPreviewItem item in items.OrderBy(i => i.IsInPantry))
        {
            PreviewItems.Add(item);
        }

        OnPropertyChanged(nameof(HasItems));
    }

    private void HandleAddToPantry(CartPreviewItem item)
    {
        PantryItem created = _pantryService.Add(new PantryItem
        {
            Name = StripMeasurements(item.FoodName),
            Category = PantryCategory.CheckIfHave,
        });

        item.PantryItemId = created.Id;
        item.PantryCategoryLabel = "Check if have";
        item.AddedToPantry = true;
        item.IsChecked = false;
        item.IsInPantry = true;
    }

    private void HandleRemoveFromPantry(CartPreviewItem item)
    {
        if (!string.IsNullOrEmpty(item.PantryItemId))
        {
            _pantryService.Delete(item.PantryItemId);
        }

        item.IsInPantry = false;
        item.AddedToPantry = false;
        item.PantryCategoryLabel = string.Empty;
        item.PantryItemId = string.Empty;
        item.IsChecked = true;
    }

    [RelayCommand]
    private void Confirm()
    {
        List<string> checkedLines = PreviewItems
            .Where(item => item.IsChecked)
            .Select(item => item.OriginalLine)
            .ToList();

        if (checkedLines.Count > 0)
        {
            _cartService.AddToCart(checkedLines);
        }

        WeakReferenceMessenger.Default.Send(new GroceryListChangedMessage());
        RequestClose?.Invoke();
    }

    [RelayCommand]
    private void Cancel()
    {
        RequestClose?.Invoke();
    }

    private static readonly System.Text.RegularExpressions.Regex s_measurementPattern = new(
        @"\b(cups?|tablespoons?|tbl|tbsps?|teaspoons?|tsps?|grams?|kilograms?|kgs?|milliliters?|millilitres?|mls?|liters?|litres?|ls?|ounces?|oz|lbs?|pounds?|drizzle|pinch|packs?|twin\s+pack|medium|large|small|heads?|cans?|cloves?|bunches?|stalks?|slices?|pieces?|sheets?|of)\s*",
        System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Compiled);

    private static string StripMeasurements(string foodName)
    {
        string result = s_measurementPattern.Replace(foodName, " ").Trim();
        while (result.Contains("  "))
        {
            result = result.Replace("  ", " ");
        }

        return string.IsNullOrWhiteSpace(result) ? foodName : result;
    }
}
