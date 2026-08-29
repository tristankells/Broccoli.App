using Avalonia;
using Avalonia.Controls;

namespace Broccoli.Avalonia.Shared;

/// <summary>
/// <c>Grid.ColumnDefinitions</c> is a plain CLR property (not an <see cref="AvaloniaProperty"/>),
/// so it can't be bound directly. This attached property accepts a bound
/// <see cref="ColumnDefinitions"/> and forwards it onto the target <see cref="Grid"/>, enabling
/// dynamic, star-sized table columns driven by a view model.
/// </summary>
public class GridColumns
{
    public static readonly AttachedProperty<ColumnDefinitions?> BoundColumnDefinitionsProperty =
        AvaloniaProperty.RegisterAttached<GridColumns, Grid, ColumnDefinitions?>(
            "BoundColumnDefinitions");

    static GridColumns()
    {
        BoundColumnDefinitionsProperty.Changed.AddClassHandler<Grid>((grid, e) =>
        {
            grid.ColumnDefinitions = e.GetNewValue<ColumnDefinitions?>() ?? new ColumnDefinitions();
        });
    }

    public static void SetBoundColumnDefinitions(Grid grid, ColumnDefinitions? value) =>
        grid.SetValue(BoundColumnDefinitionsProperty, value);

    public static ColumnDefinitions? GetBoundColumnDefinitions(Grid grid) =>
        grid.GetValue(BoundColumnDefinitionsProperty);
}
