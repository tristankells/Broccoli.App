using Broccoli.Avalonia.Models;
using Broccoli.Avalonia.Storage;

namespace Broccoli.Avalonia.Tests.Slices.Recipes;

[TestClass]
public class RecipeHistoryStoreTests
{
    private string _tempRoot = string.Empty;

    [TestInitialize]
    public void Initialize()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "Broccoli.Tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempRoot);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_tempRoot))
        {
            Directory.Delete(_tempRoot, recursive: true);
        }
    }

    [TestMethod]
    public void Save_WritesSnapshot_AndListReturnsIt()
    {
        RecipeHistoryStore store = new RecipeHistoryStore(_tempRoot);
        store.Save(Snapshot("r1", "s1", new DateTime(2026, 8, 1, 12, 0, 0, DateTimeKind.Utc), "100g chicken"), 10);

        IReadOnlyList<RecipeSnapshot> result = store.List("r1");

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("s1", result[0].Id);
        Assert.AreEqual("100g chicken", result[0].Ingredients);
    }

    [TestMethod]
    public void List_ReturnsNewestFirst()
    {
        RecipeHistoryStore store = new RecipeHistoryStore(_tempRoot);
        store.Save(Snapshot("r1", "s1", new DateTime(2026, 8, 1, 12, 0, 0, DateTimeKind.Utc), "v1"), 10);
        store.Save(Snapshot("r1", "s2", new DateTime(2026, 8, 2, 12, 0, 0, DateTimeKind.Utc), "v2"), 10);
        store.Save(Snapshot("r1", "s3", new DateTime(2026, 8, 3, 12, 0, 0, DateTimeKind.Utc), "v3"), 10);

        IReadOnlyList<RecipeSnapshot> result = store.List("r1");

        Assert.AreEqual(3, result.Count);
        Assert.AreEqual("s3", result[0].Id);
        Assert.AreEqual("s2", result[1].Id);
        Assert.AreEqual("s1", result[2].Id);
    }

    [TestMethod]
    public void Save_PrunesOldSnapshots_ButKeepsFirst()
    {
        RecipeHistoryStore store = new RecipeHistoryStore(_tempRoot);
        for (int i = 1; i <= 12; i++)
        {
            store.Save(
                Snapshot("r1", $"s{i}", new DateTime(2026, 8, i, 12, 0, 0, DateTimeKind.Utc), $"v{i}"),
                maxBackups: 10);
        }

        IReadOnlyList<RecipeSnapshot> result = store.List("r1");

        Assert.AreEqual(10, result.Count);
        // The original (first) version is always kept.
        Assert.IsTrue(result.Any(s => s.Id == "s1"));
        // The two oldest non-original snapshots (s2..s3) were pruned.
        Assert.IsFalse(result.Any(s => s.Id == "s2"));
        Assert.IsFalse(result.Any(s => s.Id == "s3"));
        // The 9 most recent versions are kept alongside the original.
        Assert.IsTrue(result.Any(s => s.Id == "s4"));
        Assert.IsTrue(result.Any(s => s.Id == "s12"));
    }

    [TestMethod]
    public void Save_MaxBackupsOfOne_KeepsOnlyFirst()
    {
        RecipeHistoryStore store = new RecipeHistoryStore(_tempRoot);
        store.Save(Snapshot("r1", "s1", new DateTime(2026, 8, 1, 12, 0, 0, DateTimeKind.Utc), "v1"), 1);
        store.Save(Snapshot("r1", "s2", new DateTime(2026, 8, 2, 12, 0, 0, DateTimeKind.Utc), "v2"), 1);

        IReadOnlyList<RecipeSnapshot> result = store.List("r1");

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("s1", result[0].Id);
    }

    [TestMethod]
    public void Get_ReturnsSnapshotById()
    {
        RecipeHistoryStore store = new RecipeHistoryStore(_tempRoot);
        store.Save(Snapshot("r1", "s1", new DateTime(2026, 8, 1, 12, 0, 0, DateTimeKind.Utc), "v1"), 10);

        RecipeSnapshot? result = store.Get("r1", "s1");

        Assert.IsNotNull(result);
        Assert.AreEqual("v1", result!.Ingredients);
    }

    [TestMethod]
    public void DeleteAll_RemovesHistory()
    {
        RecipeHistoryStore store = new RecipeHistoryStore(_tempRoot);
        store.Save(Snapshot("r1", "s1", new DateTime(2026, 8, 1, 12, 0, 0, DateTimeKind.Utc), "v1"), 10);

        store.DeleteAll("r1");

        Assert.AreEqual(0, store.List("r1").Count);
    }

    private static RecipeSnapshot Snapshot(string recipeId, string id, DateTime capturedAtUtc, string ingredients) => new()
    {
        Id = id,
        RecipeId = recipeId,
        CapturedAtUtc = capturedAtUtc,
        Name = "Test",
        Ingredients = ingredients,
        Directions = "steps",
    };
}
