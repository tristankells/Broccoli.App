using Broccoli.Avalonia.Slices.Recipes;

namespace Broccoli.Avalonia.Tests.Slices.Recipes;

[TestClass]
public class TextDiffTests
{
    [TestMethod]
    public void Diff_IdenticalText_ReturnsOnlyContext()
    {
        List<DiffLine> result = TextDiff.Diff("line one\nline two", "line one\nline two");

        Assert.AreEqual(2, result.Count);
        Assert.IsTrue(result.All(line => line.Type == DiffLineType.Context));
    }

    [TestMethod]
    public void Diff_AddedLine_ReturnsAddedLine()
    {
        List<DiffLine> result = TextDiff.Diff("a\nb", "a\nb\nc");

        Assert.IsTrue(result.Any(line => line.Type == DiffLineType.Added && line.Text == "c"));
        Assert.IsFalse(result.Any(line => line.Type == DiffLineType.Removed));
    }

    [TestMethod]
    public void Diff_RemovedLine_ReturnsRemovedLine()
    {
        List<DiffLine> result = TextDiff.Diff("a\nb\nc", "a\nb");

        Assert.IsTrue(result.Any(line => line.Type == DiffLineType.Removed && line.Text == "c"));
        Assert.IsFalse(result.Any(line => line.Type == DiffLineType.Added));
    }

    [TestMethod]
    public void Diff_ChangedLine_ReturnsRemovedThenAdded()
    {
        List<DiffLine> result = TextDiff.Diff("a\nb", "a\nc");

        Assert.AreEqual(3, result.Count);
        Assert.AreEqual(DiffLineType.Context, result[0].Type);
        Assert.AreEqual(DiffLineType.Removed, result[1].Type);
        Assert.AreEqual("b", result[1].Text);
        Assert.AreEqual(DiffLineType.Added, result[2].Type);
        Assert.AreEqual("c", result[2].Text);
    }

    [TestMethod]
    public void Diff_EmptyOld_ReturnsAllAdded()
    {
        List<DiffLine> result = TextDiff.Diff(string.Empty, "a\nb");

        Assert.AreEqual(2, result.Count);
        Assert.IsTrue(result.All(line => line.Type == DiffLineType.Added));
    }

    [TestMethod]
    public void Diff_EmptyNew_ReturnsAllRemoved()
    {
        List<DiffLine> result = TextDiff.Diff("a\nb", string.Empty);

        Assert.AreEqual(2, result.Count);
        Assert.IsTrue(result.All(line => line.Type == DiffLineType.Removed));
    }
}
