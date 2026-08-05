using Broccoli.Avalonia.Models;
using Broccoli.Avalonia.Slices.Groceries;
using Moq;

namespace Broccoli.Avalonia.Tests.Slices.Groceries;

[TestClass]
public class GroceriesViewModelTests
{
    private readonly Mock<IGroceryListService> _serviceMock = new();

    private GroceriesViewModel CreateViewModel(List<GroceryListItem>? existingItems = null)
    {
        List<GroceryListItem> items = existingItems ?? new List<GroceryListItem>();
        _serviceMock.Setup(s => s.GetAll()).Returns(items);
        return new GroceriesViewModel(_serviceMock.Object);
    }

    private static GroceryListItem MakeItem(string name, bool isChecked = false) => new()
    {
        Id = Guid.NewGuid().ToString(),
        Name = name,
        IsChecked = isChecked
    };

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
            .Returns((GroceryListItem item) => { item.Id = "new"; return item; });

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
    public void ToggleItem_FlipsIsChecked()
    {
        GroceryListItem item = MakeItem("Milk", false);
        GroceriesViewModel vm = CreateViewModel(new List<GroceryListItem> { item });

        vm.ToggleItemCommand.Execute(vm.Items[0]);

        Assert.IsTrue(vm.Items[0].IsChecked);
        _serviceMock.Verify(s => s.Update(It.IsAny<GroceryListItem>()), Times.Once);
    }

    [TestMethod]
    public void ToggleItem_ServiceError_RollsBack()
    {
        GroceryListItem item = MakeItem("Milk", false);
        _serviceMock.Setup(s => s.Update(It.IsAny<GroceryListItem>()))
            .Throws(new Exception("DB error"));
        GroceriesViewModel vm = CreateViewModel(new List<GroceryListItem> { item });

        vm.ToggleItemCommand.Execute(vm.Items[0]);

        Assert.IsFalse(vm.Items[0].IsChecked);
        Assert.IsTrue(vm.ErrorMessage!.Contains("Error updating"));
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
            MakeItem("Bread", true)
        });

        Assert.AreEqual("2 of 3 checked", vm.StatusText);
    }
}
