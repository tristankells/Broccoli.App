using Avalonia.Controls;
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

    [TestMethod]
    public void NavLabels_HiddenWhenCollapsed_ShownWhenExpanded()
    {
        MainViewModel collapsedVm = CreateViewModel();
        HeadlessUiHost.Run(
            () => new MainView { DataContext = collapsedVm, Width = 800 },
            window =>
            {
                List<TextBlock> labels = FindNavLabels(window, collapsedVm);
                Assert.IsTrue(labels.Count > 0, "Nav labels should exist.");
                Assert.IsTrue(labels.All(label => !label.IsVisible),
                    "Labels must be hidden when the drawer is collapsed to the icon rail.");
            });

        MainViewModel expandedVm = CreateViewModel();
        expandedVm.IsMenuOpen = true;
        HeadlessUiHost.Run(
            () => new MainView { DataContext = expandedVm, Width = 800 },
            window =>
            {
                List<TextBlock> labels = FindNavLabels(window, expandedVm);
                Assert.IsTrue(labels.All(label => label.IsVisible),
                    "Labels must appear once the navigation drawer is expanded.");
            });
    }

    private static List<TextBlock> FindNavLabels(Window window, MainViewModel vm)
    {
        HashSet<string> titles = vm.VisibleMenuItems.Select(item => item.Title).ToHashSet();
        return HeadlessUiHost.FindVisualChildren<TextBlock>(window)
            .Where(label => label.DataContext is MainViewModel.MenuItem
                            && label.Text is not null
                            && titles.Contains(label.Text))
            .ToList();
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
