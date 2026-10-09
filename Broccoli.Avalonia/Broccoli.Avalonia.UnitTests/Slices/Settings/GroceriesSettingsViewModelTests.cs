using Broccoli.Avalonia.Models;
using Broccoli.Avalonia.Slices.Planning;
using Broccoli.Avalonia.Slices.Settings;
using Moq;

namespace Broccoli.Avalonia.Tests.Slices.Settings;

[TestClass]
public class GroceriesSettingsViewModelTests
{
    private static Mock<IMacroTargetService> CreateMacroService(GroceryUnitNormalization mode = GroceryUnitNormalization.Off)
    {
        var settings = new MacroTargetSettings { GroceryUnitNormalization = mode };
        var service = new Mock<IMacroTargetService>();
        service.Setup(s => s.GetSettings()).Returns(settings);
        service.Setup(s => s.SaveSettings(It.IsAny<MacroTargetSettings>()))
            .Callback<MacroTargetSettings>(saved => settings.GroceryUnitNormalization = saved.GroceryUnitNormalization)
            .Returns((MacroTargetSettings saved) => saved);
        return service;
    }

    [TestMethod]
    public void Load_SelectsStoredMode()
    {
        GroceriesSettingsViewModel viewModel = new(CreateMacroService(GroceryUnitNormalization.Grams).Object);

        Assert.AreEqual(GroceryUnitNormalization.Grams, viewModel.UnitNormalization);
        Assert.AreEqual("Grams", viewModel.SelectedUnitNormalizationOption?.Label);
    }

    [TestMethod]
    public void Load_DefaultOff_SelectsOff()
    {
        GroceriesSettingsViewModel viewModel = new(CreateMacroService().Object);

        Assert.AreEqual(GroceryUnitNormalization.Off, viewModel.UnitNormalization);
        Assert.AreEqual("Off", viewModel.SelectedUnitNormalizationOption?.Label);
    }

    [TestMethod]
    public void SelectingOption_UpdatesMode()
    {
        GroceriesSettingsViewModel viewModel = new(CreateMacroService().Object);

        viewModel.SelectedUnitNormalizationOption = viewModel.UnitNormalizationOptions
            .First(option => option.Value == GroceryUnitNormalization.Pounds);

        Assert.AreEqual(GroceryUnitNormalization.Pounds, viewModel.UnitNormalization);
    }

    [TestMethod]
    public void Save_PersistsSelectedMode()
    {
        Mock<IMacroTargetService> service = CreateMacroService();
        GroceriesSettingsViewModel viewModel = new(service.Object);

        viewModel.SelectedUnitNormalizationOption = viewModel.UnitNormalizationOptions
            .First(option => option.Value == GroceryUnitNormalization.Grams);
        viewModel.SaveCommand.Execute(null);

        service.Verify(
            service => service.SaveSettings(It.Is<MacroTargetSettings>(
                settings => settings.GroceryUnitNormalization == GroceryUnitNormalization.Grams)),
            Times.Once);
    }
}
