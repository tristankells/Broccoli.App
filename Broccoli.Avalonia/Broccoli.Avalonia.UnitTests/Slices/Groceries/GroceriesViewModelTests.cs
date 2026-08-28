using Broccoli.Avalonia.IngredientParsing;
using Broccoli.Avalonia.Models;
using Broccoli.Avalonia.Slices.Groceries;
using Moq;

namespace Broccoli.Avalonia.Tests.Slices.Groceries;

[TestClass]
public class GroceriesViewModelTests
{
    private readonly Mock<IGroceryListService> _serviceMock = new();

    [TestMethod]
    public void LoadItems_PopulatesFromService()
    {
        var existing = new List<GroceryListItem> { MakeItem("Milk"), MakeItem("Eggs") };
        GroceriesViewModel vm = CreateViewModel(existing);

        Assert.AreEqual(2, vm.Items.Count);
        Assert.AreEqual("Milk", vm.Items[0].Name);
    }

    [TestMethod]
    public void AddItem_AddsToCollectionAndService()
    {
        GroceryListItem? saved = null;
        _serviceMock.Setup(s => s.Add(It.IsAny<GroceryListItem>()))
            .Callback<GroceryListItem>(item => saved = item)
            .Returns((GroceryListItem item) =>
            {
                item.Id = "new";
                return item;
            });

        GroceriesViewModel vm = CreateViewModel();
        vm.NewItemText = "Bread";
        vm.AddItemCommand.Execute(null);

        Assert.AreEqual(1, vm.Items.Count);
        Assert.AreEqual("Bread", vm.Items[0].Name);
        Assert.IsNotNull(saved);
    }

    [TestMethod]
    public void AddItem_EmptyText_DoesNotAdd()
    {
        GroceriesViewModel vm = CreateViewModel();
        vm.NewItemText = "   ";
        vm.AddItemCommand.Execute(null);

        Assert.AreEqual(0, vm.Items.Count);
    }

    [TestMethod]
    public void AddItem_MeasureContainsFoodName_SetsQuantityHint()
    {
        Food apple = MakeFood(1, "Apple", "apple", 150);
        GroceriesViewModel vm = CreateViewModel(
            null,
            CreateParser(new Dictionary<string, Food> { { "apples", apple } }));
        AddReturnsItem();

        vm.NewItemText = "2 apples";
        vm.AddItemCommand.Execute(null);

        Assert.AreEqual(1, vm.Items.Count);
        Assert.AreEqual("(~300g)", vm.Items[0].QuantityHint);
    }

    [TestMethod]
    public void AddItem_UnitMatchesMeasure_SetsQuantityHint()
    {
        Food flour = MakeFood(2, "Flour", "cup", 120);
        GroceriesViewModel vm = CreateViewModel(
            null,
            CreateParser(new Dictionary<string, Food> { { "flour", flour } }));
        AddReturnsItem();

        vm.NewItemText = "2 cups flour";
        vm.AddItemCommand.Execute(null);

        Assert.AreEqual(1, vm.Items.Count);
        Assert.AreEqual("(~240g)", vm.Items[0].QuantityHint);
    }

    [TestMethod]
    public void AddItem_MissingUnitDoesNotMatchMeasure_NoQuantityHint()
    {
        Food flour = MakeFood(4, "Flour", "cup", 120);
        GroceriesViewModel vm = CreateViewModel(
            null,
            CreateParser(new Dictionary<string, Food> { { "flour", flour } }));
        AddReturnsItem();

        vm.NewItemText = "2 flour";
        vm.AddItemCommand.Execute(null);

        Assert.AreEqual(1, vm.Items.Count);
        Assert.AreEqual(string.Empty, vm.Items[0].QuantityHint);
    }

    [TestMethod]
    public void AddItem_MetricMeasureDoesNotContainFoodName_NoQuantityHint()
    {
        Food chicken = MakeFood(3, "Chicken Breast", "100g", 100);
        GroceriesViewModel vm = CreateViewModel(
            null,
            CreateParser(new Dictionary<string, Food> { { "chicken breast", chicken } }));
        AddReturnsItem();

        vm.NewItemText = "250g chicken breast";
        vm.AddItemCommand.Execute(null);

        Assert.AreEqual(1, vm.Items.Count);
        Assert.AreEqual(string.Empty, vm.Items[0].QuantityHint);
    }

    [TestMethod]
    public void AddItem_SetsMatchedFoodInfo_WhenQuantityHintPresent()
    {
        Food apple = MakeFood(1, "Apple", "apple", 150);
        GroceriesViewModel vm = CreateViewModel(
            null,
            CreateParser(new Dictionary<string, Food> { { "apples", apple } }));
        AddReturnsItem();

        vm.NewItemText = "2 apples";
        vm.AddItemCommand.Execute(null);

        Assert.AreEqual(1, vm.Items.Count);
        Assert.AreEqual("Apple (100% match, Exact)", vm.Items[0].MatchedFoodInfo);
    }

    [TestMethod]
    public void AddItem_FuzzyMatch_SetsMatchedFoodInfoWithPercent()
    {
        Food apple = MakeFood(1, "Granny Smith Apple", "medium apple", 150);
        GroceriesViewModel vm = CreateViewModel(
            null,
            CreateParser(
                new Dictionary<string, Food> { { "granny smith apples", apple } },
                score: 0.87,
                method: "Fuzzy"));
        AddReturnsItem();

        vm.NewItemText = "2 granny smith apples";
        vm.AddItemCommand.Execute(null);

        Assert.AreEqual(1, vm.Items.Count);
        Assert.AreEqual("Granny Smith Apple (87% match, Fuzzy)", vm.Items[0].MatchedFoodInfo);
    }

    [TestMethod]
    public void AddItem_MatchedFoodInfo_Null_WhenNoQuantityHint()
    {
        Food chicken = MakeFood(3, "Chicken Breast", "100g", 100);
        GroceriesViewModel vm = CreateViewModel(
            null,
            CreateParser(new Dictionary<string, Food> { { "chicken breast", chicken } }));
        AddReturnsItem();

        vm.NewItemText = "250g chicken breast";
        vm.AddItemCommand.Execute(null);

        Assert.AreEqual(1, vm.Items.Count);
        Assert.IsNull(vm.Items[0].MatchedFoodInfo);
    }

    [TestMethod]
    public void AddItem_PluralItemMatchesMeasure_SetsQuantityHint()
    {
        Food potatoes = MakeFood(13, "Potatoes", "Potato", 213);
        GroceriesViewModel vm = CreateViewModel(
            null,
            CreateParser(new Dictionary<string, Food> { { "Potatoes", potatoes } }));
        AddReturnsItem();

        vm.NewItemText = "8 Potatoes";
        vm.AddItemCommand.Execute(null);

        Assert.AreEqual(1, vm.Items.Count);
        Assert.AreEqual("(~1704g)", vm.Items[0].QuantityHint);
    }

    [TestMethod]
    public void AddItem_SingularItemMatchesMeasure_SetsQuantityHint()
    {
        Food potatoes = MakeFood(13, "Potatoes", "Potato", 213);
        GroceriesViewModel vm = CreateViewModel(
            null,
            CreateParser(new Dictionary<string, Food> { { "potato", potatoes } }));
        AddReturnsItem();

        vm.NewItemText = "Potato";
        vm.AddItemCommand.Execute(null);

        Assert.AreEqual(1, vm.Items.Count);
        Assert.AreEqual("(~213g)", vm.Items[0].QuantityHint);
    }

    [TestMethod]
    public void CheckingItem_PersistsAndMovesBelowUnchecked()
    {
        GroceryListItem milk = MakeItem("Milk", false);
        GroceryListItem eggs = MakeItem("Eggs", false);
        GroceriesViewModel vm = CreateViewModel(new List<GroceryListItem> { milk, eggs });

        vm.Items[0].IsChecked = true;

        Assert.IsTrue(milk.IsChecked);
        Assert.AreEqual("Eggs", vm.Items[0].Name);
        Assert.AreEqual("Milk", vm.Items[1].Name);
        _serviceMock.Verify(s => s.Update(It.IsAny<GroceryListItem>()), Times.Once);
    }

    [TestMethod]
    public void CheckingItem_ServiceError_RollsBack()
    {
        GroceryListItem item = MakeItem("Milk", false);
        _serviceMock.Setup(s => s.Update(It.IsAny<GroceryListItem>()))
            .Throws(new Exception("DB error"));
        GroceriesViewModel vm = CreateViewModel(new List<GroceryListItem> { item });

        vm.Items[0].IsChecked = true;

        Assert.IsFalse(vm.Items[0].IsChecked);
        Assert.IsTrue(vm.ErrorMessage!.Contains("Error updating"));
    }

    [TestMethod]
    public void CheckingItem_MovesBelowUncheckedItems()
    {
        GroceryListItem bread = MakeItem("Bread", false);
        GroceryListItem milk = MakeItem("Milk", false);
        GroceryListItem eggs = MakeItem("Eggs", true);
        GroceriesViewModel vm = CreateViewModel(new List<GroceryListItem> { bread, milk, eggs });

        Assert.AreEqual("Bread", vm.Items[0].Name);
        Assert.AreEqual("Milk", vm.Items[1].Name);
        Assert.AreEqual("Eggs", vm.Items[2].Name);

        vm.Items[0].IsChecked = true;

        Assert.AreEqual("Milk", vm.Items[0].Name);
        Assert.AreEqual("Bread", vm.Items[1].Name);
        Assert.AreEqual("Eggs", vm.Items[2].Name);
    }

    [TestMethod]
    public void UncheckingItem_MovesAboveCheckedItems()
    {
        GroceryListItem bread = MakeItem("Bread", false);
        GroceryListItem eggs = MakeItem("Eggs", true);
        GroceryListItem milk = MakeItem("Milk", true);
        GroceriesViewModel vm = CreateViewModel(new List<GroceryListItem> { bread, eggs, milk });

        Assert.AreEqual("Bread", vm.Items[0].Name);
        Assert.AreEqual("Eggs", vm.Items[1].Name);
        Assert.AreEqual("Milk", vm.Items[2].Name);

        vm.Items[2].IsChecked = false;

        Assert.AreEqual("Bread", vm.Items[0].Name);
        Assert.AreEqual("Milk", vm.Items[1].Name);
        Assert.AreEqual("Eggs", vm.Items[2].Name);
    }

    [TestMethod]
    public void DeleteItem_RemovesAndCallsDelete()
    {
        GroceryListItem item = MakeItem("Milk");
        GroceriesViewModel vm = CreateViewModel(new List<GroceryListItem> { item });

        vm.DeleteItemCommand.Execute(vm.Items[0]);

        Assert.AreEqual(0, vm.Items.Count);
        _serviceMock.Verify(s => s.Delete(item.Id), Times.Once);
    }

    [TestMethod]
    public void DeleteItem_Error_RestoresItem()
    {
        GroceryListItem item = MakeItem("Milk");
        _serviceMock.Setup(s => s.Delete(It.IsAny<string>()))
            .Throws(new Exception("DB error"));
        GroceriesViewModel vm = CreateViewModel(new List<GroceryListItem> { item });

        vm.DeleteItemCommand.Execute(vm.Items[0]);

        Assert.AreEqual(1, vm.Items.Count);
        Assert.IsTrue(vm.ErrorMessage!.Contains("Error deleting"));
    }

    [TestMethod]
    public void ResetList_ClearsAllItems()
    {
        GroceriesViewModel vm = CreateViewModel(new List<GroceryListItem> { MakeItem("Milk"), MakeItem("Eggs") });

        vm.ResetListCommand.Execute(null);

        Assert.AreEqual(0, vm.Items.Count);
        _serviceMock.Verify(s => s.Reset(), Times.Once);
    }

    [TestMethod]
    public void StatusText_CalculatesCheckedCount()
    {
        GroceriesViewModel vm = CreateViewModel(new List<GroceryListItem>
        {
            MakeItem("Milk", true),
            MakeItem("Eggs", false),
            MakeItem("Bread", true),
        });

        Assert.AreEqual("2 of 3 checked", vm.StatusText);
    }

    [TestMethod]
    public void StartEdit_EntersEditModeWithCurrentName()
    {
        GroceryListItem item = MakeItem("Milk");
        GroceriesViewModel vm = CreateViewModel(new List<GroceryListItem> { item });

        vm.StartEditCommand.Execute(vm.Items[0]);

        Assert.IsTrue(vm.Items[0].IsEditing);
        Assert.AreEqual("Milk", vm.Items[0].EditText);
    }

    [TestMethod]
    public void CommitEdit_UpdatesNameAndRaisesPropertyChanged()
    {
        GroceryListItem item = MakeItem("Milk");
        var changedProperties = new List<string?>();
        item.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName);
        GroceriesViewModel vm = CreateViewModel(new List<GroceryListItem> { item });
        vm.StartEditCommand.Execute(vm.Items[0]);

        vm.Items[0].EditText = "Whole Milk";
        vm.CommitEditCommand.Execute(vm.Items[0]);

        Assert.AreEqual("Whole Milk", vm.Items[0].Name);
        Assert.IsFalse(vm.Items[0].IsEditing);
        Assert.IsTrue(changedProperties.Contains(nameof(GroceryListItem.Name)), "Name change must be observable so the UI updates without a tab switch.");
        _serviceMock.Verify(s => s.Update(It.IsAny<GroceryListItem>()), Times.Once);
    }

    [TestMethod]
    public void NameSetter_RaisesPropertyChanged()
    {
        GroceryListItem item = MakeItem("Milk");
        bool changed = false;
        item.PropertyChanged += (_, e) => changed |= e.PropertyName == nameof(GroceryListItem.Name);

        item.Name = "Whole Milk";

        Assert.IsTrue(changed);
    }

    private static GroceryListItem MakeItem(string name, bool isChecked = false) => new()
    {
        Id = Guid.NewGuid().ToString(),
        Name = name,
        IsChecked = isChecked,
    };

    private static Food MakeFood(int id, string name, string measure, double gramsPerMeasure) => new()
    {
        Id = id,
        Name = name,
        Measure = measure,
        GramsPerMeasure = gramsPerMeasure,
    };

    private static IngredientParserService CreateParser(Dictionary<string, Food> foods, double score = 1.0, string method = "Exact")
    {
        var caseInsensitiveFoods = new Dictionary<string, Food>(foods, StringComparer.OrdinalIgnoreCase);
        var foodService = new Mock<IFoodService>();
        foodService.Setup(s => s.FindBestMatch(It.IsAny<string>()))
            .Returns((string description) => caseInsensitiveFoods.TryGetValue(description, out Food? food)
                ? new FoodMatchResult { Food = food, Score = score, Method = method }
                : new FoodMatchResult { Score = 0, Method = "None" });

        return new IngredientParserService(foodService.Object);
    }

    private void AddReturnsItem() =>
        _serviceMock.Setup(s => s.Add(It.IsAny<GroceryListItem>()))
            .Returns((GroceryListItem item) => item);

    private GroceriesViewModel CreateViewModel(List<GroceryListItem>? existingItems = null, IngredientParserService? parser = null)
    {
        List<GroceryListItem> items = existingItems ?? new List<GroceryListItem>();
        _serviceMock.Setup(s => s.GetAll()).Returns(items);
        return new GroceriesViewModel(_serviceMock.Object, parser);
    }
}
