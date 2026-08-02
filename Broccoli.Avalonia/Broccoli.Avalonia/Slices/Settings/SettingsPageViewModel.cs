using Broccoli.Avalonia.Shared;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Broccoli.Avalonia.Slices.Settings;

public partial class SettingsPageViewModel : ViewModelBase
{
    public SettingsViewModel Sync { get; }
    public FoodDatabaseViewModel FoodDatabase { get; }
    public RecipeSettingsViewModel RecipeSettings { get; }

    [ObservableProperty] private int _currentTabIndex;

    public SettingsPageViewModel() : this(new SettingsViewModel(), new FoodDatabaseViewModel(), new RecipeSettingsViewModel())
    {
    }

    public SettingsPageViewModel(SettingsViewModel sync, FoodDatabaseViewModel foodDatabase, RecipeSettingsViewModel recipeSettings)
    {
        Sync = sync;
        FoodDatabase = foodDatabase;
        RecipeSettings = recipeSettings;
    }
}
