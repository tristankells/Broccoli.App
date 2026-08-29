using Broccoli.Avalonia.Models;
using Broccoli.Avalonia.Slices.Planning;
using Broccoli.Avalonia.Slices.Recipes;
using Moq;

namespace Broccoli.Avalonia.Tests.Slices.Recipes;

/// <summary>
/// Specifies how the recipes cards-vs-list view mode is loaded from settings and persisted back
/// when toggled, so the choice survives reloads and app restarts.
/// </summary>
[TestClass]
public class RecipeListPageViewModelViewModeTests
{
    [TestMethod]
    public void IsListView_LoadsFromSettings_WhenListIsChosen()
    {
        Mock<IMacroTargetService> macroService = CreateMacroService(showRecipesAsList: true);
        RecipeListPageViewModel viewModel = CreateViewModel(macroService);

        viewModel.Reload();

        Assert.IsTrue(viewModel.IsListView);
    }

    [TestMethod]
    public void IsListView_DefaultsToCards_WhenListIsNotChosen()
    {
        Mock<IMacroTargetService> macroService = CreateMacroService(showRecipesAsList: false);
        RecipeListPageViewModel viewModel = CreateViewModel(macroService);

        viewModel.Reload();

        Assert.IsFalse(viewModel.IsListView);
    }

    [TestMethod]
    public void TogglingIsListView_PersistsChoice_AndSurvivesReload()
    {
        Mock<IMacroTargetService> macroService = CreateMacroService(showRecipesAsList: false);
        RecipeListPageViewModel first = CreateViewModel(macroService);
        first.Reload();
        Assert.IsFalse(first.IsListView);

        first.IsListView = true;

        macroService.Verify(
            service => service.SaveSettings(It.Is<MacroTargetSettings>(s => s.ShowRecipesAsList)),
            Times.Once,
            "Toggling the view mode should persist the choice to settings.");

        RecipeListPageViewModel reloaded = CreateViewModel(macroService);
        reloaded.Reload();

        Assert.IsTrue(reloaded.IsListView, "A reloaded page should restore the persisted view mode.");
    }

    [TestMethod]
    public void TogglingIsListView_WithoutMacroService_DoesNotThrow()
    {
        var recipeService = new Mock<IRecipeService>();
        recipeService.Setup(s => s.GetAll()).Returns([]);
        RecipeListPageViewModel viewModel = new(recipeService.Object);

        viewModel.IsListView = true;

        Assert.IsTrue(viewModel.IsListView);
    }

    [TestMethod]
    public void SearchFiltering_AppliesRegardlessOfViewMode()
    {
        Mock<IMacroTargetService> macroService = CreateMacroService(showRecipesAsList: true);
        RecipeListPageViewModel viewModel = CreateViewModel(
            macroService,
            new Recipe { Name = "Banana Bread" },
            new Recipe { Name = "Chicken Curry" });
        viewModel.Reload();
        Assert.IsTrue(viewModel.IsListView);

        viewModel.SearchText = "chicken";

        Assert.HasCount(1, viewModel.FilteredRecipes);
        Assert.AreEqual("Chicken Curry", viewModel.FilteredRecipes.First().Name);
    }

    [TestMethod]
    public void SortedListItems_DefaultsToNameAscending()
    {
        Mock<IMacroTargetService> macroService = CreateMacroService(showRecipesAsList: false);
        RecipeListPageViewModel viewModel = CreateViewModel(
            macroService,
            new Recipe { Name = "Banana Bread" },
            new Recipe { Name = "Chicken Curry" },
            new Recipe { Name = "Apple Pie" });
        viewModel.Reload();

        Assert.AreEqual("Apple Pie", viewModel.SortedListItems[0].Card.Name);
        Assert.AreEqual("Banana Bread", viewModel.SortedListItems[1].Card.Name);
        Assert.AreEqual("Chicken Curry", viewModel.SortedListItems[2].Card.Name);
    }

    [TestMethod]
    public void Sort_ByCookingTime_SortsByTotalMinutes()
    {
        Mock<IMacroTargetService> macroService = CreateMacroService(showRecipesAsList: false);
        RecipeListPageViewModel viewModel = CreateViewModel(
            macroService,
            new Recipe { Name = "Slow Stew", PrepTimeMinutes = 15, CookTimeMinutes = 120 },
            new Recipe { Name = "Quick Salad", PrepTimeMinutes = 10, CookTimeMinutes = 0 },
            new Recipe { Name = "Medium Bake", PrepTimeMinutes = 20, CookTimeMinutes = 30 });
        viewModel.Reload();

        viewModel.SortCommand.Execute(RecipeListColumn.CookingTime);

        Assert.AreEqual("Quick Salad", viewModel.SortedListItems[0].Card.Name);
        Assert.AreEqual("Medium Bake", viewModel.SortedListItems[1].Card.Name);
        Assert.AreEqual("Slow Stew", viewModel.SortedListItems[2].Card.Name);
    }

    [TestMethod]
    public void Sort_TogglingSameColumn_FlipsDirection()
    {
        Mock<IMacroTargetService> macroService = CreateMacroService(showRecipesAsList: false);
        RecipeListPageViewModel viewModel = CreateViewModel(
            macroService,
            new Recipe { Name = "Apple Pie" },
            new Recipe { Name = "Banana Bread" });
        viewModel.Reload();

        Assert.AreEqual("Apple Pie", viewModel.SortedListItems[0].Card.Name, "Name ascending is the default.");
        Assert.IsTrue(viewModel.SortAscending);

        viewModel.SortCommand.Execute(RecipeListColumn.Name);

        Assert.AreEqual("Banana Bread", viewModel.SortedListItems[0].Card.Name);
        Assert.IsFalse(viewModel.SortAscending);

        viewModel.SortCommand.Execute(RecipeListColumn.Name);

        Assert.AreEqual("Apple Pie", viewModel.SortedListItems[0].Card.Name);
        Assert.IsTrue(viewModel.SortAscending);
    }

    [TestMethod]
    public void Sort_ByDateAdded_SortsNewestFirstWhenDescending()
    {
        Mock<IMacroTargetService> macroService = CreateMacroService(showRecipesAsList: false);
        RecipeListPageViewModel viewModel = CreateViewModel(
            macroService,
            new Recipe { Name = "Old", CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Recipe { Name = "New", CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) });
        viewModel.Reload();

        viewModel.SortCommand.Execute(RecipeListColumn.DateAdded);
        viewModel.SortCommand.Execute(RecipeListColumn.DateAdded);

        Assert.AreEqual("New", viewModel.SortedListItems[0].Card.Name);
    }

    [TestMethod]
    public void SortIndicator_SuffixReflectsActiveColumn()
    {
        Mock<IMacroTargetService> macroService = CreateMacroService(showRecipesAsList: false);
        RecipeListPageViewModel viewModel = CreateViewModel(macroService, new Recipe { Name = "Banana Bread" });
        viewModel.Reload();

        RecipeListColumnDefinition nameDefinition = viewModel.ListColumns.First(d => d.Column == RecipeListColumn.Name);
        RecipeListColumnDefinition caloriesDefinition = viewModel.ListColumns.First(d => d.Column == RecipeListColumn.Calories);
        Assert.AreEqual(" ▲", nameDefinition.SortSuffix, "Name is the default ascending sort column.");

        viewModel.SortCommand.Execute(RecipeListColumn.Calories);

        Assert.AreEqual(string.Empty, nameDefinition.SortSuffix);
        Assert.AreEqual(" ▲", caloriesDefinition.SortSuffix);
    }

    [TestMethod]
    public void ListColumns_LoadFromSettings_RespectStoredOrder()
    {
        Mock<IMacroTargetService> macroService = CreateMacroService(showRecipesAsList: false);
        macroService.Setup(s => s.GetSettings()).Returns(new MacroTargetSettings
        {
            ShowRecipesAsList = false,
            RecipeListColumns = "Name,Fat,Calories",
        });
        RecipeListPageViewModel viewModel = CreateViewModel(macroService, new Recipe { Name = "Banana Bread" });

        viewModel.Reload();

        Assert.AreEqual(3, viewModel.ListColumns.Count);
        Assert.AreEqual(RecipeListColumn.Name, viewModel.ListColumns[0].Column);
        Assert.AreEqual(RecipeListColumn.Fat, viewModel.ListColumns[1].Column);
        Assert.AreEqual(RecipeListColumn.Calories, viewModel.ListColumns[2].Column);
    }

    private static Mock<IMacroTargetService> CreateMacroService(bool showRecipesAsList)
    {
        var settings = new MacroTargetSettings { ShowRecipesAsList = showRecipesAsList };
        var service = new Mock<IMacroTargetService>();
        service.Setup(s => s.GetSettings()).Returns(settings);
        service.Setup(s => s.SaveSettings(It.IsAny<MacroTargetSettings>()))
            .Callback<MacroTargetSettings>(saved => settings.ShowRecipesAsList = saved.ShowRecipesAsList)
            .Returns((MacroTargetSettings saved) => saved);
        return service;
    }

    private static RecipeListPageViewModel CreateViewModel(Mock<IMacroTargetService> macroService, params Recipe[] recipes)
    {
        var recipeService = new Mock<IRecipeService>();
        recipeService.Setup(s => s.GetAll()).Returns(recipes);
        return new RecipeListPageViewModel(recipeService.Object, null, null, macroService.Object);
    }
}
