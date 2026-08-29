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
