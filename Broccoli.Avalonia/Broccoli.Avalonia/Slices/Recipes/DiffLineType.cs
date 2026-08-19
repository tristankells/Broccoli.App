namespace Broccoli.Avalonia.Slices.Recipes;

/// <summary>Classifies a line in a text diff (see <see cref="TextDiff"/>).</summary>
public enum DiffLineType
{
    /// <summary>Unchanged line, present in both versions.</summary>
    Context,

    /// <summary>Line only present in the new version.</summary>
    Added,

    /// <summary>Line only present in the old version.</summary>
    Removed,
}
