using Broccoli.Avalonia.IngredientParsing;
using Broccoli.Avalonia.Models;
using Broccoli.Avalonia.Slices.Groceries;
using Moq;

namespace Broccoli.Avalonia.Tests.Slices.Groceries;

[TestClass]
public class GroceryUnitNormalizerTests
{
    [TestMethod]
    public void Normalize_OffMode_KeepsOriginalLineAndWeightHint()
    {
        (string name, string? hint) = GroceryUnitNormalizer.Normalize(
            Parse("2 carrots", Food("carrots", "Carrot", "Medium Carrot", 61)),
            GroceryUnitNormalization.Off);

        Assert.AreEqual("2 Carrot", name);
        Assert.AreEqual("(~122g)", hint);
    }

    [TestMethod]
    public void Normalize_Grams_ConvertsCountItemToWeightLine()
    {
        (string name, string? hint) = GroceryUnitNormalizer.Normalize(
            Parse("1 onion", Food("onion", "Onion", "Medium Onion", 110)),
            GroceryUnitNormalization.Grams);

        Assert.AreEqual("110g Onion", name);
        Assert.AreEqual("(~1 medium onion)", hint);
    }

    [TestMethod]
    public void Normalize_Grams_ShowsOriginalCountAsHint()
    {
        (string name, string? hint) = GroceryUnitNormalizer.Normalize(
            Parse("2 carrots", Food("carrots", "Carrot", "Medium Carrot", 61)),
            GroceryUnitNormalization.Grams);

        Assert.AreEqual("122g Carrot", name);
        Assert.AreEqual("(~2 medium carrot)", hint);
    }

    [TestMethod]
    public void Normalize_Pounds_ConvertsCountItemToPounds()
    {
        (string name, string? hint) = GroceryUnitNormalizer.Normalize(
            Parse("2 carrots", Food("carrots", "Carrot", "Medium Carrot", 61)),
            GroceryUnitNormalization.Pounds);

        Assert.AreEqual("0.27 lb Carrot", name);
        Assert.AreEqual("(~2 medium carrot)", hint);
    }

    [TestMethod]
    public void Normalize_UnmatchedIngredient_KeepsLineAndHasNoHint()
    {
        (string name, string? hint) = GroceryUnitNormalizer.Normalize(
            Parse("1 zucchini", new Dictionary<string, Food>()),
            GroceryUnitNormalization.Grams);

        Assert.AreEqual("1 zucchini", name);
        Assert.IsNull(hint);
    }

    [TestMethod]
    public void Normalize_WeightMeasuredFood_KeepsLine()
    {
        (string name, string? hint) = GroceryUnitNormalizer.Normalize(
            Parse("250g chicken breast", Food("chicken breast", "Chicken Breast, Skinless", "Gram", 1)),
            GroceryUnitNormalization.Grams);

        Assert.AreEqual("250g Chicken Breast, Skinless", name);
        Assert.IsNull(hint);
    }

    [TestMethod]
    public void Normalize_EachMeasure_OmitMeasureFromHint()
    {
        (string name, string? hint) = GroceryUnitNormalizer.Normalize(
            Parse("1 leek", Food("leek", "Leek", "each", 89)),
            GroceryUnitNormalization.Grams);

        Assert.AreEqual("89g Leek", name);
        Assert.AreEqual("(~1)", hint);
    }

    private static ParsedIngredientMatch Parse(string line, Dictionary<string, Food> foods)
    {
        var caseInsensitiveFoods = new Dictionary<string, Food>(foods, StringComparer.OrdinalIgnoreCase);
        var foodService = new Mock<IFoodService>();
        foodService.Setup(s => s.FindBestMatch(It.IsAny<string>()))
            .Returns((string description) => caseInsensitiveFoods.TryGetValue(description, out Food? food)
                ? new FoodMatchResult { Food = food, Score = 1.0, Method = "Exact" }
                : new FoodMatchResult { Score = 0, Method = "None" });

        var parser = new IngredientParserService(foodService.Object);
        return parser.ParseAndMatchIngredients(line).Single();
    }

    private static Dictionary<string, Food> Food(string key, string name, string measure, double gramsPerMeasure) =>
        new()
        {
            [key] = new Food
            {
                Id = 1,
                Name = name,
                Measure = measure,
                GramsPerMeasure = gramsPerMeasure,
            },
        };
}
