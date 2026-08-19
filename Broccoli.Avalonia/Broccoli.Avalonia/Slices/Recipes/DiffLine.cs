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

    /// <summary>Foreground colour for the line, keyed off its type.</summary>
    public string Foreground => Type switch
    {
        DiffLineType.Added => "#1E7E34",
        DiffLineType.Removed => "#C0392B",
        _ => "#2C2C2C",
    };

    /// <summary>Background tint for the line, keyed off its type.</summary>
    public string Background => Type switch
    {
        DiffLineType.Added => "#E8F8EE",
        DiffLineType.Removed => "#FDECEA",
        _ => "Transparent",
    };
}
