using Broccoli.Avalonia.IngredientParsing;
using Broccoli.Avalonia.Models;
using Broccoli.Avalonia.Slices.Recipes;
using Moq;

namespace Broccoli.Avalonia.Tests.Slices.Recipes;

[TestClass]
public class RecipeIngredientSearchServiceTests
{
    [TestMethod]
    public void Search_ExactMatch_ReturnsRecipeWithHit()
    {
        Recipe recipe = MakeRecipe("Chicken Curry", "500g chicken breast", "2 tomatoes");
        TestHarness harness = CreateHarness(
            [recipe],
            scores: [("chicken", "chicken breast", 1.0)]);

        IReadOnlyList<RecipeIngredientSearchResult> results = harness.Service.Search(["chicken"]);

        Assert.HasCount(1, results);
        RecipeIngredientSearchResult result = results[0];
        Assert.AreEqual("Chicken Curry", result.Recipe.Name);
        Assert.AreEqual(1, result.MatchCount);
        Assert.AreEqual(1, result.TotalTerms);
        Assert.AreEqual(100.0, result.AverageMatchPercent, 0.001);
        Assert.AreEqual("chicken", result.MatchedIngredients[0].SearchTerm);
        Assert.AreEqual("500g chicken breast", result.MatchedIngredients[0].IngredientLine);
        Assert.AreEqual(100.0, result.MatchedIngredients[0].ScorePercent, 0.001);
    }

    [TestMethod]
    public void Search_HitBelowThreshold_ExcludesRecipe()
    {
        Recipe recipe = MakeRecipe("Chicken Curry", "chicken breast");
        TestHarness harness = CreateHarness(
            [recipe],
            scores: [("chicken", "chicken breast", 0.4)]);

        IReadOnlyList<RecipeIngredientSearchResult> results = harness.Service.Search(["chicken"]);

        Assert.IsEmpty(results);
    }

    [TestMethod]
    public void Search_OrdersByMatchScore_MoreTermsFirst()
    {
        Recipe usesBoth = MakeRecipe("Chicken Tomato Salad", "chicken breast", "tomato");
        Recipe usesOne = MakeRecipe("Chicken Soup", "chicken breast");
        TestHarness harness = CreateHarness(
            [usesBoth, usesOne],
            scores:
            [
                ("chicken", "chicken breast", 1.0),
                ("tomato", "tomato", 1.0),
            ]);

        IReadOnlyList<RecipeIngredientSearchResult> results = harness.Service.Search(["chicken", "tomato"]);

        Assert.HasCount(2, results);
        Assert.AreEqual("Chicken Tomato Salad", results[0].Recipe.Name);
        Assert.AreEqual("Chicken Soup", results[1].Recipe.Name);
        Assert.IsTrue(results[0].MatchScore > results[1].MatchScore);
    }

    [TestMethod]
    public void Search_EmptyTerms_ReturnsEmpty()
    {
        Recipe recipe = MakeRecipe("Chicken Curry", "chicken breast");
        TestHarness harness = CreateHarness([recipe]);

        Assert.IsEmpty(harness.Service.Search([]));
        Assert.IsEmpty(harness.Service.Search([""]));
        Assert.IsEmpty(harness.Service.Search(["   "]));
    }

    [TestMethod]
    public void Search_NoMatches_ReturnsEmpty()
    {
        Recipe recipe = MakeRecipe("Chicken Curry", "chicken breast");
        TestHarness harness = CreateHarness([recipe]);

        IReadOnlyList<RecipeIngredientSearchResult> results = harness.Service.Search(["beef"]);

        Assert.IsEmpty(results);
    }

    [TestMethod]
    public void Search_DeduplicatesTermsIgnoringCase()
    {
        Recipe recipe = MakeRecipe("Chicken Curry", "chicken breast");
        TestHarness harness = CreateHarness(
            [recipe],
            scores: [("chicken", "chicken breast", 1.0)]);

        IReadOnlyList<RecipeIngredientSearchResult> results = harness.Service.Search(["chicken", "CHICKEN", "Chicken"]);

        Assert.HasCount(1, results);
        Assert.AreEqual(1, results[0].TotalTerms);
        Assert.AreEqual(1, results[0].MatchCount);
    }

    [TestMethod]
    public void Search_UsesCanonicalFoodName_WhenDescriptionScoresBelowThreshold()
    {
        Recipe recipe = MakeRecipe("Chicken Curry", "500g chicken breast");
        TestHarness harness = CreateHarness(
            [recipe],
            foods: [("chicken breast", "Chicken, breast, raw")],
            scores:
            [
                // The raw description only loosely matches the search term...
                ("chicken", "chicken breast", 0.3),
                // ...but the canonical food name resolves it.
                ("chicken", "chicken, breast, raw", 0.9),
            ]);

        IReadOnlyList<RecipeIngredientSearchResult> results = harness.Service.Search(["chicken"]);

        Assert.HasCount(1, results);
        Assert.AreEqual("Chicken, breast, raw", results[0].MatchedIngredients[0].MatchedFoodName);
        Assert.AreEqual(90.0, results[0].MatchedIngredients[0].ScorePercent, 0.001);
    }

    [TestMethod]
    public void Search_RecipeWithNoIngredients_IsExcluded()
    {
        Recipe recipe = MakeRecipe("Empty");
        TestHarness harness = CreateHarness([recipe]);

        Assert.IsEmpty(harness.Service.Search(["chicken"]));
    }

    private static TestHarness CreateHarness(
        Recipe[] recipes,
        List<(string Term, string Candidate, double Score)>? scores = null,
        List<(string Description, string FoodName)>? foods = null)
    {
        var recipeService = new Mock<IRecipeService>();
        recipeService.Setup(s => s.GetAll()).Returns(recipes);

        Dictionary<string, Food> foodByName = (foods ?? [])
            .ToDictionary(
                pair => pair.Description.ToLowerInvariant(),
                pair => new Food { Name = pair.FoodName });

        var foodService = new Mock<IFoodService>();
        foodService.Setup(f => f.FindBestMatch(It.IsAny<string>()))
            .Returns((string description) =>
                foodByName.TryGetValue(description.ToLowerInvariant(), out Food? food)
                    ? new FoodMatchResult { Food = food, Score = 1.0, Method = "Exact" }
                    : new FoodMatchResult { Score = 0, Method = "None" });

        Dictionary<(string Term, string Candidate), double> scoreLookup = (scores ?? [])
            .ToDictionary(
                score => (score.Term.ToLowerInvariant(), score.Candidate.ToLowerInvariant()),
                score => score.Score);

        foodService.Setup(f => f.ScoreMatch(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string term, string candidate) =>
                scoreLookup.TryGetValue((term.ToLowerInvariant(), candidate.ToLowerInvariant()), out double score)
                    ? new FoodMatchResult { Score = score, Method = "Fuzzy" }
                    : new FoodMatchResult { Score = 0, Method = "None" });

        var service = new RecipeIngredientSearchService(recipeService.Object, new IngredientParserService(foodService.Object), foodService.Object);
        return new TestHarness(service);
    }

    private static Recipe MakeRecipe(string name, params string[] ingredientLines) =>
        new() { Name = name, Ingredients = string.Join('\n', ingredientLines) };

    private sealed record TestHarness(IRecipeIngredientSearchService Service);
}
