using Avalonia.Media;
using Broccoli.Avalonia.Shared;
using Broccoli.Avalonia.Slices.Groceries;
using Broccoli.Avalonia.Slices.Planning;
using Broccoli.Avalonia.Slices.Recipes;
using Broccoli.Avalonia.Slices.Settings;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Broccoli.Avalonia.Shell;

public partial class MainViewModel : ViewModelBase
{
    private readonly Lazy<SettingsViewModel> _settingsViewModel;

    [ObservableProperty]
    private string _greeting = "Welcome to Avalonia!";

    /// <summary>
    /// Fixed at construction time and never mutated afterwards, so a plain read-only list is
    /// enough here - no need for the change-notification overhead of <c>ObservableCollection</c>.
    /// </summary>
    public IReadOnlyList<MenuItem> MenuItems { get; }

    [ObservableProperty]
    private MenuItem? _selectedMenuItem;

    [ObservableProperty]
    private ViewModelBase? _currentPage;

    /// <summary>
    /// Resolved on first access only (see <see cref="ServiceCollectionExtensions.AddAppServices"/>),
    /// so opening the app never eagerly constructs the settings view model or touches the file
    /// system for the stored Drive account unless the user actually opens the settings flyout.
    /// </summary>
    public SettingsViewModel SettingsViewModel => _settingsViewModel.Value;

    /// <summary>Design-time/parameterless constructor; not used by the app at runtime (DI always
    /// resolves the constructor below).</summary>
    public MainViewModel() : this(
        new RecipesListViewModel(),
        new PlanningViewModel(),
        new GroceriesViewModel(),
        new Lazy<SettingsViewModel>(() => new SettingsViewModel()))
    {
    }

    public MainViewModel(
        RecipesListViewModel recipesViewModel,
        PlanningViewModel planningViewModel,
        GroceriesViewModel groceriesViewModel,
        Lazy<SettingsViewModel> settingsViewModel)
    {
        _settingsViewModel = settingsViewModel;

        // Each MenuItem wraps an already-constructed (singleton) page view model, so switching
        // between nav items and back preserves whatever state that page was in - e.g. the
        // Recipes section stays on its detail/edit sub-page instead of resetting to the list.
        MenuItems =
        [
            new MenuItem("Recipes",
                Geometry.Parse("M21,5C19.89,4.65 18.67,4.5 17.5,4.5C15.55,4.5 13.45,4.9 12,6C10.55,4.9 8.45,4.5 6.5,4.5C4.55,4.5 2.45,4.9 1,6V20.65C1,20.9 1.25,21.15 1.5,21.15C1.6,21.15 1.65,21.1 1.75,21.1C3.1,20.45 5.05,20 6.5,20C8.45,20 10.55,20.4 12,21.5C13.35,20.65 15.8,20 17.5,20C19.15,20 20.85,20.3 22.25,21.05C22.35,21.1 22.4,21.1 22.5,21.1C22.75,21.1 23,20.85 23,20.6V6C22.4,5.55 21.75,5.25 21,5M21,18.5C19.9,18.15 18.7,18 17.5,18C15.8,18 13.35,18.65 12,19.5V8C13.35,7.15 15.8,6.5 17.5,6.5C18.7,6.5 19.9,6.65 21,7V18.5Z"),
                recipesViewModel),
            new MenuItem("Planning",
                Geometry.Parse("M19,3H18V1H16V3H8V1H6V3H5A2,2 0 0,0 3,5V19A2,2 0 0,0 5,21H19A2,2 0 0,0 21,19V5A2,2 0 0,0 19,3M19,19H5V8H19V19M7,10H12V15H7V10Z"),
                planningViewModel),
            new MenuItem("Groceries",
                Geometry.Parse("M17,18C15.89,18 15,18.89 15,20A2,2 0 0,0 17,22A2,2 0 0,0 19,20C19,18.89 18.1,18 17,18M1,2V4H3L6.6,11.59L5.24,14.04C5.09,14.32 5,14.65 5,15A2,2 0 0,0 7,17H19V15H7.42A0.25,0.25 0 0,1 7.17,14.75C7.17,14.7 7.18,14.66 7.2,14.63L8.1,13H15.55C16.3,13 16.96,12.59 17.3,11.97L20.88,5.5C20.95,5.34 21,5.17 21,5A1,1 0 0,0 20,4H5.21L4.27,2M7,18C5.89,18 5,18.89 5,20A2,2 0 0,0 7,22A2,2 0 0,0 9,20C9,18.89 8.1,18 7,18Z"),
                groceriesViewModel),
        ];

        SelectedMenuItem = MenuItems[0];
        CurrentPage = SelectedMenuItem.Page;
    }

    partial void OnSelectedMenuItemChanged(MenuItem? value)
    {
        CurrentPage = value?.Page;
    }

    public record MenuItem(string Title, Geometry Icon, ViewModelBase Page);
}

