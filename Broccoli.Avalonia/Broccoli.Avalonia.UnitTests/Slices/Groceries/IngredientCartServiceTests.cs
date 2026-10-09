using Broccoli.Avalonia.IngredientParsing;
using Broccoli.Avalonia.Models;
using Broccoli.Avalonia.Slices.Groceries;
using Broccoli.Avalonia.Slices.Planning;
using Moq;

namespace Broccoli.Avalonia.Tests.Slices.Groceries;

[TestClass]
public class IngredientCartServiceTests
{
    [TestMethod]
    public void PreviewAddToCart_MatchedIngredient_PreservesOriginalSpellingAndFormatting()
    {
        IngredientCartService cart = CreateCart(
            foods: new Dictionary<string, Food> { { "carrots", MakeFood(3, "Carrot", "Medium Carrot", 61) } });

        List<CartPreviewData> preview = cart.PreviewAddToCart(["2 carrots"]);

        Assert.AreEqual(1, preview.Count);
        Assert.AreEqual("2 carrots", preview[0].DisplayName);
        Assert.AreEqual("2 carrots", preview[0].OriginalLine);
        Assert.AreEqual("Carrot", preview[0].FoodName);
        Assert.IsFalse(preview[0].IsMerge);
    }

    [TestMethod]
    public void PreviewAddToCart_MatchedIngredient_ShowsFoodMatchHintInBrackets()
    {
        IngredientCartService cart = CreateCart(
            foods: new Dictionary<string, Food> { { "carrots", MakeFood(3, "Carrot", "Medium Carrot", 61) } });

        List<CartPreviewData> preview = cart.PreviewAddToCart(["2 carrots"]);

        Assert.AreEqual(1, preview.Count);
        Assert.AreEqual("(~122g Carrot)", preview[0].FoodMatchHint);
    }

    [TestMethod]
    public void PreviewAddToCart_MatchedGramMeasuredFood_ShowsFoodInBracketsOnly()
    {
        IngredientCartService cart = CreateCart(
            foods: new Dictionary<string, Food>
            {
                { "chicken breast", MakeFood(4, "Chicken Breast, Skinless", "Gram", 1) },
            });

        List<CartPreviewData> preview = cart.PreviewAddToCart(["250g chicken breast"]);

        Assert.AreEqual(1, preview.Count);
        Assert.AreEqual("250g chicken breast", preview[0].DisplayName);
        Assert.AreEqual("(Chicken Breast, Skinless)", preview[0].FoodMatchHint);
    }

    [TestMethod]
    public void PreviewAddToCart_UnmatchedIngredient_KeepsLineAndHasNoHint()
    {
        IngredientCartService cart = CreateCart();

        List<CartPreviewData> preview = cart.PreviewAddToCart(["1 zucchini"]);

        Assert.AreEqual(1, preview.Count);
        Assert.AreEqual("1 zucchini", preview[0].DisplayName);
        Assert.AreEqual("1 zucchini", preview[0].OriginalLine);
        Assert.IsNull(preview[0].FoodMatchHint);
        Assert.IsFalse(preview[0].IsMerge);
    }

    [TestMethod]
    public void PreviewAddToCart_CombiningWithExisting_DoesNotChangeDisplayedLine()
    {
        GroceryListItem existing = MakeListItem("1 Carrot");
        IngredientCartService cart = CreateCart(
            foods: new Dictionary<string, Food> { { "carrots", MakeFood(3, "Carrot", "Medium Carrot", 61) } },
            existingItems: [existing]);

        List<CartPreviewData> preview = cart.PreviewAddToCart(["2 carrots"]);

        Assert.AreEqual(1, preview.Count);
        Assert.IsTrue(preview[0].IsMerge);
        Assert.AreEqual("2 carrots", preview[0].DisplayName);
        Assert.AreEqual("(~122g Carrot)", preview[0].FoodMatchHint);
    }

    [TestMethod]
    public void PreviewAddToCart_CombiningWithExisting_DoesNotMutateExistingItem()
    {
        GroceryListItem existing = MakeListItem("1 Carrot");
        IngredientCartService cart = CreateCart(
            foods: new Dictionary<string, Food> { { "carrots", MakeFood(3, "Carrot", "Medium Carrot", 61) } },
            existingItems: [existing]);

        cart.PreviewAddToCart(["2 carrots"]);

        Assert.AreEqual("1 Carrot", existing.Name);
        Assert.IsNull(existing.QuantityHint);
    }

    [TestMethod]
    public void PreviewAddToCart_GramsMode_ShowsNormalizedLineAndCountHint()
    {
        IngredientCartService cart = CreateCart(
            foods: new Dictionary<string, Food> { { "carrots", MakeFood(3, "Carrot", "Medium Carrot", 61) } },
            mode: GroceryUnitNormalization.Grams);

        List<CartPreviewData> preview = cart.PreviewAddToCart(["2 carrots"]);

        Assert.AreEqual(1, preview.Count);
        Assert.AreEqual("122g Carrot", preview[0].DisplayName);
        Assert.AreEqual("(~2 medium carrot)", preview[0].FoodMatchHint);
        Assert.AreEqual("Carrot", preview[0].FoodName);
    }

    [TestMethod]
    public void PreviewAddToCart_GramsMode_MergesIntoExistingWeightLine()
    {
        GroceryListItem existing = MakeListItem("1 Carrot");
        IngredientCartService cart = CreateCart(
            foods: new Dictionary<string, Food>
            {
                { "Carrot", MakeFood(3, "Carrot", "Medium Carrot", 61) },
                { "carrots", MakeFood(3, "Carrot", "Medium Carrot", 61) },
            },
            existingItems: [existing],
            mode: GroceryUnitNormalization.Grams);

        List<CartPreviewData> preview = cart.PreviewAddToCart(["2 carrots"]);

        Assert.AreEqual(1, preview.Count);
        Assert.IsTrue(preview[0].IsMerge);
        Assert.AreEqual("183g Carrot", preview[0].DisplayName);
        Assert.AreEqual("(~3 medium carrot)", preview[0].FoodMatchHint);
    }

    [TestMethod]
    public void AddToCart_GramsMode_AddsNormalizedItemWithCountHint()
    {
        var listService = new Mock<IGroceryListService>();
        IngredientCartService cart = CreateCart(
            foods: new Dictionary<string, Food> { { "onion", MakeFood(7, "Onion", "Medium Onion", 110) } },
            mode: GroceryUnitNormalization.Grams,
            listService: listService);

        cart.AddToCart(["1 onion"]);

        listService.Verify(s => s.AddMultiple(It.Is<List<GroceryListItem>>(items =>
            items.Count == 1
            && items[0].Name == "110g Onion"
            && items[0].QuantityHint == "(~1 medium onion)")), Times.Once);
    }

    [TestMethod]
    public void AddToCart_OffMode_AddsOriginalLine()
    {
        var listService = new Mock<IGroceryListService>();
        IngredientCartService cart = CreateCart(
            foods: new Dictionary<string, Food> { { "onion", MakeFood(7, "Onion", "Medium Onion", 110) } },
            listService: listService);

        cart.AddToCart(["1 onion"]);

        listService.Verify(s => s.AddMultiple(It.Is<List<GroceryListItem>>(items =>
            items.Count == 1
            && items[0].Name == "1 Onion"
            && items[0].QuantityHint == "(~110g)")), Times.Once);
    }

    private static IngredientCartService CreateCart(
        Dictionary<string, Food>? foods = null,
        List<GroceryListItem>? existingItems = null,
        GroceryUnitNormalization mode = GroceryUnitNormalization.Off,
        Mock<IGroceryListService>? listService = null)
    {
        var foodService = new Mock<IFoodService>();
        foodService.Setup(s => s.FindBestMatch(It.IsAny<string>()))
            .Returns(new FoodMatchResult { Score = 0, Method = "None" });
        if (foods is not null)
        {
            foreach (KeyValuePair<string, Food> kvp in foods)
            {
                foodService.Setup(s => s.FindBestMatch(kvp.Key))
                    .Returns(new FoodMatchResult { Food = kvp.Value, Score = 1.0, Method = "Exact" });
            }
        }

        var parser = new IngredientParserService(foodService.Object);
        listService ??= new Mock<IGroceryListService>();
        listService.Setup(s => s.GetAll()).Returns(existingItems ?? []);
        var macroService = new Mock<IMacroTargetService>();
        macroService.Setup(s => s.GetSettings())
            .Returns(new MacroTargetSettings { GroceryUnitNormalization = mode });
        return new IngredientCartService(parser, listService.Object, macroService.Object);
    }

    private static Food MakeFood(int id, string name, string measure, double gramsPerMeasure) => new()
    {
        Id = id,
        Name = name,
        Measure = measure,
        GramsPerMeasure = gramsPerMeasure,
        CaloriesPer100g = 50,
    };

    private static GroceryListItem MakeListItem(string name) => new()
    {
        Id = Guid.NewGuid().ToString(),
        Name = name,
        IsChecked = false,
    };
}
