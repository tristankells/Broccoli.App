using Broccoli.Avalonia.Models;
using Broccoli.Avalonia.Slices.Planning;
using Broccoli.Avalonia.Slices.Settings;
using Moq;

namespace Broccoli.Avalonia.Tests.Slices.Settings;

[TestClass]
public class RecipeSettingsViewModelColumnTests
{
    private static Mock<IMacroTargetService> CreateMacroService(string? columns = null)
    {
        var settings = new MacroTargetSettings { RecipeListColumns = columns ?? "Name,Calories,Fat" };
        var service = new Mock<IMacroTargetService>();
        service.Setup(s => s.GetSettings()).Returns(settings);
        service.Setup(s => s.GetAll()).Returns([]);
        service.Setup(s => s.SaveSettings(It.IsAny<MacroTargetSettings>()))
            .Callback<MacroTargetSettings>(saved => settings.RecipeListColumns = saved.RecipeListColumns)
            .Returns((MacroTargetSettings saved) => saved);
        return service;
    }

    [TestMethod]
    public void Load_SelectedColumnsMatchStoredOrder()
    {
        RecipeSettingsViewModel viewModel = new(CreateMacroService("Name,Fat,Calories").Object);

        CollectionAssert.AreEqual(
            new[] { "Name", "Fat", "Calories" },
            viewModel.ListColumnOptions.Where(o => o.IsSelected).Select(o => o.Title).ToArray());
    }

    [TestMethod]
    public void MoveColumnDown_ReordersOptions()
    {
        RecipeSettingsViewModel viewModel = new(CreateMacroService("Name,Calories").Object);
        RecipeListColumnOption name = viewModel.ListColumnOptions.First(o => o.Title == "Name");

        viewModel.MoveColumnDownCommand.Execute(name);

        Assert.AreEqual("Calories", viewModel.ListColumnOptions[0].Title);
        Assert.AreEqual("Name", viewModel.ListColumnOptions[1].Title);
    }

    [TestMethod]
    public void MoveColumnUp_MovesOptionEarlier()
    {
        RecipeSettingsViewModel viewModel = new(CreateMacroService("Name,Calories,Fat").Object);
        RecipeListColumnOption fat = viewModel.ListColumnOptions.First(o => o.Title == "Fat");

        viewModel.MoveColumnUpCommand.Execute(fat);

        Assert.AreEqual("Fat", viewModel.ListColumnOptions[1].Title);
        Assert.AreEqual("Calories", viewModel.ListColumnOptions[2].Title);
    }

    [TestMethod]
    public void Save_PersistsSelectedColumnsInCurrentOrder()
    {
        Mock<IMacroTargetService> service = CreateMacroService("Name,Calories,Fat,Source");
        RecipeSettingsViewModel viewModel = new(service.Object);
        RecipeListColumnOption calories = viewModel.ListColumnOptions.First(o => o.Title == "Calories");
        calories.IsSelected = false;
        RecipeListColumnOption source = viewModel.ListColumnOptions.First(o => o.Title == "Source");

        viewModel.MoveColumnUpCommand.Execute(source);
        viewModel.SaveCommand.Execute(null);

        service.Verify(
            service => service.SaveSettings(It.Is<MacroTargetSettings>(settings => settings.RecipeListColumns == "Name,Source,Fat")),
            Times.Once);
    }
}
