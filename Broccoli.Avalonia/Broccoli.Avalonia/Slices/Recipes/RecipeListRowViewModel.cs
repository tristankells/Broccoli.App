namespace Broccoli.Avalonia.Slices.Recipes;

/// <summary>
/// One row of the list-view table: wraps a recipe card and exposes the formatted cells for the
/// currently visible columns, in their configured order.
/// </summary>
internal sealed class RecipeListRowViewModel
{
    public RecipeListRowViewModel(RecipeCardViewModel card, IReadOnlyList<RecipeListColumnDefinition> columns)
    {
        Card = card;
        Cells = columns
            .Select(column => new RecipeListCell(card.ColumnText(column.Column), column.Width, column.Alignment))
            .ToList();
    }

    public RecipeCardViewModel Card { get; }

    public IReadOnlyList<RecipeListCell> Cells { get; }
}
