namespace Broccoli.Avalonia.Slices.Recipes;

/// <summary>
/// Computes a line-level diff between two blocks of text using a longest-common-subsequence
/// algorithm, producing git-style <see cref="DiffLine"/> entries (old → new).
/// </summary>
public static class TextDiff
{
    /// <summary>
    /// Produces the diff lines describing the change from <paramref name="oldText"/> to
    /// <paramref name="newText"/>. Lines only in the old text are <see cref="DiffLineType.Removed"/>;
    /// lines only in the new text are <see cref="DiffLineType.Added"/>.
    /// </summary>
    public static List<DiffLine> Diff(string oldText, string newText)
    {
        string[] oldLines = SplitLines(oldText);
        string[] newLines = SplitLines(newText);
        int oldCount = oldLines.Length;
        int newCount = newLines.Length;

        int[,] lcs = new int[oldCount + 1, newCount + 1];
        for (int i = oldCount - 1; i >= 0; i--)
        {
            for (int j = newCount - 1; j >= 0; j--)
            {
                lcs[i, j] = oldLines[i] == newLines[j]
                    ? lcs[i + 1, j + 1] + 1
                    : Math.Max(lcs[i + 1, j], lcs[i, j + 1]);
            }
        }

        List<DiffLine> result = new List<DiffLine>();
        int oldIndex = 0;
        int newIndex = 0;
        while (oldIndex < oldCount && newIndex < newCount)
        {
            if (oldLines[oldIndex] == newLines[newIndex])
            {
                result.Add(new DiffLine { Type = DiffLineType.Context, Text = oldLines[oldIndex] });
                oldIndex++;
                newIndex++;
            }
            else if (lcs[oldIndex + 1, newIndex] >= lcs[oldIndex, newIndex + 1])
            {
                result.Add(new DiffLine { Type = DiffLineType.Removed, Text = oldLines[oldIndex] });
                oldIndex++;
            }
            else
            {
                result.Add(new DiffLine { Type = DiffLineType.Added, Text = newLines[newIndex] });
                newIndex++;
            }
        }

        while (oldIndex < oldCount)
        {
            result.Add(new DiffLine { Type = DiffLineType.Removed, Text = oldLines[oldIndex] });
            oldIndex++;
        }

        while (newIndex < newCount)
        {
            result.Add(new DiffLine { Type = DiffLineType.Added, Text = newLines[newIndex] });
            newIndex++;
        }

        return result;
    }

    private static string[] SplitLines(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return Array.Empty<string>();
        }

        return text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
    }
}
