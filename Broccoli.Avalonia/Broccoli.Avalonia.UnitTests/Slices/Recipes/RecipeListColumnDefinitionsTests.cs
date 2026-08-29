using Broccoli.Avalonia.Slices.Recipes;

namespace Broccoli.Avalonia.Tests.Slices.Recipes;

[TestClass]
public class RecipeListColumnDefinitionsTests
{
    [TestMethod]
    public void Parse_EmptyOrNull_ReturnsDefaultOrder()
    {
        RecipeListColumn[] fromNull = RecipeListColumnDefinitions.Parse(null);
        RecipeListColumn[] fromEmpty = RecipeListColumnDefinitions.Parse(string.Empty);

        CollectionAssert.AreEqual(RecipeListColumnDefinitions.DefaultOrder, fromNull);
        CollectionAssert.AreEqual(RecipeListColumnDefinitions.DefaultOrder, fromEmpty);
    }

    [TestMethod]
    public void Parse_RoundTripsSerialize()
    {
        RecipeListColumn[] columns =
        [
            RecipeListColumn.Name,
            RecipeListColumn.Calories,
            RecipeListColumn.DateAdded,
        ];

        RecipeListColumn[] parsed = RecipeListColumnDefinitions.Parse(RecipeListColumnDefinitions.Serialize(columns));

        CollectionAssert.AreEqual(columns, parsed);
    }

    [TestMethod]
    public void Parse_IgnoresUnknownNames_AndKeepsValidOrder()
    {
        RecipeListColumn[] parsed = RecipeListColumnDefinitions.Parse("Name,Bogus,Fat,DateAdded");

        CollectionAssert.AreEqual(
            new[] { RecipeListColumn.Name, RecipeListColumn.Fat, RecipeListColumn.DateAdded },
            parsed);
    }

    [TestMethod]
    public void Parse_AllInvalid_FallsBackToDefaultOrder()
    {
        RecipeListColumn[] parsed = RecipeListColumnDefinitions.Parse("Bogus1,Bogus2");

        CollectionAssert.AreEqual(RecipeListColumnDefinitions.DefaultOrder, parsed);
    }

    [TestMethod]
    public void Serialize_JoinsEnumNamesInOrder()
    {
        string serialized = RecipeListColumnDefinitions.Serialize(
            [RecipeListColumn.Fat, RecipeListColumn.Name]);

        Assert.AreEqual("Fat,Name", serialized);
    }
}
