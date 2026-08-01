using Broccoli.Avalonia.IngredientParsing;
using Broccoli.Avalonia.Models;
using Moq;

namespace Broccoli.Avalonia.Tests.Slices.Groceries;

[TestClass]
public class IngredientParserServiceTests
{
    private static IngredientParserService CreateService(Dictionary<string, Food>? foods = null)
    {
        var foodService = new Mock<IFoodService>();
        foodService.Setup(s => s.FindBestMatch(It.IsAny<string>()))
            .Returns(new FoodMatchResult { Score = 0, Method = "None" });
        if (foods is not null)
        {
            foreach (var kvp in foods)
            {
                foodService.Setup(s => s.FindBestMatch(kvp.Key))
                    .Returns(new FoodMatchResult { Food = kvp.Value, Score = 1.0, Method = "Exact" });
            }
        }
        return new IngredientParserService(foodService.Object);
    }

    private static Food MakeFood(int id, string name) => new()
    {
        Id = id, Name = name,
        Measure = "cup", GramsPerMeasure = 100,
        CaloriesPer100g = 50
    };

    [TestMethod]
    public void Parse_MetricWeight_ExtractsQuantityAndUnit()
    {
        var service = CreateService();
        var results = service.ParseAndMatchIngredients("250g chicken breast");

        Assert.AreEqual(1, results.Count);
        Assert.AreEqual(250, results[0].ParsedIngredient.Quantity);
        Assert.AreEqual("g", results[0].ParsedIngredient.Unit);
        Assert.AreEqual("chicken breast", results[0].ParsedIngredient.FoodDescription);
    }

    [TestMethod]
    public void Parse_VolumeUnit_ExtractsCorrectly()
    {
        var service = CreateService();
        var results = service.ParseAndMatchIngredients("2 cups flour");

        Assert.AreEqual(1, results.Count);
        Assert.AreEqual(2, results[0].ParsedIngredient.Quantity);
        Assert.AreEqual("cup", results[0].ParsedIngredient.Unit);
    }

    [TestMethod]
    public void Parse_NoUnit_DefaultsToQuantityOne()
    {
        var service = CreateService();
        var results = service.ParseAndMatchIngredients("carrots");

        Assert.AreEqual(1, results.Count);
        Assert.AreEqual(1, results[0].ParsedIngredient.Quantity);
        Assert.AreEqual("carrots", results[0].ParsedIngredient.FoodDescription);
    }

    [TestMethod]
    public void Parse_Fraction_CalculatesCorrectly()
    {
        var service = CreateService();
        var results = service.ParseAndMatchIngredients("1 1/2 cups sugar");

        Assert.AreEqual(1.5, results[0].ParsedIngredient.Quantity);
    }

    [TestMethod]
    public void Parse_MultipleLines_ReturnsMultipleResults()
    {
        var service = CreateService();
        var results = service.ParseAndMatchIngredients("250g chicken\n2 cups flour");

        Assert.AreEqual(2, results.Count);
    }

    [TestMethod]
    public void Parse_EmptyString_ReturnsEmpty()
    {
        var service = CreateService();
        var results = service.ParseAndMatchIngredients("");

        Assert.AreEqual(0, results.Count);
    }

    [TestMethod]
    public void Parse_MatchesAgainstFoodDatabase()
    {
        var food = MakeFood(1, "chicken breast");
        var service = CreateService(new Dictionary<string, Food> { { "chicken breast", food } });
        var results = service.ParseAndMatchIngredients("250g chicken breast");

        Assert.IsTrue(results[0].IsMatched);
        Assert.AreEqual("chicken breast", results[0].MatchedFood!.Name);
    }

    [TestMethod]
    public void Parse_UnmatchedFood_HasNoMatch()
    {
        var service = CreateService();
        var results = service.ParseAndMatchIngredients("250g zargblax");

        Assert.IsFalse(results[0].IsMatched);
    }

    [TestMethod]
    public void Parse_Duplicates_AreMerged()
    {
        var food = MakeFood(1, "chicken breast");
        var service = CreateService(new Dictionary<string, Food> { { "chicken breast", food } });
        var results = service.ParseAndMatchIngredients("250g chicken breast\n250g chicken breast");

        Assert.AreEqual(1, results.Count);
        Assert.AreEqual(500, results[0].ParsedIngredient.Quantity);
    }

    [TestMethod]
    public void Parse_UnitNormalization_LiterToL()
    {
        var service = CreateService();
        var results = service.ParseAndMatchIngredients("1 liter milk");

        Assert.AreEqual("l", results[0].ParsedIngredient.Unit);
    }

    [TestMethod]
    public void Parse_SkipsCommentLines()
    {
        var service = CreateService();
        var results = service.ParseAndMatchIngredients("# My header\n250g chicken");

        Assert.AreEqual(1, results.Count);
        Assert.AreEqual("chicken", results[0].ParsedIngredient.FoodDescription);
    }

    [TestMethod]
    public void Parse_StripNotesAfterComma()
    {
        var service = CreateService();
        var results = service.ParseAndMatchIngredients("Carrots, Raw");

        Assert.AreEqual("Carrots", results[0].ParsedIngredient.FoodDescription);
    }
}
