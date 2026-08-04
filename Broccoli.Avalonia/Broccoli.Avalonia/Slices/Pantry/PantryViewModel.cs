using Broccoli.Avalonia.Models;
using Broccoli.Avalonia.Shared;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace Broccoli.Avalonia.Slices.Pantry;

public partial class PantryViewModel : ViewModelBase
{
    private readonly IPantryService _pantryService;

    public ObservableCollection<PantryItem> Items { get; } = new();

    [ObservableProperty] private string _newItemName = string.Empty;
    [ObservableProperty] private PantryCategory _newItemCategory = PantryCategory.CheckIfHave;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private string? _editingItemId;
    [ObservableProperty] private string _editingName = string.Empty;

    public IEnumerable<PantryItem> AlwaysHaveItems => Items.Where(i => i.Category == PantryCategory.AlwaysHave);
    public IEnumerable<PantryItem> CheckIfHaveItems => Items.Where(i => i.Category == PantryCategory.CheckIfHave);
    public bool HasAlwaysHaveItems => AlwaysHaveItems.Any();
    public bool HasCheckIfHaveItems => CheckIfHaveItems.Any();

    public PantryViewModel() : this(new PantryService())
    {
    }

    public PantryViewModel(IPantryService pantryService)
    {
        _pantryService = pantryService;
        Load();
    }

    private void Load()
    {
        ErrorMessage = null;
        try
        {
            var items = _pantryService.GetAll();
            Items.Clear();
            foreach (var i in items)
            {
                Items.Add(i);
            }
        }
        catch (Exception ex) { ErrorMessage = $"Failed to load: {ex.Message}"; }
        RefreshCollections();
    }

    [RelayCommand]
    private void AddItem()
    {
        var name = NewItemName.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        ErrorMessage = null;
        try
        {
            var item = new PantryItem { Name = name, Category = NewItemCategory };
            var created = _pantryService.Add(item);
            Items.Add(created);
            NewItemName = string.Empty;
            RefreshCollections();
        }
        catch (Exception ex) { ErrorMessage = $"Failed to add: {ex.Message}"; }
    }

    [RelayCommand]
    private void StartEdit(PantryItem item)
    {
        EditingItemId = item.Id;
        EditingName = item.Name;
    }

    [RelayCommand]
    private void SaveEdit(PantryItem item)
    {
        if (string.IsNullOrWhiteSpace(EditingName))
        {
            return;
        }

        item.Name = EditingName.Trim();
        EditingItemId = null;
        try { _pantryService.Update(item); }
        catch (Exception ex) { ErrorMessage = $"Failed to save: {ex.Message}"; }
    }

    [RelayCommand]
    private void CancelEdit()
    {
        EditingItemId = null;
    }

    [RelayCommand]
    private void ChangeCategory(PantryItem item)
    {
        try
        {
            _pantryService.Update(item);
            RefreshCollections();
        }
        catch (Exception ex) { ErrorMessage = $"Failed to update: {ex.Message}"; }
    }

    [RelayCommand]
    private void DeleteItem(PantryItem item)
    {
        Items.Remove(item);
        try
        {
            _pantryService.Delete(item.Id);
            RefreshCollections();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to delete: {ex.Message}";
            Items.Add(item);
            RefreshCollections();
        }
    }

    private void RefreshCollections()
    {
        OnPropertyChanged(nameof(AlwaysHaveItems));
        OnPropertyChanged(nameof(CheckIfHaveItems));
        OnPropertyChanged(nameof(HasAlwaysHaveItems));
        OnPropertyChanged(nameof(HasCheckIfHaveItems));
    }
}
