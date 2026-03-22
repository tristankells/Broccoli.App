using Broccoli.App.Shared.IngredientParsing;
using Broccoli.App.Shared.Slices.Seasonality;
using Broccoli.Data.Models;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Broccoli.App.Tests.Services;

[TestClass]
public class SeasonalityServiceTests
{
    // -- SeasonHelper: GetCurrentSeason --------------------------------------

    [TestMethod]
    [DataRow(9,  "spring")]
    [DataRow(10, "spring")]
    [DataRow(11, "spring")]
    [DataRow(12, "summer")]
    [DataRow(1,  "summer")]
    [DataRow(2,  "summer")]
    [DataRow(3,  "autumn")]
    [DataRow(4,  "autumn")]
    [DataRow(5,  "autumn")]
    [DataRow(6,  "winter")]
    [DataRow(7,  "winter")]
    [DataRow(8,  "winter")]
    public void GetCurrentSeason_AllMonths_CorrectMapping(int month, string expectedSeason)
    {
        var date = new DateTime(2026, month, 15);
        Assert.AreEqual(expectedSeason, SeasonHelper.GetCurrentSeason(date));
    }

    // -- SeasonHelper: GetScarcityWeight -------------------------------------

    [TestMethod]
    public void GetScarcityWeight_OneSeason_Returns1_0()
    {
        var item = new ProduceItem { Seasons = ["summer"], YearRound = false };
        Assert.AreEqual(1.00, SeasonHelper.GetScarcityWeight(item));
    }

    [TestMethod]
    public void GetScarcityWeight_TwoSeasons_Returns0_75()
    {
        var item = new ProduceItem { Seasons = ["spring", "summer"], YearRound = false };
        Assert.AreEqual(0.75, SeasonHelper.GetScarcityWeight(item));
    }

    [TestMethod]
    public void GetScarcityWeight_ThreeSeasons_Returns0_5()
    {
        var item = new ProduceItem { Seasons = ["spring", "summer", "autumn"], YearRound = false };
        Assert.AreEqual(0.50, SeasonHelper.GetScarcityWeight(item));
    }

    [TestMethod]
    public void GetScarcityWeight_FourSeasons_Returns0_25()
    {
        var item = new ProduceItem { Seasons = ["spring", "summer", "autumn", "winter"], YearRound = false };
        Assert.AreEqual(0.25, SeasonHelper.GetScarcityWeight(item));
    }

    [TestMethod]
    public void GetScarcityWeight_YearRound_Returns0_25_RegardlessOfSeasonCount()
    {
        // year_round: true overrides to 0.25 even with only 1 season listed
        var item = new ProduceItem { Seasons = ["summer"], YearRound = true };
        Assert.AreEqual(0.25, SeasonHelper.GetScarcityWeight(item));
    }

    // -- LocalJsonSeasonalityService: NormaliseName -------------------------

    [TestMethod]
    public void NormaliseName_StripsCommaAndSuffix()
    {
        Assert.AreEqual("carrot", LocalJsonSeasonalityService.NormaliseName("Carrots, Raw"));
    }

    [TestMethod]
    public void NormaliseName_RemovesStopwords()
    {
        Assert.AreEqual("carrot", LocalJsonSeasonalityService.NormaliseName("fresh baby carrot"));
    }

    [TestMethod]
    public void NormaliseName_IrregularPlural_Strawberries()
    {
        Assert.AreEqual("strawberry", LocalJsonSeasonalityService.NormaliseName("Strawberries"));
    }

    [TestMethod]
    public void NormaliseName_NaiveDeplural_Mushrooms()
    {
        Assert.AreEqual("mushroom", LocalJsonSeasonalityService.NormaliseName("Mushrooms"));
    }

    // -- Score: edge cases ---------------------------------------------------

    [TestMethod]
    public void Score_EmptyMatches_ReturnsUnavailable()
    {
        var svc = CreateService();
        var result = svc.Score(new List<ParsedIngredientMatch>());
        Assert.IsNull(result.Score);
        Assert.AreEqual(SeasonalityLabel.Unavailable, result.Label);
    }

    [TestMethod]
    public void Score_NonProduceOnly_ReturnsUnavailable()
    {
        // Chicken and flour are not in the produce dataset
        var svc = CreateService();
        var matches = new List<ParsedIngredientMatch>
        {
            MakeGramsMatch("Chicken", 200),
            MakeGramsMatch("Flour",   150)
        };
        var result = svc.Score(matches);
        Assert.IsNull(result.Score);
        Assert.AreEqual(SeasonalityLabel.Unavailable, result.Label);
    }

    [TestMethod]
    public void Score_GramsBelowNoiseFloor_Excluded()
    {
        // Strawberry (summer only) with 4 g should be excluded ? no produce ? Unavailable
        var svc = CreateService();
        var matches = new List<ParsedIngredientMatch>
        {
            MakeGramsMatch("Strawberry", 4.0)
        };
        var result = svc.Score(matches, new DateTime(2026, 1, 15)); // summer
        Assert.IsNull(result.Score);
        Assert.AreEqual(SeasonalityLabel.Unavailable, result.Label);
    }

    [TestMethod]
    public void Score_UnmatchedIngredient_IsSkipped()
    {
        var svc = CreateService();
        var matches = new List<ParsedIngredientMatch>
        {
            MakeGramsMatch("Strawberry", 100, isMatched: false)
        };
        var result = svc.Score(matches, new DateTime(2026, 1, 15)); // summer
        Assert.IsNull(result.Score);
        Assert.AreEqual(SeasonalityLabel.Unavailable, result.Label);
    }

    // -- Score: algorithm correctness ----------------------------------------

    [TestMethod]
    public void Score_SingleInSeasonIngredient_Returns100()
    {
        // Strawberry: seasons=[spring, summer], year_round=false ? in season in summer
        var svc = CreateService();
        var matches = new List<ParsedIngredientMatch> { MakeGramsMatch("Strawberry", 100) };
        var result  = svc.Score(matches, new DateTime(2026, 1, 15)); // summer
        Assert.IsNotNull(result.Score);
        Assert.AreEqual(100.0, result.Score!.Value, delta: 0.01);
        Assert.AreEqual(SeasonalityLabel.PeakSeason, result.Label);
    }

    [TestMethod]
    public void Score_SingleOutOfSeasonIngredient_Returns0()
    {
        // Strawberry: seasons=[spring, summer] ? out of season in winter
        var svc = CreateService();
        var matches = new List<ParsedIngredientMatch> { MakeGramsMatch("Strawberry", 100) };
        var result  = svc.Score(matches, new DateTime(2026, 7, 15)); // winter
        Assert.IsNotNull(result.Score);
        Assert.AreEqual(0.0, result.Score!.Value, delta: 0.01);
        Assert.AreEqual(SeasonalityLabel.OffSeason, result.Label);
    }

    [TestMethod]
    public void Score_YearRoundIngredient_AlwaysInSeason()
    {
        // Carrot: year_round=true ? always in season ? score = 100 in any season
        var svc = CreateService();
        foreach (var month in new[] { 1, 4, 7, 10 })
        {
            var result = svc.Score(
                new List<ParsedIngredientMatch> { MakeGramsMatch("Carrot", 100) },
                new DateTime(2026, month, 15));
            Assert.IsNotNull(result.Score, $"Expected score for month {month}");
            Assert.AreEqual(100.0, result.Score!.Value, delta: 0.01,
                message: $"Expected 100 for month {month}");
        }
    }

    [TestMethod]
    public void Score_MixedIngredients_CorrectWeightedScore()
    {
        // Scoring in summer (Dec/Jan/Feb):
        //
        //   Strawberry: seasons=[spring,summer], year_round=false, scarcity=0.75
        //     ? inSeason=true, contribution = 0.75 * 100 = 75, possible = 75
        //
        //   Blackberry: seasons=[summer], year_round=false, scarcity=1.0
        //     ? inSeason=true, contribution = 1.0 * 80 = 80, possible = 80
        //
        //   Feijoa: seasons=[autumn,winter], year_round=false, scarcity=0.75
        //     ? inSeason=false, contribution = 0, possible = 0.75 * 60 = 45
        //
        // totalWeighted = 75 + 80 + 0 = 155
        // totalPossible = 75 + 80 + 45 = 200
        // score = (155/200)*100 = 77.5 ? PeakSeason

        var svc = CreateService();
        var matches = new List<ParsedIngredientMatch>
        {
            MakeGramsMatch("Strawberry", 100),
            MakeGramsMatch("Blackberry", 80),
            MakeGramsMatch("Feijoa",     60)
        };
        var result = svc.Score(matches, new DateTime(2026, 1, 15)); // summer

        Assert.IsNotNull(result.Score);
        Assert.AreEqual(77.5, result.Score!.Value, delta: 0.1);
        Assert.AreEqual(SeasonalityLabel.PeakSeason, result.Label);
        Assert.AreEqual(3, result.Breakdown.Count);
    }

    [TestMethod]
    public void Score_AllYearRound_Returns100_InAllSeasons()
    {
        // Carrot and Broccoli are both year_round=true — recipe always scores 100
        var svc = CreateService();
        var matches = new List<ParsedIngredientMatch>
        {
            MakeGramsMatch("Carrot",   200),
            MakeGramsMatch("Broccoli", 150)
        };
        foreach (var month in new[] { 1, 4, 7, 10 })
        {
            var result = svc.Score(matches, new DateTime(2026, month, 15));
            Assert.IsNotNull(result.Score, $"month {month}");
            Assert.AreEqual(100.0, result.Score!.Value, delta: 0.01, message: $"month {month}");
        }
    }

    // -- Score: breakdown content ---------------------------------------------

    [TestMethod]
    public void Score_Breakdown_ContainsCorrectDetails()
    {
        var svc = CreateService();
        var matches = new List<ParsedIngredientMatch> { MakeGramsMatch("Strawberry", 100) };
        var result  = svc.Score(matches, new DateTime(2026, 1, 15)); // summer

        Assert.AreEqual(1, result.Breakdown.Count);
        var detail = result.Breakdown[0];
        Assert.AreEqual("Strawberry", detail.Name);
        Assert.IsTrue(detail.IsInSeason);
        Assert.AreEqual(0.75, detail.ScarcityWeight, delta: 0.001); // 2 seasons ? 0.75
        Assert.AreEqual(100.0, detail.WeightInGrams, delta: 0.01);
    }

    [TestMethod]
    public void Score_LimitedSeason_IsLimitedSeason_WhenOutOfSeason()
    {
        // Blackberry: 1 season (summer), scarcity=1.0 ? IsLimitedSeason=true when out of season
        var svc = CreateService();
        var matches = new List<ParsedIngredientMatch> { MakeGramsMatch("Blackberry", 100) };
        var result  = svc.Score(matches, new DateTime(2026, 7, 15)); // winter — out of season

        Assert.AreEqual(1, result.Breakdown.Count);
        var detail = result.Breakdown[0];
        Assert.IsFalse(detail.IsInSeason);
        Assert.AreEqual(1.0, detail.ScarcityWeight, delta: 0.001);
        Assert.IsTrue(detail.IsLimitedSeason);
    }

    // -- Score: BestSeasons --------------------------------------------------

    [TestMethod]
    public void Score_BestSeasons_SingleBestSeason()
    {
        // Blackberry is only in summer ? best in summer
        var svc = CreateService();
        var matches = new List<ParsedIngredientMatch> { MakeGramsMatch("Blackberry", 100) };
        var result  = svc.Score(matches, new DateTime(2026, 7, 15)); // winter

        Assert.AreEqual("Best in summer", result.BestSeasons);
    }

    [TestMethod]
    public void Score_BestSeasons_Unavailable_IsEmpty()
    {
        var svc = CreateService();
        var result = svc.Score(new List<ParsedIngredientMatch>());
        Assert.AreEqual(string.Empty, result.BestSeasons);
    }

    [TestMethod]
    public void Score_BestSeasons_MultipleSeasonsFormatted()
    {
        // Feijoa: seasons=[autumn, winter] ? best in autumn and winter
        var svc = CreateService();
        var matches = new List<ParsedIngredientMatch> { MakeGramsMatch("Feijoa", 100) };
        var result  = svc.Score(matches, new DateTime(2026, 1, 15)); // summer

        Assert.AreEqual("Best in autumn and winter", result.BestSeasons);
    }

    // -- Helpers -------------------------------------------------------------

    /// <summary>
    /// Creates the service using the real embedded nz-produce.json dataset.
    /// This works because the test project references Broccoli.App.Shared,
    /// which carries the embedded resource in its assembly.
    /// </summary>
    private static LocalJsonSeasonalityService CreateService() =>
        new(Mock.Of<ILogger<LocalJsonSeasonalityService>>());

    /// <summary>
    /// Creates a <see cref="ParsedIngredientMatch"/> where Unit="g" so that
    /// <see cref="ParsedIngredientMatch.GetWeightInGrams"/> returns exactly <paramref name="grams"/>.
    /// </summary>
    private static ParsedIngredientMatch MakeGramsMatch(
        string foodName, double grams, bool isMatched = true) =>
        new()
        {
            ParsedIngredient = new ParsedIngredient
            {
                RawLine         = foodName,
                Quantity        = grams,
                Unit            = "g",
                CanonicalUnit   = "g",
                FoodDescription = foodName
            },
            MatchedFood   = isMatched ? new Food { Id = 1, Name = foodName, GramsPerMeasure = 1 } : null,
            MatchScore    = isMatched ? 1.0 : 0.0,
            MatchDistance = 0,
            IsMatched     = isMatched
        };
}

