using Broccoli.Avalonia.Models;
using Broccoli.Avalonia.Seasonality;

namespace Broccoli.Avalonia.Tests.Seasonality;

[TestClass]
public class SeasonalityFileSerializerTests
{
    [TestMethod]
    public void Serialize_ThenParse_RoundTrips()
    {
        var items = new List<ProduceItem>
        {
            new()
            {
                Id = "feijoa",
                Name = "Feijoa",
                Type = "fruit",
                Notes = "Test note",
                Months = new Dictionary<int, SeasonalityState>
                {
                    [1] = SeasonalityState.InSeason,
                    [2] = SeasonalityState.PartiallyInSeason,
                    [3] = SeasonalityState.OutOfSeason,
                    [12] = SeasonalityState.InSeason,
                },
            },
        };

        string json = SeasonalityFileSerializer.Serialize(items);
        bool ok = SeasonalityFileSerializer.TryParse(json, out List<ProduceItem> parsed, out List<string> errors);

        Assert.IsTrue(ok, string.Join("; ", errors));
        Assert.HasCount(1, parsed);
        Assert.AreEqual("Feijoa", parsed[0].Name);
        Assert.AreEqual("fruit", parsed[0].Type);
        Assert.AreEqual("Test note", parsed[0].Notes);
        Assert.AreEqual(SeasonalityState.InSeason, parsed[0].GetStateForMonth(1));
        Assert.AreEqual(SeasonalityState.PartiallyInSeason, parsed[0].GetStateForMonth(2));
        Assert.AreEqual(SeasonalityState.OutOfSeason, parsed[0].GetStateForMonth(3));
        Assert.AreEqual(SeasonalityState.InSeason, parsed[0].GetStateForMonth(12));
        // All 12 months are written out explicitly.
        Assert.AreEqual(SeasonalityState.OutOfSeason, parsed[0].GetStateForMonth(7));
    }

    [TestMethod]
    public void Parse_SeedFormatJson()
    {
        const string json = """
        {
          "meta": { "region": "NZ" },
          "produce": [
            { "id": "apple", "name": "Apple", "type": "fruit",
              "months": { "jan": "in", "feb": "in", "mar": "partial", "apr": "out" } }
          ]
        }
        """;

        bool ok = SeasonalityFileSerializer.TryParse(json, out List<ProduceItem> items, out List<string> errors);

        Assert.IsTrue(ok, string.Join("; ", errors));
        Assert.HasCount(1, items);
        Assert.AreEqual(SeasonalityState.InSeason, items[0].GetStateForMonth(1));
        Assert.AreEqual(SeasonalityState.PartiallyInSeason, items[0].GetStateForMonth(3));
        Assert.AreEqual(SeasonalityState.OutOfSeason, items[0].GetStateForMonth(4));
    }

    [TestMethod]
    public void Parse_UnknownMonth_SkipsItemAndReportsError()
    {
        const string json = """
        { "produce": [
            { "id": "a", "name": "Good", "type": "fruit", "months": { "jan": "in" } },
            { "id": "b", "name": "Bad", "type": "fruit", "months": { "febr": "in" } }
        ] }
        """;

        bool ok = SeasonalityFileSerializer.TryParse(json, out List<ProduceItem> items, out List<string> errors);

        Assert.IsTrue(ok);
        Assert.HasCount(1, items);
        Assert.AreEqual("Good", items[0].Name);
        Assert.HasCount(1, errors);
        StringAssert.Contains(errors[0], "Bad");
        StringAssert.Contains(errors[0], "febr");
    }

    [TestMethod]
    public void Parse_InvalidState_SkipsItemAndReportsError()
    {
        const string json = """
        { "produce": [
            { "id": "a", "name": "Good", "type": "fruit", "months": { "jan": "in" } },
            { "id": "b", "name": "Bad", "type": "fruit", "months": { "jan": "in_seasonn" } }
        ] }
        """;

        bool ok = SeasonalityFileSerializer.TryParse(json, out List<ProduceItem> items, out List<string> errors);

        Assert.IsTrue(ok);
        Assert.HasCount(1, items);
        StringAssert.Contains(errors[0], "in_seasonn");
    }

    [TestMethod]
    public void Parse_DuplicateId_SkipsDuplicate()
    {
        const string json = """
        { "produce": [
            { "id": "a", "name": "First", "type": "fruit", "months": { "jan": "in" } },
            { "id": "a", "name": "Second", "type": "fruit", "months": { "jan": "out" } }
        ] }
        """;

        bool ok = SeasonalityFileSerializer.TryParse(json, out List<ProduceItem> items, out List<string> errors);

        Assert.IsTrue(ok);
        Assert.HasCount(1, items);
        Assert.AreEqual("First", items[0].Name);
        StringAssert.Contains(errors[0], "duplicate");
    }

    [TestMethod]
    public void Parse_MissingRequiredField_SkipsItem()
    {
        const string json = """
        { "produce": [ { "id": "a", "type": "fruit", "months": { "jan": "in" } } ] }
        """;

        bool ok = SeasonalityFileSerializer.TryParse(json, out List<ProduceItem> items, out List<string> errors);

        Assert.IsTrue(ok);
        Assert.HasCount(0, items);
        Assert.HasCount(1, errors);
        StringAssert.Contains(errors[0], "name");
    }

    [TestMethod]
    public void Parse_InvalidJson_ReturnsError()
    {
        bool ok = SeasonalityFileSerializer.TryParse("{ not json", out List<ProduceItem> items, out List<string> errors);

        Assert.IsFalse(ok);
        Assert.HasCount(0, items);
        Assert.HasCount(1, errors);
        StringAssert.Contains(errors[0], "JSON");
    }

    [TestMethod]
    public void Parse_EmptyProduce_ReturnsError()
    {
        bool ok = SeasonalityFileSerializer.TryParse("{ \"produce\": [] }", out List<ProduceItem> items, out List<string> errors);

        Assert.IsFalse(ok);
        Assert.HasCount(0, items);
        StringAssert.Contains(errors[0], "no produce");
    }
}
