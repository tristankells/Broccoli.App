using Broccoli.Avalonia.Shared;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Broccoli.Avalonia.Slices.Settings;

public partial class SettingsPageViewModel : ViewModelBase
{
    [ObservableProperty]
    private int _currentTabIndex;

    public SettingsPageViewModel()
        : this(new SettingsViewModel(), new FoodDatabaseViewModel(), new RecipeSettingsViewModel(), new SeasonalitySettingsViewModel())
    {
    }

    public SettingsPageViewModel(SettingsViewModel sync, FoodDatabaseViewModel foodDatabase, RecipeSettingsViewModel recipeSettings, SeasonalitySettingsViewModel seasonalitySettings)
    {
        Sync = sync;
        FoodDatabase = foodDatabase;
        RecipeSettings = recipeSettings;
        SeasonalitySettings = seasonalitySettings;
    }

    public SettingsViewModel Sync { get; }

    public FoodDatabaseViewModel FoodDatabase { get; }

    public RecipeSettingsViewModel RecipeSettings { get; }

    public SeasonalitySettingsViewModel SeasonalitySettings { get; }
}
