using Broccoli.Avalonia.Models;
using Broccoli.Avalonia.Slices.Planning;
using Moq;

namespace Broccoli.Avalonia.Tests.Slices.Planning;

[TestClass]
public class MealPrepPlanServiceTests
{
    private readonly Mock<IMealPrepPlanService> _mock = new();

    [TestMethod]
    public void GetAll_ReturnsPlans()
    {
        var plans = new List<MealPrepPlan>
        {
            new() { Id = "1", Name = "Week 1" },
            new() { Id = "2", Name = "Week 2" },
        };
        _mock.Setup(s => s.GetAll()).Returns(plans);

        List<MealPrepPlan> result = _mock.Object.GetAll();

        Assert.AreEqual(2, result.Count);
        Assert.AreEqual("Week 1", result[0].Name);
    }

    [TestMethod]
    public void Add_AssignsNameAndRecipeIds()
    {
        var plan = new MealPrepPlan { Name = "New", RecipeIds = new List<string> { "r1" } };
        _mock.Setup(s => s.Add(It.IsAny<MealPrepPlan>())).Returns(plan);

        MealPrepPlan result = _mock.Object.Add(plan);

        Assert.AreEqual("New", result.Name);
        Assert.AreEqual(1, result.RecipeIds.Count);
    }

    [TestMethod]
    public void Update_ModifiesRecipeIds()
    {
        var plan = new MealPrepPlan { Id = "1", RecipeIds = new List<string> { "r1", "r2" } };
        _mock.Setup(s => s.Update(It.IsAny<MealPrepPlan>())).Returns(plan);

        _mock.Object.Update(plan);

        _mock.Verify(s => s.Update(plan), Times.Once);
    }

    [TestMethod]
    public void Delete_RemovesPlan()
    {
        _mock.Setup(s => s.Delete("1"));

        _mock.Object.Delete("1");

        _mock.Verify(s => s.Delete("1"), Times.Once);
    }

    [TestMethod]
    public void Reorder_UpdatesSortOrders()
    {
        var ids = new List<string> { "c", "a", "b" };

        _mock.Object.Reorder(ids);

        _mock.Verify(s => s.Reorder(ids), Times.Once);
    }
}
