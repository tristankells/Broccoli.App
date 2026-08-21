namespace Broccoli.Avalonia.Slices.Recipes;

/// <summary>A single line in a text diff, carrying display metadata for a git-style view.</summary>
public class DiffLine
{
    /// <summary>Whether this line is context, added, or removed.</summary>
    public DiffLineType Type { get; init; }

    /// <summary>The line's content.</summary>
    public string Text { get; init; } = string.Empty;

    /// <summary>The one-character prefix shown to the left of the line (+/-/space).</summary>
    public string Prefix => Type switch
    {
        DiffLineType.Added => "+",
        DiffLineType.Removed => "-",
        _ => " ",
    };

    /// <summary>True when this line would be added by restoring to the older version.</summary>
    public bool IsAdded => Type == DiffLineType.Added;

    /// <summary>True when this line would be removed by restoring to the older version.</summary>
    public bool IsRemoved => Type == DiffLineType.Removed;
}
