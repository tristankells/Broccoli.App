using Broccoli.Avalonia.Models;
using Broccoli.Avalonia.Slices.Planning;
using Broccoli.Avalonia.Slices.Recipes;
using Broccoli.Avalonia.Storage;
using Moq;

namespace Broccoli.Avalonia.Tests.Slices.Recipes;

[TestClass]
public class RecipeServiceHistoryTests
{
    private readonly Mock<IRecipeMarkdownStore> _store = new();
    private readonly Mock<IRecipeHistoryStore> _historyStore = new();
    private readonly Mock<IMacroTargetService> _macroService = new();

    private const string RecipeId = "r1";

    public RecipeServiceHistoryTests()
    {
        _macroService.Setup(s => s.GetSettings())
            .Returns(new MacroTargetSettings { RecipeHistoryBackupCount = 10 });
    }

    [TestMethod]
    public void Update_CapturesPreviousVersion_WhenContentChanged()
    {
        _store.Setup(s => s.Load(RecipeId)).Returns(new Recipe
        {
            Id = RecipeId,
            Name = "Test",
            Ingredients = "old ingredient",
            Directions = "old steps",
        });
        RecipeService service = CreateService();

        service.Update(new Recipe
        {
            Id = RecipeId,
            Name = "Test",
            Ingredients = "new ingredient",
            Directions = "old steps",
        });

        _historyStore.Verify(
            h => h.Save(It.Is<RecipeSnapshot>(s => s.Ingredients == "old ingredient"), It.IsAny<int>()),
            Times.Once);
    }

    [TestMethod]
    public void Update_SkipsSnapshot_WhenNoContentChanged()
    {
        _store.Setup(s => s.Load(RecipeId)).Returns(new Recipe
        {
            Id = RecipeId,
            Name = "Test",
            Ingredients = "same ingredient",
        });
        RecipeService service = CreateService();

        service.Update(new Recipe
        {
            Id = RecipeId,
            Name = "Test",
            Ingredients = "same ingredient",
        });

        _historyStore.Verify(h => h.Save(It.IsAny<RecipeSnapshot>(), It.IsAny<int>()), Times.Never);
    }

    [TestMethod]
    public void Update_SkipsSnapshot_WhenOnlyImagesChanged()
    {
        _store.Setup(s => s.Load(RecipeId)).Returns(new Recipe
        {
            Id = RecipeId,
            Name = "Test",
            Ingredients = "same ingredient",
            Images = new List<string>(),
        });
        RecipeService service = CreateService();

        service.Update(new Recipe
        {
            Id = RecipeId,
            Name = "Test",
            Ingredients = "same ingredient",
            Images = new List<string> { "photo.jpg" },
        });

        _historyStore.Verify(h => h.Save(It.IsAny<RecipeSnapshot>(), It.IsAny<int>()), Times.Never);
    }

    [TestMethod]
    public void Update_PassesConfiguredBackupCount()
    {
        _macroService.Setup(s => s.GetSettings())
            .Returns(new MacroTargetSettings { RecipeHistoryBackupCount = 5 });
        _store.Setup(s => s.Load(RecipeId)).Returns(new Recipe
        {
            Id = RecipeId,
            Name = "Test",
            Ingredients = "old",
        });
        RecipeService service = CreateService();

        service.Update(new Recipe { Id = RecipeId, Name = "Test", Ingredients = "new" });

        _historyStore.Verify(h => h.Save(It.IsAny<RecipeSnapshot>(), 5), Times.Once);
    }

    [TestMethod]
    public void Restore_CopiesSnapshotContent_AndSnapshotsCurrent()
    {
        _historyStore.Setup(h => h.Get(RecipeId, "s1")).Returns(new RecipeSnapshot
        {
            Id = "s1",
            RecipeId = RecipeId,
            Name = "Test",
            Ingredients = "original ingredient",
            Directions = "original steps",
        });
        _store.Setup(s => s.Load(RecipeId)).Returns(() => new Recipe
        {
            Id = RecipeId,
            Name = "Test",
            Ingredients = "current ingredient",
            Directions = "current steps",
        });
        RecipeService service = CreateService();

        Recipe? restored = service.Restore(RecipeId, "s1");

        Assert.IsNotNull(restored);
        Assert.AreEqual("original ingredient", restored!.Ingredients);
        Assert.AreEqual("original steps", restored.Directions);
        // Restore snapshots the current version first so it can be undone.
        _historyStore.Verify(
            h => h.Save(It.Is<RecipeSnapshot>(s => s.Ingredients == "current ingredient"), It.IsAny<int>()),
            Times.Once);
    }

    [TestMethod]
    public void Restore_ReturnsNull_WhenSnapshotMissing()
    {
        _historyStore.Setup(h => h.Get(RecipeId, "missing")).Returns((RecipeSnapshot?)null);
        _store.Setup(s => s.Load(RecipeId)).Returns(new Recipe { Id = RecipeId, Name = "Test" });
        RecipeService service = CreateService();

        Recipe? restored = service.Restore(RecipeId, "missing");

        Assert.IsNull(restored);
    }

    private RecipeService CreateService() =>
        new(_store.Object, _historyStore.Object, _macroService.Object);
}
