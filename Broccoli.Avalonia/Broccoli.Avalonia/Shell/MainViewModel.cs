using Avalonia.Media;
using Broccoli.Avalonia.Shared;
using Broccoli.Avalonia.Slices.Groceries;
using Broccoli.Avalonia.Slices.Pantry;
using Broccoli.Avalonia.Slices.Planning;
using Broccoli.Avalonia.Slices.Recipes;
using Broccoli.Avalonia.Slices.Settings;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Broccoli.Avalonia.Shell;

public partial class MainViewModel : ViewModelBase
{
    private readonly Lazy<SettingsViewModel> _settingsViewModel;
    private readonly RecipesListViewModel _recipesViewModel;

    /// <summary>Right-aligned storage-usage footer shown at the bottom of the shell.</summary>
    public StorageUsageFooterViewModel StorageUsageFooter { get; }

    [ObservableProperty]
    private string _greeting = "Welcome to Avalonia!";

    [ObservableProperty]
    private MenuItem? _selectedMenuItem;

    [ObservableProperty]
    private ViewModelBase? _currentPage;

    /// <summary>
    /// Whether the navigation drawer is open. Bound two-way to <c>DrawerPage.IsOpen</c> so that
    /// navigating (below) also dismisses the drawer after a selection on small screens.
    /// </summary>
    [ObservableProperty]
    private bool _isMenuOpen;

    /// <summary>Design-time/parameterless constructor; not used by the app at runtime (DI always
    /// resolves the constructor below).</summary>
    public MainViewModel()
        : this(
        new RecipesListViewModel(),
        new Lazy<PlanningPageViewModel>(() => new PlanningPageViewModel()),
        new Lazy<GroceriesViewModel>(() => new GroceriesViewModel()),
        new Lazy<PantryViewModel>(() => new PantryViewModel()),
        new Lazy<SettingsPageViewModel>(() => new SettingsPageViewModel()),
        new Lazy<SettingsViewModel>(() => new SettingsViewModel()),
        new StorageUsageFooterViewModel())
    {
    }

    public MainViewModel(
        RecipesListViewModel recipesViewModel,
        Lazy<PlanningPageViewModel> planningViewModel,
        Lazy<GroceriesViewModel> groceriesViewModel,
        Lazy<PantryViewModel> pantryViewModel,
        Lazy<SettingsPageViewModel> settingsViewModel,
        Lazy<SettingsViewModel> lazySettingsViewModel,
        StorageUsageFooterViewModel storageUsageFooterViewModel)
    {
        _settingsViewModel = lazySettingsViewModel;
        _recipesViewModel = recipesViewModel;
        StorageUsageFooter = storageUsageFooterViewModel;

        // Each MenuItem wraps an already-constructed (singleton) page view model, so switching
        // between nav items and back preserves whatever state that page was in - e.g. the
        // Recipes section stays on its detail/edit sub-page instead of resetting to the list.
        MenuItems =
        [
            new MenuItem(
                "Recipes",
                Geometry.Parse("M21,5C19.89,4.65 18.67,4.5 17.5,4.5C15.55,4.5 13.45,4.9 12,6C10.55,4.9 8.45,4.5 6.5,4.5C4.55,4.5 2.45,4.9 1,6V20.65C1,20.9 1.25,21.15 1.5,21.15C1.6,21.15 1.65,21.1 1.75,21.1C3.1,20.45 5.05,20 6.5,20C8.45,20 10.55,20.4 12,21.5C13.35,20.65 15.8,20 17.5,20C19.15,20 20.85,20.3 22.25,21.05C22.35,21.1 22.4,21.1 22.5,21.1C22.75,21.1 23,20.85 23,20.6V6C22.4,5.55 21.75,5.25 21,5M21,18.5C19.9,18.15 18.7,18 17.5,18C15.8,18 13.35,18.65 12,19.5V8C13.35,7.15 15.8,6.5 17.5,6.5C18.7,6.5 19.9,6.65 21,7V18.5Z"),
                new Lazy<ViewModelBase>(recipesViewModel)),
            new MenuItem(
                "Planning",
                Geometry.Parse("M19,3H18V1H16V3H8V1H6V3H5A2,2 0 0,0 3,5V19A2,2 0 0,0 5,21H19A2,2 0 0,0 21,19V5A2,2 0 0,0 19,3M19,19H5V8H19V19M7,10H12V15H7V10Z"),
                new Lazy<ViewModelBase>(() => planningViewModel.Value)),
            new MenuItem(
                "Groceries",
                Geometry.Parse("M17,18C15.89,18 15,18.89 15,20A2,2 0 0,0 17,22A2,2 0 0,0 19,20C19,18.89 18.1,18 17,18M1,2V4H3L6.6,11.59L5.24,14.04C5.09,14.32 5,14.65 5,15A2,2 0 0,0 7,17H19V15H7.42A0.25,0.25 0 0,1 7.17,14.75C7.17,14.7 7.18,14.66 7.2,14.63L8.1,13H15.55C16.3,13 16.96,12.59 17.3,11.97L20.88,5.5C20.95,5.34 21,5.17 21,5A1,1 0 0,0 20,4H5.21L4.27,2M7,18C5.89,18 5,18.89 5,20A2,2 0 0,0 7,22A2,2 0 0,0 9,20C9,18.89 8.1,18 7,18Z"),
                new Lazy<ViewModelBase>(() => groceriesViewModel.Value)),
            new MenuItem(
                "Pantry",
                Geometry.Parse("M19,20H5V9H19M16,2V4H8V2H6V4H5A2,2 0 0,0 3,6V20A2,2 0 0,0 5,22H19A2,2 0 0,0 21,20V6A2,2 0 0,0 19,4H18V2H16M9,13H15V18H9V13Z"),
                new Lazy<ViewModelBase>(() => pantryViewModel.Value)),
            new MenuItem(
                "Settings",
                Geometry.Parse("M12,15.5A3.5,3.5 0 0,1 8.5,12A3.5,3.5 0 0,1 12,8.5A3.5,3.5 0 0,1 15.5,12A3.5,3.5 0 0,1 12,15.5M19.43,12.97C19.47,12.65 19.5,12.33 19.5,12C19.5,11.67 19.47,11.34 19.43,11L21.54,9.37C21.73,9.22 21.78,8.95 21.66,8.73L19.66,5.27C19.54,5.05 19.27,4.96 19.05,5.05L16.56,6.05C16.04,5.66 15.5,5.32 14.87,5.07L14.5,2.42C14.46,2.18 14.25,2 14,2H10C9.75,2 9.54,2.18 9.5,2.42L9.13,5.07C8.5,5.32 7.96,5.66 7.44,6.05L4.95,5.05C4.73,4.96 4.46,5.05 4.34,5.27L2.34,8.73C2.21,8.95 2.27,9.22 2.46,9.37L4.57,11C4.53,11.34 4.5,11.67 4.5,12C4.5,12.33 4.53,12.65 4.57,12.97L2.46,14.63C2.27,14.78 2.21,15.05 2.34,15.27L4.34,18.73C4.46,18.95 4.73,19.03 4.95,18.95L7.44,17.95C7.96,18.34 8.5,18.68 9.13,18.93L9.5,21.58C9.54,21.82 9.75,22 10,22H14C14.25,22 14.46,21.82 14.5,21.58L14.87,18.93C15.5,18.68 16.04,18.34 16.56,17.95L19.05,18.95C19.27,19.03 19.54,18.95 19.66,18.73L21.66,15.27C21.78,15.05 21.73,14.78 21.54,14.63L19.43,12.97Z"),
                new Lazy<ViewModelBase>(() => settingsViewModel.Value)),
        ];

        SelectedMenuItem = MenuItems[0];
        CurrentPage = SelectedMenuItem.Page.Value;
    }

    /// <summary>
    /// Fixed at construction time and never mutated afterwards, so a plain read-only list is
    /// enough here - no need for the change-notification overhead of <c>ObservableCollection</c>.
    /// </summary>
    public IReadOnlyList<MenuItem> MenuItems { get; }

    /// <summary>
    /// Resolved on first access only (see <see cref="ServiceCollectionExtensions.AddAppServices"/>),
    /// so opening the app never eagerly constructs the settings view model or touches the file
    /// system for the stored Drive account unless the user actually opens the settings flyout.
    /// </summary>
    public SettingsViewModel SettingsViewModel => _settingsViewModel.Value;

    /// <summary>
    /// Loads the initially-visible page's data. Called after the window is shown (and the database
    /// has been migrated) so the UI can appear immediately and fill in as data becomes ready.
    /// </summary>
    public async Task LoadAsync()
    {
        await _recipesViewModel.LoadAsync();
        _ = StorageUsageFooter.RefreshAsync();
    }

    partial void OnSelectedMenuItemChanged(MenuItem? value)
    {
        CurrentPage = value?.Page.Value;
        IsMenuOpen = false;
    }

    public record MenuItem(string Title, Geometry Icon, Lazy<ViewModelBase> Page);
}
