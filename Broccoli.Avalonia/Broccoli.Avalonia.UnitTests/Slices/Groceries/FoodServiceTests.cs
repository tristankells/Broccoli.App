using Broccoli.Avalonia.IngredientParsing;
using Broccoli.Avalonia.Models;
using Broccoli.Avalonia.Storage;
using Microsoft.EntityFrameworkCore;

namespace Broccoli.Avalonia.Tests.Slices.Groceries;

[TestClass]
public class FoodServiceTests
{
    [TestMethod]
    public void FindMatches_ReturnsMultipleRankedCandidates()
    {
        FoodService service = CreateService();
        service.Add(new Food { Name = "chicken breast" });
        service.Add(new Food { Name = "chicken thigh" });
        service.Add(new Food { Name = "chicken mince" });

        IReadOnlyList<FoodMatchResult> results = service.FindMatches("chicken", 10);

        Assert.IsTrue(results.Count >= 2);
        Assert.IsTrue(results.All(r => r.Food is not null && r.Score > 0));
    }

    [TestMethod]
    public void FindMatches_ExactQuery_ReturnsOnlyExactMatch()
    {
        FoodService service = CreateService();
        service.Add(new Food { Name = "chicken breast" });
        service.Add(new Food { Name = "chicken thigh" });

        IReadOnlyList<FoodMatchResult> results = service.FindMatches("chicken breast", 10);

        Assert.AreEqual(1, results.Count);
        Assert.AreEqual("chicken breast", results[0].Food!.Name);
        Assert.AreEqual("Exact", results[0].Method);
    }

    [TestMethod]
    public void Add_PersistsAcrossServiceInstances_AndMarksCustom()
    {
        string dbPath = Path.Combine(Path.GetTempPath(), $"broccoli-test-{Guid.NewGuid():N}.db");
        DbContextOptions<BroccoliDbContext> options = new DbContextOptionsBuilder<BroccoliDbContext>()
            .UseSqlite($"Data Source={dbPath}")
            .Options;
        using (var context = new BroccoliDbContext(options))
        {
            context.Database.EnsureCreated();
        }

        var first = new FoodService(() => new BroccoliDbContext(options));
        Food added = first.Add(new Food { Name = "test food", Measure = "100g", GramsPerMeasure = 100 });
        Assert.IsTrue(added.IsCustom);

        var second = new FoodService(() => new BroccoliDbContext(options));
        Assert.IsTrue(second.TryGetFood("test food", out Food reloaded));
        Assert.IsTrue(reloaded.IsCustom);
    }

    [TestMethod]
    public void Build_AttachesMassUnitsAndSpacesOthers()
    {
        Assert.AreEqual("250g Chicken Thigh", IngredientLineFormatter.Build(250, "g", "Chicken Thigh"));
        Assert.AreEqual("1 drizzle Olive Oil", IngredientLineFormatter.Build(1, "drizzle", "Olive Oil"));
        Assert.AreEqual("2 Chicken Breast", IngredientLineFormatter.Build(2, string.Empty, "Chicken Breast"));
        Assert.AreEqual("1.5 cup Flour", IngredientLineFormatter.Build(1.5, "cup", "Flour"));
    }

    [TestMethod]
    public void ScoreMatch_ExactName_ReturnsExactMatch()
    {
        FoodService service = CreateService();

        FoodMatchResult result = service.ScoreMatch("chicken breast", "Chicken Breast");

        Assert.AreEqual("Exact", result.Method);
        Assert.AreEqual(1.0, result.Score);
    }

    [TestMethod]
    public void ScoreMatch_NearMiss_ReturnsPositiveScore()
    {
        FoodService service = CreateService();

        FoodMatchResult result = service.ScoreMatch("chicken breast", "Chicken breast fillet");

        Assert.AreNotEqual("Exact", result.Method);
        Assert.IsTrue(result.Score > 0);
    }

    [TestMethod]
    public void ScoreMatch_EmptyInput_ReturnsNone()
    {
        FoodService service = CreateService();

        FoodMatchResult result = service.ScoreMatch("chicken breast", string.Empty);

        Assert.AreEqual("None", result.Method);
        Assert.AreEqual(0.0, result.Score);
    }

    private static FoodService CreateService()
    {
        string dbPath = Path.Combine(Path.GetTempPath(), $"broccoli-test-{Guid.NewGuid():N}.db");
        DbContextOptions<BroccoliDbContext> options = new DbContextOptionsBuilder<BroccoliDbContext>()
            .UseSqlite($"Data Source={dbPath}")
            .Options;
        using (var context = new BroccoliDbContext(options))
        {
            context.Database.EnsureCreated();
        }

        return new FoodService(() => new BroccoliDbContext(options));
    }
}
