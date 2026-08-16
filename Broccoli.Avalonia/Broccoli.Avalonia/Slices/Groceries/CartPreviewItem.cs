using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Broccoli.Avalonia.Slices.Groceries;

public partial class CartPreviewItem : ObservableObject
{
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

    public string DisplayName { get; init; } = string.Empty;

    public string FormattedLine { get; init; } = string.Empty;

    public string OriginalLine { get; init; } = string.Empty;

    public string FoodName { get; init; } = string.Empty;

    public bool IsMerge { get; init; }

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
