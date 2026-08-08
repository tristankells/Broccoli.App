using Broccoli.Avalonia.Models;
using Broccoli.Avalonia.Slices.Planning;
using Moq;

namespace Broccoli.Avalonia.Tests.Slices.Planning;

[TestClass]
public class MacroTargetsViewModelTests
{
    private readonly Mock<IMacroTargetService> _serviceMock = new();
    private readonly MacroCalculatorService _calculator = new();

    private static MacroTargetSettings DefaultSettings() => new()
    {
        Id = "default",
        UnitSystem = UnitSystem.Metric,
        BmrFormula = BmrFormula.MifflinStJeor,
        ProteinMethod = ProteinMethod.RatioPercent,
        ProteinPercent = 30,
        CarbPercent = 40,
        FatPercent = 30,
    };

    private MacroTargetsViewModel CreateViewModel(
        List<MacroTarget>? existingTargets = null)
    {
        MacroTargetSettings settings = DefaultSettings();
        List<MacroTarget> targets = existingTargets ?? new List<MacroTarget>();

        _serviceMock.Setup(s => s.GetSettings()).Returns(settings);
        _serviceMock.Setup(s => s.GetAll()).Returns(targets);

        return new MacroTargetsViewModel(_serviceMock.Object, _calculator);
    }

    [TestMethod]
    public void LoadData_WithExistingTargets_PopulatesCollection()
    {
        var targets = new List<MacroTarget>
        {
            new() { Id = "1", Name = "Alice", WeightKg = 65, HeightCm = 165, Age = 25 },
            new() { Id = "2", Name = "Bob", WeightKg = 80, HeightCm = 180, Age = 30 },
        };
        MacroTargetsViewModel vm = CreateViewModel(targets);

        Assert.AreEqual(2, vm.Targets.Count);
        Assert.AreEqual("Alice", vm.Targets[0].Name);
        Assert.AreEqual("Bob", vm.Targets[1].Name);
    }

    [TestMethod]
    public void LoadData_CalculatesEachTarget()
    {
        var targets = new List<MacroTarget>
        {
            new() { Id = "1", WeightKg = 80, HeightCm = 180, Age = 30 },
        };
        MacroTargetsViewModel vm = CreateViewModel(targets);

        Assert.IsTrue(vm.Targets[0].BmrText != "—");
        Assert.IsTrue(vm.Targets[0].CaloriesText != "—");
    }

    [TestMethod]
    public void AddPerson_AddsRowAndPersists()
    {
        MacroTarget? saved = null;
        _serviceMock.Setup(s => s.Add(It.IsAny<MacroTarget>())).Callback<MacroTarget>(t => saved = t)
            .Returns((MacroTarget t) => { t.Id = "new"; return t; });

        MacroTargetsViewModel vm = CreateViewModel();
        vm.AddPersonCommand.Execute(null);

        Assert.AreEqual(1, vm.Targets.Count);
        Assert.IsNotNull(saved);
    }

    [TestMethod]
    public void AddPerson_Error_SetsErrorMessage()
    {
        _serviceMock.Setup(s => s.Add(It.IsAny<MacroTarget>())).Throws(new Exception("DB error"));
        MacroTargetsViewModel vm = CreateViewModel();

        vm.AddPersonCommand.Execute(null);

        Assert.IsTrue(vm.ErrorMessage!.Contains("Failed to add"));
    }

    [TestMethod]
    public void DeletePerson_RemovesRowAndCallsDelete()
    {
        var targets = new List<MacroTarget>
        {
            new() { Id = "1", Name = "Alice", WeightKg = 65, HeightCm = 165, Age = 25 },
        };
        _serviceMock.Setup(s => s.Delete(It.IsAny<string>()));
        MacroTargetsViewModel vm = CreateViewModel(targets);

        MacroTargetRowViewModel row = vm.Targets[0];
        vm.DeletePersonCommand.Execute(row);

        Assert.AreEqual(0, vm.Targets.Count);
        _serviceMock.Verify(s => s.Delete("1"), Times.Once);
    }

    [TestMethod]
    public void DeletePerson_Error_RestoresRow()
    {
        var targets = new List<MacroTarget>
        {
            new() { Id = "1", Name = "Alice", WeightKg = 65, HeightCm = 165, Age = 25 },
        };
        _serviceMock.Setup(s => s.Delete(It.IsAny<string>())).Throws(new Exception("DB error"));
        MacroTargetsViewModel vm = CreateViewModel(targets);

        MacroTargetRowViewModel row = vm.Targets[0];
        vm.DeletePersonCommand.Execute(row);

        Assert.AreEqual(1, vm.Targets.Count);
        Assert.IsTrue(vm.ErrorMessage!.Contains("Failed to delete"));
    }

    [TestMethod]
    public void OpenSettings_CopiesCurrentSettingsToDraft()
    {
        MacroTargetsViewModel vm = CreateViewModel();
        vm.OpenSettingsCommand.Execute(null);

        Assert.IsTrue(vm.IsSettingsOpen);
        Assert.AreEqual(UnitSystem.Metric, vm.DraftUnitSystem);
        Assert.AreEqual(BmrFormula.MifflinStJeor, vm.DraftBmrFormula);
        Assert.AreEqual(30, vm.DraftProteinPercent);
    }

    [TestMethod]
    public void CancelSettings_ClosesDialog()
    {
        MacroTargetsViewModel vm = CreateViewModel();
        vm.OpenSettingsCommand.Execute(null);
        Assert.IsTrue(vm.IsSettingsOpen);

        vm.CancelSettingsCommand.Execute(null);
        Assert.IsFalse(vm.IsSettingsOpen);
    }

    [TestMethod]
    public void SaveSettings_ValidPercents_SavesAndUpdatesRows()
    {
        var targets = new List<MacroTarget>
        {
            new() { Id = "1", Name = "Alice", WeightKg = 65, HeightCm = 165, Age = 25 },
        };
        _serviceMock.Setup(s => s.SaveSettings(It.IsAny<MacroTargetSettings>()))
            .Returns((MacroTargetSettings s) => s);
        _serviceMock.Setup(s => s.Update(It.IsAny<MacroTarget>())).Returns((MacroTarget t) => t);
        MacroTargetsViewModel vm = CreateViewModel(targets);

        vm.OpenSettingsCommand.Execute(null);
        vm.DraftProteinPercent = 35;
        vm.DraftCarbPercent = 40;
        vm.DraftFatPercent = 25;
        vm.SaveSettingsCommand.Execute(null);

        Assert.IsFalse(vm.IsSettingsOpen);
        _serviceMock.Verify(s => s.SaveSettings(It.IsAny<MacroTargetSettings>()), Times.Once);
        _serviceMock.Verify(s => s.Update(It.IsAny<MacroTarget>()), Times.AtLeastOnce);
    }

    [TestMethod]
    public void SaveSettings_InvalidPercents_DoesNotSave()
    {
        MacroTargetsViewModel vm = CreateViewModel();
        vm.OpenSettingsCommand.Execute(null);
        vm.DraftProteinPercent = 50;
        vm.DraftCarbPercent = 50;
        vm.DraftFatPercent = 50;

        Assert.IsFalse(vm.DraftCanSave);
        vm.SaveSettingsCommand.Execute(null);
        Assert.IsTrue(vm.IsSettingsOpen);
    }

    [TestMethod]
    public void DraftMacroSum_CalculatesCorrectly()
    {
        MacroTargetsViewModel vm = CreateViewModel();
        vm.OpenSettingsCommand.Execute(null);
        vm.DraftProteinPercent = 25;
        vm.DraftCarbPercent = 45;

        Assert.AreEqual(100, vm.DraftMacroSum);
        Assert.IsTrue(vm.DraftMacroSumValid);
    }

    [TestMethod]
    public void ChangeProteinMethod_UpdatesDraftValidation()
    {
        MacroTargetsViewModel vm = CreateViewModel();
        vm.OpenSettingsCommand.Execute(null);
        Assert.IsTrue(vm.DraftIsRatioPercent);
        Assert.IsFalse(vm.DraftIsGramsPerKg);

        vm.DraftProteinMethod = ProteinMethod.GramsPerKg;

        Assert.IsFalse(vm.DraftIsRatioPercent);
        Assert.IsTrue(vm.DraftIsGramsPerKg);
        Assert.IsTrue(vm.DraftCanSave);
    }
}
