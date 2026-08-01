using Broccoli.Avalonia.Models;
using Broccoli.Avalonia.Slices.Planning;
using Moq;

namespace Broccoli.Avalonia.Tests.Slices.Planning;

[TestClass]
public class DayPlanViewModelTests
{
    private readonly Mock<IDailyFoodPlanService> _mock = new();

    [TestMethod]
    public void NewPlan_CreatesWithOneTab()
    {
        DailyFoodPlan? saved = null;
        _mock.Setup(s => s.GetAll()).Returns(new List<DailyFoodPlan>());
        _mock.Setup(s => s.Add(It.IsAny<DailyFoodPlan>())).Callback<DailyFoodPlan>(p => saved = p)
            .Returns((DailyFoodPlan p) => { p.Id = "new"; return p; });

        var vm = new DayPlanViewModel(_mock.Object);
        vm.NewPlanCommand.Execute(null);

        Assert.IsNotNull(saved);
        Assert.AreEqual("New Plan", saved!.Name);
        Assert.AreEqual(1, saved.Tabs.Count);
    }

    [TestMethod]
    public void OpenPlan_SetsSelectedPlan()
    {
        var plan = new DailyFoodPlan { Id = "1", Name = "Test Plan" };
        _mock.Setup(s => s.GetAll()).Returns(new List<DailyFoodPlan> { plan });

        var vm = new DayPlanViewModel(_mock.Object);
        vm.OpenPlanCommand.Execute(plan);

        Assert.IsNotNull(vm.SelectedPlan);
        Assert.AreEqual("Test Plan", vm.SelectedPlan!.Name);
    }

    [TestMethod]
    public void BackToList_ClearsSelection()
    {
        var vm = new DayPlanViewModel(_mock.Object);
        vm.SelectedPlan = new DailyFoodPlan { Id = "1" };
        vm.BackToListCommand.Execute(null);

        Assert.IsNull(vm.SelectedPlan);
    }

    [TestMethod]
    public void DeletePlan_RemovesFromCollection()
    {
        var plan = new DailyFoodPlan { Id = "1", Name = "To Delete" };
        _mock.Setup(s => s.GetAll()).Returns(new List<DailyFoodPlan> { plan });

        var vm = new DayPlanViewModel(_mock.Object);
        vm.DeletePlanCommand.Execute(vm.Plans[0]);

        Assert.AreEqual(0, vm.Plans.Count);
        _mock.Verify(s => s.Delete("1"), Times.Once);
    }

    [TestMethod]
    public void AddTab_AppendsTab()
    {
        var plan = new DailyFoodPlan { Id = "1", Tabs = new List<DailyFoodPlanTab>() };
        _mock.Setup(s => s.GetAll()).Returns(new List<DailyFoodPlan> { plan });

        var vm = new DayPlanViewModel(_mock.Object);
        vm.SelectedPlan = vm.Plans[0];
        vm.AddTabCommand.Execute(null);

        Assert.AreEqual(1, vm.SelectedPlan!.Tabs.Count);
        Assert.AreEqual("Day 1", vm.SelectedPlan.Tabs[0].Name);
    }

    [TestMethod]
    public void AddFoodRow_InsertsRow()
    {
        var plan = new DailyFoodPlan { Id = "1", Tabs = new List<DailyFoodPlanTab> { new() } };
        _mock.Setup(s => s.GetAll()).Returns(new List<DailyFoodPlan> { plan });

        var vm = new DayPlanViewModel(_mock.Object);
        vm.SelectedPlan = vm.Plans[0];
        vm.AddFoodRowCommand.Execute(null);

        Assert.AreEqual(1, vm.SelectedPlan!.Tabs[0].Rows.Count);
    }
}
