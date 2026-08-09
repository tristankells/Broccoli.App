using Broccoli.Avalonia.Models;
using Broccoli.Avalonia.Shared;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace Broccoli.Avalonia.Slices.Groceries;

public partial class GroceriesViewModel : ViewModelBase
{
    private readonly IGroceryListService _groceryListService;

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

    public GroceriesViewModel() : this(new GroceryListService())
    {
    }

    public GroceriesViewModel(IGroceryListService groceryListService)
    {
        _groceryListService = groceryListService;
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
            var item = new GroceryListItem
            {
                Name = name,
                IsChecked = false,
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
    private Task CopyToClipboard()
    {
        return Task.CompletedTask;
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
