using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;

namespace Broccoli.Avalonia.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _greeting = "Welcome to Avalonia!";

    public ObservableCollection<MenuItem> MenuItems { get; } =
    [
        new MenuItem("Recipe List", "HomeIcon", () => new RecipesListViewModel())
    ];

    [ObservableProperty]
    private MenuItem? _selectedMenuItem;

    [ObservableProperty]
    private ObservableObject? _currentPage;

    partial void OnSelectedMenuItemChanged(MenuItem? value)
    {
        CurrentPage = value?.CreatePage();
    }

    public record MenuItem(string Title, string Icon, Func<ObservableObject> CreatePage);
}
