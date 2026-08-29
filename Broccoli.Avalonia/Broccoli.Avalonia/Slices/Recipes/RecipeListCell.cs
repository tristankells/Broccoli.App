using Avalonia.Media;

namespace Broccoli.Avalonia.Slices.Recipes;

/// <summary>
/// A single table cell in the list view: its text, the column it belongs to (so it lands in the
/// right grid column), and its alignment to match the header.
/// </summary>
internal sealed record RecipeListCell(int Column, string Text, TextAlignment Alignment);
