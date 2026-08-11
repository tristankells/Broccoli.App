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
    public string FoodName { get; init; } = string.Empty;
    public bool IsMerge { get; init; }

    [ObservableProperty]
    private bool _isChecked = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowAddToPantry))]
    private bool _isInPantry;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowAddToPantry))]
    private bool _addedToPantry;

    public bool ShowAddToPantry => !IsInPantry && !AddedToPantry;
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
        foreach (CartPreviewData data in previewData)
        {
            bool inPantry = _pantryService.Exists(data.FoodName);

            PreviewItems.Add(new CartPreviewItem
            {
                DisplayName = data.DisplayName,
                FormattedLine = data.FormattedLine,
                FoodName = data.FoodName,
                IsMerge = data.IsMerge,
                IsChecked = !inPantry,
                IsInPantry = inPantry,
            });
        }

        OnPropertyChanged(nameof(HasItems));
    }

    [RelayCommand]
    private void AddToPantry(CartPreviewItem item)
    {
        _pantryService.Add(new PantryItem
        {
            Name = item.FoodName,
            Category = PantryCategory.CheckIfHave,
        });

        item.AddedToPantry = true;
        item.IsChecked = false;
    }

    [RelayCommand]
    private void Confirm()
    {
        List<string> checkedLines = PreviewItems
            .Where(item => item.IsChecked && !item.IsMerge)
            .Select(item => item.FormattedLine)
            .ToList();

        List<string> checkedMergeLines = PreviewItems
            .Where(item => item.IsChecked && item.IsMerge)
            .Select(item => item.FormattedLine)
            .ToList();

        if (checkedLines.Count > 0 || checkedMergeLines.Count > 0)
        {
            List<string> allChecked = [..checkedLines, ..checkedMergeLines];
            _cartService.AddToCart(allChecked);
        }

        WeakReferenceMessenger.Default.Send(new GroceryListChangedMessage());
        RequestClose?.Invoke();
    }

    [RelayCommand]
    private void Cancel()
    {
        RequestClose?.Invoke();
    }
}
