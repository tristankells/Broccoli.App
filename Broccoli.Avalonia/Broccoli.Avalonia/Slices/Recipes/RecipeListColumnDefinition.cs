using Avalonia.Controls;
using Avalonia.Media;
using Broccoli.Avalonia.Shared;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Broccoli.Avalonia.Slices.Recipes;

/// <summary>
/// One visible list-view column: its metadata, its index within the table (used to place the
/// header cell in the right grid column), and the current sort indicator suffix so the header can
/// show "Name ▲"/"Name ▼" for the active sort column.
/// </summary>
internal partial class RecipeListColumnDefinition : ViewModelBase
{
    public RecipeListColumnDefinition(RecipeListColumn column, int index)
    {
        Column = column;
        Index = index;
        Title = RecipeListColumnDefinitions.Title(column);
        Width = new GridLength(RecipeListColumnDefinitions.Weight(column), GridUnitType.Star);
        Alignment = RecipeListColumnDefinitions.Alignment(column);
    }

    public RecipeListColumn Column { get; }

    /// <summary>0-based position of the column in the visible column list.</summary>
    public int Index { get; }

    /// <summary>Star width: the table columns share the available width proportionally.</summary>
    public GridLength Width { get; }

    public string Title { get; }

    public TextAlignment Alignment { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HeaderText))]
    private string _sortSuffix = string.Empty;

    public string HeaderText => $"{Title}{SortSuffix}";
}
