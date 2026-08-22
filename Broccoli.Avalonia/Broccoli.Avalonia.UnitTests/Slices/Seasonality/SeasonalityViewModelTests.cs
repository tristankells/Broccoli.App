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
        Assert.AreEqual("Apple", vm.Items[0].Name);
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
    public void AddItem_PersistsNewRow()
    {
        var addedItems = new List<ProduceItem>();
        _storeMock.Setup(s => s.GetAll()).Returns(() => addedItems.ToList());
        _storeMock.Setup(s => s.Add(It.IsAny<ProduceItem>()))
            .Callback<ProduceItem>(item => addedItems.Add(item))
            .Returns((ProduceItem item) => item);

        SeasonalityViewModel vm = CreateViewModel();
        vm.AddItemCommand.Execute(null);

        _storeMock.Verify(s => s.Add(It.Is<ProduceItem>(p => p.Name == "New item")), Times.Once);
        Assert.AreEqual(1, vm.Items.Count);
        Assert.AreEqual("New item", vm.Items[0].Name);
    }

    [TestMethod]
    public void EditName_UpdatesStore()
    {
        ProduceItem item = MakeProduce("apple", "fruit", ["summer"]);
        _storeMock.Setup(s => s.GetAll()).Returns(new List<ProduceItem> { item });

        SeasonalityViewModel vm = CreateViewModel();
        vm.Items[0].Name = "Apple (edited)";

        Assert.AreEqual("Apple (edited)", item.Name);
        _storeMock.Verify(s => s.Update(It.Is<ProduceItem>(p => p.Name == "Apple (edited)")), Times.Once);
    }

    [TestMethod]
    public void EditSeasons_UpdatesStore()
    {
        ProduceItem item = MakeProduce("apple", "fruit", ["summer"]);
        _storeMock.Setup(s => s.GetAll()).Returns(new List<ProduceItem> { item });

        SeasonalityViewModel vm = CreateViewModel();
        vm.Items[0].InWinter = true;

        Assert.IsTrue(item.Seasons.Contains("winter"));
        _storeMock.Verify(s => s.Update(It.Is<ProduceItem>(p => p.Seasons.Contains("winter"))), Times.Once);
    }

    [TestMethod]
    public void EditYearRound_UpdatesStoreAndDisablesSeasons()
    {
        ProduceItem item = MakeProduce("apple", "fruit", ["summer"]);
        _storeMock.Setup(s => s.GetAll()).Returns(new List<ProduceItem> { item });

        SeasonalityViewModel vm = CreateViewModel();
        ProduceItemRowViewModel row = vm.Items[0];

        row.YearRound = true;

        Assert.IsTrue(item.YearRound);
        Assert.IsFalse(row.CanEditSeasons);
        _storeMock.Verify(s => s.Update(It.Is<ProduceItem>(p => p.YearRound)), Times.Once);
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
        Assert.AreEqual("Apple", vm.FilteredItems[0].Name);
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
        Assert.AreEqual("Broccoli", vm.FilteredItems[0].Name);
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
