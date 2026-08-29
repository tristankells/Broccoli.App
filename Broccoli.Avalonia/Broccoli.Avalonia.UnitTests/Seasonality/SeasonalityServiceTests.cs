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
        Assert.AreEqual(SeasonalityBand.Green, result.Band);
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
        Assert.AreEqual(SeasonalityBand.Red, result.Band);
        Assert.AreEqual(SeasonalityState.OutOfSeason, result.Breakdown[0].State);
    }

    [TestMethod]
    public void Score_PartiallyInSeason_HalfWeight()
    {
        Mock<ISeasonalityDataStore> store = CreateStoreWithFeijoa();
        SeasonalityService service = new(store.Object);

        SeasonalityResult result = service.Score([MakeFeijoaMatch()], new DateTime(2000, 4, 15));

        Assert.AreEqual(50, result.Score);
        Assert.AreEqual(SeasonalityBand.Yellow, result.Band);
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
        Assert.AreEqual(SeasonalityBand.Unavailable, result.Band);
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
        Assert.AreEqual(SeasonalityBand.Unavailable, result.Band);
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
    public void Score_Scarcity_WeightsShortSeasonItemsMore()
    {
        // The score is a scarcity-weighted in-season fraction: a scarce out-of-season item counts
        // more than a year-round staple (gentle curve: ~3.5x for a 1-month item, 2x for a 3-month
        // item, 1x year-round), so 100g of out-of-season strawberry plus 400g of in-season onion
        // scores 400/(3.46*100 + 400) ~= 54%, not the 80% a pure grams average would give.
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
        Assert.AreEqual(54, Math.Round(result.Score!.Value), 0.001);
        Assert.AreEqual(Math.Sqrt(12.0), result.Breakdown[0].ScarcityWeight, 0.001);
        Assert.AreEqual(1, result.Breakdown[1].ScarcityWeight, 0.001);
    }

    [TestMethod]
    public void GetScarcityWeight_ReflectsYearRoundAvailability()
    {
        Assert.AreEqual(1.0, SeasonHelper.GetScarcityWeight(Item(AllInSeason())), 0.001);
        Assert.AreEqual(Math.Sqrt(2.0), SeasonHelper.GetScarcityWeight(Item(OnlyInSeason([1, 2, 3, 4, 5, 6]))), 0.001);
        Assert.AreEqual(2.0, SeasonHelper.GetScarcityWeight(Item(OnlyInSeason([1, 2, 3]))), 0.001);
        // A 1-month item would be sqrt(12) ~= 3.46x.
        Assert.AreEqual(Math.Sqrt(12.0), SeasonHelper.GetScarcityWeight(Item(OnlyInSeason([1]))), 0.001);
    }

    [TestMethod]
    public void GetScarcityWeight_HonorsInfluenceMode()
    {
        ProduceItem shortSeason = Item(OnlyInSeason([1, 2, 3])); // ~3 months/year
        ProduceItem yearRound = Item(AllInSeason());

        Assert.AreEqual(1.0, SeasonHelper.GetScarcityWeight(shortSeason, SeasonalityInfluenceMode.None), 0.001);
        Assert.AreEqual(1.0, SeasonHelper.GetScarcityWeight(yearRound, SeasonalityInfluenceMode.None), 0.001);

        Assert.AreEqual(2.0, SeasonHelper.GetScarcityWeight(shortSeason, SeasonalityInfluenceMode.Gentle), 0.001);
        Assert.AreEqual(4.0, SeasonHelper.GetScarcityWeight(shortSeason, SeasonalityInfluenceMode.Moderate), 0.001);
        Assert.AreEqual(8.0, SeasonHelper.GetScarcityWeight(shortSeason, SeasonalityInfluenceMode.Strong), 0.001);

        Assert.AreEqual(1.0, SeasonHelper.GetScarcityWeight(yearRound, SeasonalityInfluenceMode.Strong), 0.001);
    }

    [TestMethod]
    public void Score_InfluenceModeNone_UsesPlainGramsAverage()
    {
        // Same strawberry + onion recipe as the weighting test, but with influence Off the score
        // is the plain grams-weighted average (80%), confirming the mode is honoured.
        Mock<ISeasonalityDataStore> store = CreateStoreWithProduce(
            new ProduceItem { Id = "strawberry", Name = "Strawberry", Type = "fruit", Months = OnlyInSeason([1]) },
            new ProduceItem { Id = "onion", Name = "Onion", Type = "vegetable", Months = AllInSeason() });
        SeasonalityService service = new(store.Object);
        var thresholds = new SeasonalityThresholds(80, 55, 45, 25, SeasonalityInfluenceMode.None);

        SeasonalityResult result = service.Score([MakeMatch("Strawberry", 100), MakeMatch("Onion", 400)], new DateTime(2000, 7, 15), thresholds);

        Assert.IsNotNull(result.Score);
        Assert.AreEqual(80, Math.Round(result.Score!.Value), 0.001);
        Assert.AreEqual(1, result.Breakdown[0].ScarcityWeight, 0.001);
        Assert.AreEqual(1, result.Breakdown[1].ScarcityWeight, 0.001);
    }

    [TestMethod]
    public void Classify_UsesConfigurableRankingPercentiles()
    {
        // Top 60th / bottom 40th instead of the default top/bottom third.
        var thresholds = new SeasonalityThresholds(80, 55, 45, 25, SeasonalityInfluenceMode.Gentle, TopPercentile: 0.6, BottomPercentile: 0.4);
        List<double> library = [10, 20, 30, 40, 50, 60, 70, 80, 90];

        Assert.AreEqual(SeasonalityBand.Green, SeasonalityService.Classify(60, thresholds, library)); // 62.5th percentile >= 60th
        Assert.AreEqual(SeasonalityBand.Green, SeasonalityService.Classify(70, thresholds, library));
        Assert.AreEqual(SeasonalityBand.Yellow, SeasonalityService.Classify(50, thresholds, library)); // between the cutoffs
        Assert.AreEqual(SeasonalityBand.Red, SeasonalityService.Classify(40, thresholds, library)); // 37.5th percentile <= 40th
        Assert.AreEqual(SeasonalityBand.Red, SeasonalityService.Classify(30, thresholds, library));
    }

    [TestMethod]
    public void Thresholds_FromSettings_ReadsInfluenceAndRanking()
    {
        var settings = new MacroTargetSettings
        {
            SeasonalityInfluenceMode = SeasonalityInfluenceMode.Strong,
            SeasonalityTopPercentile = 0.5,
            SeasonalityBottomPercentile = 0.2,
        };

        SeasonalityThresholds thresholds = SeasonalityThresholds.FromSettings(settings);

        Assert.AreEqual(SeasonalityInfluenceMode.Strong, thresholds.InfluenceMode);
        Assert.AreEqual(0.5, thresholds.TopPercentile, 0.001);
        Assert.AreEqual(0.2, thresholds.BottomPercentile, 0.001);
        Assert.AreEqual(80, thresholds.GreenAlwaysHigh);
        Assert.AreEqual(25, thresholds.RedAlwaysLow);
    }

    [TestMethod]
    public void IsLimitedSeason_OnlyWhenScarceAndInSeason()
    {
        var scarceInSeason = new IngredientSeasonalityDetail { State = SeasonalityState.InSeason, ScarcityWeight = 4.0 };
        var scarceOutOfSeason = new IngredientSeasonalityDetail { State = SeasonalityState.OutOfSeason, ScarcityWeight = 4.0 };
        var commonInSeason = new IngredientSeasonalityDetail { State = SeasonalityState.InSeason, ScarcityWeight = 1.0 };

        Assert.IsTrue(scarceInSeason.IsLimitedSeason);
        Assert.IsFalse(scarceOutOfSeason.IsLimitedSeason);
        Assert.IsFalse(commonInSeason.IsLimitedSeason);
    }

    // ── Band classification ──────────────────────────────────────────────────

    [TestMethod]
    public void Classify_WithoutLibrary_UsesAbsoluteThresholds()
    {
        SeasonalityThresholds t = SeasonalityThresholds.Default;

        Assert.AreEqual(SeasonalityBand.Green, SeasonalityService.Classify(100, t));
        Assert.AreEqual(SeasonalityBand.Green, SeasonalityService.Classify(80, t));
        Assert.AreEqual(SeasonalityBand.Green, SeasonalityService.Classify(60, t));
        Assert.AreEqual(SeasonalityBand.Yellow, SeasonalityService.Classify(50, t));
        Assert.AreEqual(SeasonalityBand.Red, SeasonalityService.Classify(40, t));
        Assert.AreEqual(SeasonalityBand.Red, SeasonalityService.Classify(25, t));
        Assert.AreEqual(SeasonalityBand.Red, SeasonalityService.Classify(0, t));
        Assert.AreEqual(SeasonalityBand.Unavailable, SeasonalityService.Classify(null, t));
    }

    [TestMethod]
    public void Classify_SmallLibrary_UsesAbsoluteThresholdsOnly()
    {
        // A library of 3 recipes isn't enough for meaningful percentiles.
        SeasonalityThresholds t = SeasonalityThresholds.Default;

        Assert.AreEqual(SeasonalityBand.Green, SeasonalityService.Classify(60, t, [10, 50, 60]));
        Assert.AreEqual(SeasonalityBand.Yellow, SeasonalityService.Classify(50, t, [10, 50, 60]));
        Assert.AreEqual(SeasonalityBand.Red, SeasonalityService.Classify(40, t, [10, 50, 60]));
    }

    [TestMethod]
    public void Classify_TopThirdGreen_BottomThirdRed_WithLibrary()
    {
        SeasonalityThresholds t = SeasonalityThresholds.Default;
        List<double> library = [10, 20, 30, 40, 50, 60, 70, 80, 90];

        // Top third (>= 66.7th percentile) green, bottom third red, middle yellow.
        Assert.AreEqual(SeasonalityBand.Green, SeasonalityService.Classify(90, t, library));
        Assert.AreEqual(SeasonalityBand.Green, SeasonalityService.Classify(80, t, library));
        Assert.AreEqual(SeasonalityBand.Green, SeasonalityService.Classify(70, t, library));
        Assert.AreEqual(SeasonalityBand.Yellow, SeasonalityService.Classify(60, t, library));
        Assert.AreEqual(SeasonalityBand.Yellow, SeasonalityService.Classify(50, t, library));
        Assert.AreEqual(SeasonalityBand.Yellow, SeasonalityService.Classify(40, t, library));
        Assert.AreEqual(SeasonalityBand.Red, SeasonalityService.Classify(30, t, library));
        Assert.AreEqual(SeasonalityBand.Red, SeasonalityService.Classify(20, t, library));
        Assert.AreEqual(SeasonalityBand.Red, SeasonalityService.Classify(10, t, library));
    }

    [TestMethod]
    public void Classify_AbsoluteGuards_OverridePercentile()
    {
        SeasonalityThresholds t = SeasonalityThresholds.Default;

        // Above Always-Green: green even if the whole library scores similarly high.
        List<double> highLibrary = [85, 86, 87, 88, 89, 90, 91, 92, 93];
        Assert.AreEqual(SeasonalityBand.Green, SeasonalityService.Classify(85, t, highLibrary));

        // Below Always-Red: red even if the whole library scores similarly low.
        List<double> lowLibrary = [5, 6, 7, 8, 9, 10, 11, 12, 13];
        Assert.AreEqual(SeasonalityBand.Red, SeasonalityService.Classify(13, t, lowLibrary));

        // Bottom third but above the Red ceiling: the absolute guard wins, so it's not red.
        List<double> lowWithHighCeiling = [1, 46, 47, 48, 49, 50, 51, 52, 53];
        Assert.AreEqual(SeasonalityBand.Yellow, SeasonalityService.Classify(46, t, lowWithHighCeiling));
    }

    // ── ScoreAll + coverage ──────────────────────────────────────────────────

    [TestMethod]
    public void ScoreAll_ClassifiesRelativeToLibrary()
    {
        Mock<ISeasonalityDataStore> store = CreateStoreWithProduce(
            new ProduceItem { Id = "feijoa", Name = "Feijoa", Type = "fruit", Months = OnlyInSeason([1]) },
            new ProduceItem { Id = "onion", Name = "Onion", Type = "vegetable", Months = AllInSeason() });
        SeasonalityService service = new(store.Object);
        DateTime july = new(2000, 7, 15);

        // Feijoa is short-season (weight sqrt(12) ~= 3.5x) and out of season in July; onion is
        // year-round (weight 1x) and in season. Scores work out to [100, 100, 30, 22, 16, 0] ->
        // top third green, middle yellow, bottom third red.
        List<SeasonalityResult> results = service.ScoreAll(
            [
                [MakeMatch("Onion", 100)],
                [MakeMatch("Onion", 100)],
                [MakeMatch("Onion", 60), MakeMatch("Feijoa", 40)],
                [MakeMatch("Onion", 50), MakeMatch("Feijoa", 50)],
                [MakeMatch("Onion", 40), MakeMatch("Feijoa", 60)],
                [MakeMatch("Feijoa", 100)],
            ],
            july);

        Assert.AreEqual(6, results.Count);
        Assert.AreEqual(SeasonalityBand.Green, results[0].Band); // 100, top third
        Assert.AreEqual(SeasonalityBand.Green, results[1].Band); // 100, top third
        Assert.AreEqual(SeasonalityBand.Yellow, results[2].Band); // 30, middle
        Assert.AreEqual(SeasonalityBand.Red, results[3].Band); // 22, at/below Always-Red
        Assert.AreEqual(SeasonalityBand.Red, results[4].Band); // 16, bottom third
        Assert.AreEqual(SeasonalityBand.Red, results[5].Band); // 0, bottom third
    }

    [TestMethod]
    public void Score_ReportsCoverageCounts()
    {
        Mock<ISeasonalityDataStore> store = CreateStoreWithProduce(
            new ProduceItem { Id = "onion", Name = "Onion", Type = "vegetable", Months = AllInSeason() });
        SeasonalityService service = new(store.Object);

        // Two ingredients, but only onion maps to produce.
        ParsedIngredientMatch onion = MakeMatch("Onion");
        ParsedIngredientMatch chicken = MakeMatch("Chicken");
        SeasonalityResult result = service.Score([onion, chicken], new DateTime(2000, 1, 15));

        Assert.AreEqual(1, result.MatchedProduceCount);
        Assert.AreEqual(2, result.TotalIngredientCount);
        Assert.AreEqual("1 of 2 ingredients matched", result.CoverageText);
    }

    [TestMethod]
    public void GetProduceStateForFood_ReturnsStateOrNull()
    {
        Mock<ISeasonalityDataStore> store = CreateStoreWithProduce(
            new ProduceItem { Id = "feijoa", Name = "Feijoa", Type = "fruit", Months = OnlyInSeason([1]) });
        SeasonalityService service = new(store.Object);

        Assert.AreEqual(SeasonalityState.InSeason, service.GetProduceStateForFood("Feijoa", new DateTime(2000, 1, 15)));
        Assert.AreEqual(SeasonalityState.OutOfSeason, service.GetProduceStateForFood("Feijoa", new DateTime(2000, 7, 15)));
        Assert.IsNull(service.GetProduceStateForFood("Chicken", new DateTime(2000, 7, 15)));
    }

    private static Dictionary<int, SeasonalityState> AllInSeason() => OnlyInSeason([1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12]);

    private static ProduceItem Item(Dictionary<int, SeasonalityState> months) => new()
    {
        Id = "item",
        Name = "Item",
        Type = "fruit",
        Months = months,
    };

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

    private static ParsedIngredientMatch MakeMatch(string name, double grams = 100)
    {
        return new ParsedIngredientMatch
        {
            ParsedIngredient = new ParsedIngredient { RawLine = $"{grams:0} g {name.ToLowerInvariant()}", Quantity = grams, Unit = "g", CanonicalUnit = "g", FoodDescription = name.ToLowerInvariant() },
            MatchedFood = new Food { Name = name },
            MatchScore = 1,
            MatchDistance = 0,
            MatchMethod = "Exact",
            IsMatched = true,
        };
    }
}
