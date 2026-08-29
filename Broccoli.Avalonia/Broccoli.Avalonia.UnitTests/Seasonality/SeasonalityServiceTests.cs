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
        Assert.AreEqual(SeasonalityState.InSeason, result.Breakdown[0].State);
    }

    [TestMethod]
    public void Score_UsesStoreData_OutOfSeason()
    {
        Mock<ISeasonalityDataStore> store = CreateStoreWithFeijoa();
        SeasonalityService service = new(store.Object);

        SeasonalityResult result = service.Score([MakeFeijoaMatch()], new DateTime(2000, 7, 15));

        Assert.AreEqual(0, result.Score);
        Assert.AreEqual(SeasonalityLabel.OffSeason, result.Label);
        Assert.AreEqual(SeasonalityState.OutOfSeason, result.Breakdown[0].State);
    }

    [TestMethod]
    public void Score_PartiallyInSeason_HalfWeight()
    {
        Mock<ISeasonalityDataStore> store = CreateStoreWithFeijoa();
        SeasonalityService service = new(store.Object);

        SeasonalityResult result = service.Score([MakeFeijoaMatch()], new DateTime(2000, 4, 15));

        Assert.AreEqual(50, result.Score);
        Assert.AreEqual(SeasonalityLabel.PartiallyInSeason, result.Label);
        Assert.AreEqual(SeasonalityState.PartiallyInSeason, result.Breakdown[0].State);
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
            new()
            {
                Id = "kiwifruit",
                Name = "Kiwifruit",
                Type = "fruit",
                Months = new Dictionary<int, SeasonalityState> { [1] = SeasonalityState.InSeason },
            },
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

    [TestMethod]
    public void Score_Peas_DoesNotMatchPeach()
    {
        Mock<ISeasonalityDataStore> store = CreateStoreWithProduce(
            new ProduceItem { Id = "peach", Name = "Peach", Type = "fruit", Months = AllInSeason() },
            new ProduceItem { Id = "green-peas", Name = "Green Peas", Type = "vegetable", Months = AllInSeason() });
        SeasonalityService service = new(store.Object);

        SeasonalityResult result = service.Score([MakeMatch("Green Peas")], new DateTime(2000, 1, 15));

        Assert.IsNotNull(result.Score);
        Assert.AreEqual(1, result.Breakdown.Count);
        Assert.AreEqual("Green Peas", result.Breakdown[0].Name);
    }

    [TestMethod]
    public void Score_Butter_DoesNotMatchButternut()
    {
        Mock<ISeasonalityDataStore> store = CreateStoreWithProduce(
            new ProduceItem { Id = "butternut", Name = "Butternut", Type = "vegetable", Months = AllInSeason() });
        SeasonalityService service = new(store.Object);

        SeasonalityResult result = service.Score([MakeMatch("Butter")], new DateTime(2000, 1, 15));

        Assert.IsNull(result.Score);
        Assert.AreEqual(SeasonalityLabel.Unavailable, result.Label);
    }

    [TestMethod]
    public void Score_GenericName_MatchesSpecificProduce()
    {
        Mock<ISeasonalityDataStore> store = CreateStoreWithProduce(
            new ProduceItem { Id = "green-cabbage", Name = "Green Cabbage", Type = "vegetable", Months = AllInSeason() });
        SeasonalityService service = new(store.Object);

        SeasonalityResult result = service.Score([MakeMatch("Cabbage")], new DateTime(2000, 1, 15));

        Assert.IsNotNull(result.Score);
        Assert.AreEqual("Green Cabbage", result.Breakdown[0].Name);
    }

    [TestMethod]
    public void Score_QualifiedName_MatchesBaseProduce()
    {
        Mock<ISeasonalityDataStore> store = CreateStoreWithProduce(
            new ProduceItem { Id = "onion", Name = "Onion", Type = "vegetable", Months = AllInSeason() },
            new ProduceItem { Id = "spring-onion", Name = "Spring Onion", Type = "vegetable", Months = AllInSeason() });
        SeasonalityService service = new(store.Object);

        SeasonalityResult result = service.Score([MakeMatch("Red Onion")], new DateTime(2000, 1, 15));

        Assert.IsNotNull(result.Score);
        Assert.AreEqual("Onion", result.Breakdown[0].Name);
    }

    [TestMethod]
    public void Score_Scarcity_DoesNotScaleTheScore()
    {
        // The score must be the grams-weighted in-season fraction, not re-weighted by scarcity:
        // 100g of a scarce out-of-season item + 400g of a common in-season item = 80% (400/500),
        // regardless of the scarcity weights involved.
        Mock<ISeasonalityDataStore> store = CreateStoreWithProduce(
            new ProduceItem { Id = "strawberry", Name = "Strawberry", Type = "fruit", Months = OnlyInSeason([1]) },
            new ProduceItem { Id = "onion", Name = "Onion", Type = "vegetable", Months = AllInSeason() });
        SeasonalityService service = new(store.Object);

        var strawberry = new ParsedIngredientMatch
        {
            ParsedIngredient = new ParsedIngredient { RawLine = "100 g strawberry", Quantity = 100, Unit = "g", CanonicalUnit = "g", FoodDescription = "strawberry" },
            MatchedFood = new Food { Name = "Strawberry" },
            MatchScore = 1, MatchDistance = 0, MatchMethod = "Exact", IsMatched = true,
        };
        var onion = new ParsedIngredientMatch
        {
            ParsedIngredient = new ParsedIngredient { RawLine = "400 g onion", Quantity = 400, Unit = "g", CanonicalUnit = "g", FoodDescription = "onion" },
            MatchedFood = new Food { Name = "Onion" },
            MatchScore = 1, MatchDistance = 0, MatchMethod = "Exact", IsMatched = true,
        };

        // July: strawberry is out of season, onion in season.
        SeasonalityResult result = service.Score([strawberry, onion], new DateTime(2000, 7, 15));

        Assert.IsNotNull(result.Score);
        Assert.AreEqual(80, Math.Round(result.Score!.Value), 0.001);
    }

    [TestMethod]
    public void IsLimitedSeason_OnlyWhenScarceAndInSeason()
    {
        var scarceInSeason = new IngredientSeasonalityDetail { State = SeasonalityState.InSeason, ScarcityWeight = 1.0 };
        var scarceOutOfSeason = new IngredientSeasonalityDetail { State = SeasonalityState.OutOfSeason, ScarcityWeight = 1.0 };
        var commonInSeason = new IngredientSeasonalityDetail { State = SeasonalityState.InSeason, ScarcityWeight = 0.25 };

        Assert.IsTrue(scarceInSeason.IsLimitedSeason);
        Assert.IsFalse(scarceOutOfSeason.IsLimitedSeason);
        Assert.IsFalse(commonInSeason.IsLimitedSeason);
    }

    private static Dictionary<int, SeasonalityState> AllInSeason() => OnlyInSeason([1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12]);

    private static Dictionary<int, SeasonalityState> OnlyInSeason(int[] months)
    {
        var dict = new Dictionary<int, SeasonalityState>();
        foreach (int month in months)
        {
            dict[month] = SeasonalityState.InSeason;
        }

        return dict;
    }

    private static Mock<ISeasonalityDataStore> CreateStoreWithProduce(params ProduceItem[] items)
    {
        var store = new Mock<ISeasonalityDataStore>();
        store.Setup(s => s.GetAll()).Returns(items.ToList());
        return store;
    }

    private static Mock<ISeasonalityDataStore> CreateStoreWithFeijoa()
    {
        var store = new Mock<ISeasonalityDataStore>();
        store.Setup(s => s.GetAll()).Returns(new List<ProduceItem>
        {
            new()
            {
                Id = "feijoa",
                Name = "Feijoa",
                Type = "fruit",
                Months = new Dictionary<int, SeasonalityState>
                {
                    [1] = SeasonalityState.InSeason,
                    [2] = SeasonalityState.InSeason,
                    [4] = SeasonalityState.PartiallyInSeason,
                    [12] = SeasonalityState.InSeason,
                },
            },
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
