using Avalonia.Media;

namespace Broccoli.Avalonia.Slices.Recipes;

/// <summary>A single table cell in the list view: the formatted text plus layout to match the header.</summary>
internal sealed record RecipeListCell(string Text, double Width, TextAlignment Alignment);
