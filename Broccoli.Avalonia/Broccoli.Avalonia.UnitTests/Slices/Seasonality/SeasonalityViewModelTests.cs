using Broccoli.Avalonia.Models;
using Broccoli.Avalonia.Seasonality;
using Broccoli.Avalonia.Slices.Seasonality;
using Moq;

namespace Broccoli.Avalonia.Tests.Slices.Seasonality;

[TestClass]
public class SeasonalityViewModelTests
{
    private readonly Mock<ISeasonalityDataStore> _storeMock = new();

    [TestMethod]
    public void Load_PopulatesItemsFromStore()
    {
        var items = new List<ProduceItem>
        {
            MakeProduce("apple", "fruit", ["summer", "autumn"]),
            MakeProduce("broccoli", "vegetable", ["winter"]),
        };
        _storeMock.Setup(s => s.GetAll()).Returns(items);

        SeasonalityViewModel vm = CreateViewModel();

        Assert.AreEqual(2, vm.Items.Count);
        Assert.AreEqual(2, vm.TotalCount);
        Assert.AreEqual("Apple", vm.Items[0].DisplayName);
    }

    [TestMethod]
    public void ChangeMonth_UpdatesInSeasonState()
    {
        _storeMock.Setup(s => s.GetAll()).Returns(new List<ProduceItem>
        {
            MakeProduce("apricot", "fruit", ["summer"]),
        });

        SeasonalityViewModel vm = CreateViewModel();
        ProduceItemRowViewModel row = vm.Items[0];

        vm.SelectedMonthIndex = 0; // January -> summer
        Assert.IsTrue(row.IsInSeason);

        vm.SelectedMonthIndex = 6; // July -> winter
        Assert.IsFalse(row.IsInSeason);
    }

    [TestMethod]
    public void MonthBanner_ReflectsSelectedMonth()
    {
        _storeMock.Setup(s => s.GetAll()).Returns(new List<ProduceItem>());
        SeasonalityViewModel vm = CreateViewModel();

        vm.SelectedMonthIndex = 0; // January

        Assert.AreEqual("January → Summer", vm.SeasonBannerText);
    }

    [TestMethod]
    public void AddItem_StartsEditingNewItem()
    {
        _storeMock.Setup(s => s.GetAll()).Returns(new List<ProduceItem>());
        SeasonalityViewModel vm = CreateViewModel();

        vm.AddItemCommand.Execute(null);

        Assert.IsTrue(vm.IsEditing);
        Assert.IsNotNull(vm.EditingRow);
        Assert.IsTrue(vm.EditingRow!.IsNewItem);
    }

    [TestMethod]
    public void SaveEdit_NewItem_AddsToStore()
    {
        ProduceItem? added = null;
        _storeMock.Setup(s => s.GetAll()).Returns(new List<ProduceItem>());
        _storeMock.Setup(s => s.Add(It.IsAny<ProduceItem>()))
            .Callback<ProduceItem>(item => added = item)
            .Returns((ProduceItem item) => item);

        SeasonalityViewModel vm = CreateViewModel();
        vm.AddItemCommand.Execute(null);
        vm.EditingRow!.Name = "Feijoa";
        vm.EditingRow.InSummer = true;
        vm.SaveEditCommand.Execute(null);

        Assert.IsNotNull(added);
        Assert.AreEqual("Feijoa", added!.Name);
        Assert.IsTrue(added.Seasons.Contains("summer"));
        Assert.IsFalse(string.IsNullOrWhiteSpace(added.Id));
        Assert.IsFalse(vm.IsEditing);
        _storeMock.Verify(s => s.Add(It.IsAny<ProduceItem>()), Times.Once);
    }

    [TestMethod]
    public void SaveEdit_ExistingItem_UpdatesStore()
    {
        ProduceItem item = MakeProduce("apple", "fruit", ["summer", "autumn"]);
        _storeMock.Setup(s => s.GetAll()).Returns(new List<ProduceItem> { item });

        SeasonalityViewModel vm = CreateViewModel();
        vm.StartEditCommand.Execute(vm.Items[0]);
        vm.EditingRow!.Name = "Apple (edited)";
        vm.SaveEditCommand.Execute(null);

        Assert.AreEqual("Apple (edited)", item.Name);
        _storeMock.Verify(s => s.Update(It.Is<ProduceItem>(p => p.Name == "Apple (edited)")), Times.Once);
        Assert.IsFalse(vm.IsEditing);
    }

    [TestMethod]
    public void SaveEdit_EmptyName_ShowsError()
    {
        _storeMock.Setup(s => s.GetAll()).Returns(new List<ProduceItem>());
        SeasonalityViewModel vm = CreateViewModel();

        vm.AddItemCommand.Execute(null);
        vm.SaveEditCommand.Execute(null);

        Assert.IsNotNull(vm.ErrorMessage);
        Assert.IsTrue(vm.IsEditing);
        _storeMock.Verify(s => s.Add(It.IsAny<ProduceItem>()), Times.Never);
    }

    [TestMethod]
    public void DeleteItem_CallsStore()
    {
        ProduceItem item = MakeProduce("apple", "fruit", ["summer"]);
        _storeMock.Setup(s => s.GetAll()).Returns(new List<ProduceItem> { item });

        SeasonalityViewModel vm = CreateViewModel();
        vm.DeleteItemCommand.Execute(vm.Items[0]);

        _storeMock.Verify(s => s.Delete(item.Id), Times.Once);
    }

    [TestMethod]
    public void ResetData_RestoresSeedData()
    {
        _storeMock.Setup(s => s.GetAll()).Returns(new List<ProduceItem>());
        SeasonalityViewModel vm = CreateViewModel();

        vm.ResetDataCommand.Execute(null);

        _storeMock.Verify(s => s.Reset(), Times.Once);
    }

    [TestMethod]
    public void TypeFilter_FiltersList()
    {
        _storeMock.Setup(s => s.GetAll()).Returns(new List<ProduceItem>
        {
            MakeProduce("apple", "fruit", ["summer"]),
            MakeProduce("broccoli", "vegetable", ["winter"]),
        });

        SeasonalityViewModel vm = CreateViewModel();
        Assert.AreEqual(2, vm.FilteredItems.Count);

        vm.SelectedTypeFilterIndex = 1; // Fruit

        Assert.AreEqual(1, vm.FilteredItems.Count);
        Assert.AreEqual("Apple", vm.FilteredItems[0].DisplayName);
    }

    [TestMethod]
    public void SearchText_FiltersList()
    {
        _storeMock.Setup(s => s.GetAll()).Returns(new List<ProduceItem>
        {
            MakeProduce("apple", "fruit", ["summer"]),
            MakeProduce("broccoli", "vegetable", ["winter"]),
        });

        SeasonalityViewModel vm = CreateViewModel();

        vm.SearchText = "broc";

        Assert.AreEqual(1, vm.FilteredItems.Count);
        Assert.AreEqual("Broccoli", vm.FilteredItems[0].DisplayName);
    }

    private SeasonalityViewModel CreateViewModel()
    {
        return new SeasonalityViewModel(_storeMock.Object);
    }

    private static ProduceItem MakeProduce(string id, string type, string[] seasons)
    {
        return new ProduceItem
        {
            Id = id,
            Name = char.ToUpperInvariant(id[0]) + id[1..],
            Type = type,
            Seasons = seasons.ToList(),
        };
    }
}
