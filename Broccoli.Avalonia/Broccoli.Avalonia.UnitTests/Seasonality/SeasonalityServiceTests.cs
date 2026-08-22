using Broccoli.Avalonia.IngredientParsing;
using Broccoli.Avalonia.Models;
using Broccoli.Avalonia.Seasonality;
using CommunityToolkit.Mvvm.Messaging;
using Moq;

namespace Broccoli.Avalonia.Tests.Seasonality;

[TestClass]
public class SeasonalityServiceTests
{
    [TestMethod]
    public void Score_UsesStoreData_InSeason()
    {
        Mock<ISeasonalityDataStore> store = CreateStoreWithFeijoa();
        SeasonalityService service = new(store.Object);

        SeasonalityResult result = service.Score([MakeFeijoaMatch()], new DateTime(2000, 1, 15));

        Assert.IsNotNull(result.Score);
        Assert.AreEqual(100, result.Score!.Value);
        Assert.AreEqual(SeasonalityLabel.PeakSeason, result.Label);
        Assert.AreEqual(1, result.Breakdown.Count);
        Assert.IsTrue(result.Breakdown[0].IsInSeason);
    }

    [TestMethod]
    public void Score_UsesStoreData_OutOfSeason()
    {
        Mock<ISeasonalityDataStore> store = CreateStoreWithFeijoa();
        SeasonalityService service = new(store.Object);

        SeasonalityResult result = service.Score([MakeFeijoaMatch()], new DateTime(2000, 7, 15));

        Assert.AreEqual(0, result.Score);
        Assert.AreEqual(SeasonalityLabel.OffSeason, result.Label);
        Assert.IsFalse(result.Breakdown[0].IsInSeason);
    }

    [TestMethod]
    public void Score_UnmatchedIngredient_IsUnavailable()
    {
        Mock<ISeasonalityDataStore> store = CreateStoreWithFeijoa();
        SeasonalityService service = new(store.Object);

        var match = new ParsedIngredientMatch
        {
            ParsedIngredient = new ParsedIngredient { RawLine = "100 g unicorn", Quantity = 100, Unit = "g", CanonicalUnit = "g", FoodDescription = "unicorn" },
            MatchedFood = new Food { Name = "Unicorn" },
            MatchScore = 1,
            MatchDistance = 0,
            MatchMethod = "Exact",
            IsMatched = true,
        };

        SeasonalityResult result = service.Score([match], new DateTime(2000, 1, 15));

        Assert.IsNull(result.Score);
        Assert.AreEqual(SeasonalityLabel.Unavailable, result.Label);
        Assert.AreEqual(0, result.Breakdown.Count);
    }

    [TestMethod]
    public void Reloads_WhenDataChanged()
    {
        var store = new Mock<ISeasonalityDataStore>();
        store.Setup(s => s.GetAll()).Returns(new List<ProduceItem>());
        SeasonalityService service = new(store.Object);

        store.Setup(s => s.GetAll()).Returns(new List<ProduceItem>
        {
            new() { Id = "kiwifruit", Name = "Kiwifruit", Type = "fruit", Seasons = ["summer"] },
        });
        WeakReferenceMessenger.Default.Send(new Broccoli.Avalonia.Shared.SeasonalityDataChangedMessage());

        SeasonalityResult result = service.Score([MakeMatch("Kiwifruit")], new DateTime(2000, 1, 15));

        Assert.IsNotNull(result.Score);
        Assert.AreEqual(1, result.Breakdown.Count);
    }

    [TestMethod]
    public void NormaliseName_RemovesStopwordsAndPlurals()
    {
        Assert.AreEqual("strawberry", SeasonalityService.NormaliseName("fresh strawberries"));
        Assert.AreEqual("potato", SeasonalityService.NormaliseName("potatoes"));
        Assert.AreEqual("apple", SeasonalityService.NormaliseName("Apple"));
    }

    private static Mock<ISeasonalityDataStore> CreateStoreWithFeijoa()
    {
        var store = new Mock<ISeasonalityDataStore>();
        store.Setup(s => s.GetAll()).Returns(new List<ProduceItem>
        {
            new() { Id = "feijoa", Name = "Feijoa", Type = "fruit", Seasons = ["summer"] },
        });
        return store;
    }

    private static ParsedIngredientMatch MakeFeijoaMatch() => MakeMatch("Feijoa");

    private static ParsedIngredientMatch MakeMatch(string name)
    {
        return new ParsedIngredientMatch
        {
            ParsedIngredient = new ParsedIngredient { RawLine = $"100 g {name.ToLowerInvariant()}", Quantity = 100, Unit = "g", CanonicalUnit = "g", FoodDescription = name.ToLowerInvariant() },
            MatchedFood = new Food { Name = name },
            MatchScore = 1,
            MatchDistance = 0,
            MatchMethod = "Exact",
            IsMatched = true,
        };
    }
}
