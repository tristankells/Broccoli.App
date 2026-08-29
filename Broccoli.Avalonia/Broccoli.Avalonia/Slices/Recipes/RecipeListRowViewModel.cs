using Avalonia.Controls;

namespace Broccoli.Avalonia.Slices.Recipes;

/// <summary>
/// One row of the list-view table: wraps a recipe card and exposes the formatted cells for the
/// currently visible columns (in their configured order) plus the matching grid column
/// definitions, so every row lays out identically and fills the available width.
/// </summary>
internal sealed class RecipeListRowViewModel
{
    public RecipeListRowViewModel(RecipeCardViewModel card, IReadOnlyList<RecipeListColumnDefinition> columns)
    {
        Card = card;

        ColumnDefinitions = new ColumnDefinitions();
        foreach (RecipeListColumnDefinition column in columns)
        {
            ColumnDefinitions.Add(new ColumnDefinition(column.Width));
        }

        Cells = columns
            .Select((column, index) => new RecipeListCell(index, card.ColumnText(column.Column), column.Alignment))
            .ToList();
    }

    public RecipeCardViewModel Card { get; }

    public ColumnDefinitions ColumnDefinitions { get; }

    public IReadOnlyList<RecipeListCell> Cells { get; }
}
