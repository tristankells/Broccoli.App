using Broccoli.Avalonia.Models;
using Broccoli.Avalonia.Seasonality;
using Broccoli.Avalonia.Shared;
using Broccoli.Avalonia.Shell;
using Broccoli.Avalonia.Slices.Groceries;
using Broccoli.Avalonia.Slices.Pantry;
using Broccoli.Avalonia.Slices.Planning;
using Broccoli.Avalonia.Slices.Recipes;
using Broccoli.Avalonia.Slices.Seasonality;
using Broccoli.Avalonia.Slices.Settings;
using CommunityToolkit.Mvvm.Messaging;
using Moq;

namespace Broccoli.Avalonia.Tests.Shell;

[TestClass]
public class MainViewModelTests
{
    [TestMethod]
    public void SeasonalityTab_VisibleByDefault()
    {
        MainViewModel vm = CreateViewModel();

        Assert.IsTrue(vm.VisibleMenuItems.Any(m => m.Title == "Seasonality"));
    }

    [TestMethod]
    public void NavVisibilityMessage_HidesSeasonalityTab()
    {
        MainViewModel vm = CreateViewModel();

        WeakReferenceMessenger.Default.Send(new NavVisibilityChangedMessage(false));

        Assert.IsFalse(vm.VisibleMenuItems.Any(m => m.Title == "Seasonality"));
    }

    [TestMethod]
    public void NavVisibilityMessage_ShowsSeasonalityTabAgain()
    {
        MainViewModel vm = CreateViewModel();

        WeakReferenceMessenger.Default.Send(new NavVisibilityChangedMessage(false));
        WeakReferenceMessenger.Default.Send(new NavVisibilityChangedMessage(true));

        Assert.IsTrue(vm.VisibleMenuItems.Any(m => m.Title == "Seasonality"));
    }

    [TestMethod]
    public void HidingCurrentTab_ReselectsFirstVisibleItem()
    {
        MainViewModel vm = CreateViewModel();
        MainViewModel.MenuItem seasonality = vm.VisibleMenuItems.First(m => m.Title == "Seasonality");
        vm.SelectedMenuItem = seasonality;
        Assert.AreEqual("SeasonalityViewModel", vm.CurrentPage?.GetType().Name);

        WeakReferenceMessenger.Default.Send(new NavVisibilityChangedMessage(false));

        Assert.IsNotNull(vm.SelectedMenuItem);
        Assert.AreNotEqual("Seasonality", vm.SelectedMenuItem!.Title);
    }

    private static MainViewModel CreateViewModel()
    {
        Mock<ISeasonalityDataStore> storeMock = new();
        storeMock.Setup(s => s.GetAll()).Returns(new List<ProduceItem>());

        return new MainViewModel(
            new RecipesListViewModel(),
            new Lazy<PlanningPageViewModel>(() => new PlanningPageViewModel()),
            new Lazy<GroceriesViewModel>(() => new GroceriesViewModel()),
            new Lazy<PantryViewModel>(() => new PantryViewModel()),
            new Lazy<SeasonalityViewModel>(() => new SeasonalityViewModel(storeMock.Object)),
            new Lazy<SettingsPageViewModel>(() => new SettingsPageViewModel()),
            new Lazy<SettingsViewModel>(() => new SettingsViewModel()),
            new StorageUsageFooterViewModel(),
            Mock.Of<IMacroTargetService>());
    }
}
