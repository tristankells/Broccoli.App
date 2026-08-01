using Broccoli.Avalonia.Shared;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Broccoli.Avalonia.Slices.Settings;

public partial class SettingsPageViewModel : ViewModelBase
{
    public SettingsViewModel Sync { get; }
    public FoodDatabaseViewModel FoodDatabase { get; }

    [ObservableProperty] private int _currentTabIndex;

    public SettingsPageViewModel() : this(new SettingsViewModel(), new FoodDatabaseViewModel())
    {
    }

    public SettingsPageViewModel(SettingsViewModel sync, FoodDatabaseViewModel foodDatabase)
    {
        Sync = sync;
        FoodDatabase = foodDatabase;
    }
}
