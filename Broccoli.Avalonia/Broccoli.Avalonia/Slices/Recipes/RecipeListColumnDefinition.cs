using Avalonia.Media;
using Broccoli.Avalonia.Shared;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Broccoli.Avalonia.Slices.Recipes;

/// <summary>
/// One visible list-view column: its metadata plus the current sort indicator suffix, so the
/// table header can show "Name ▲"/"Name ▼" for the active sort column.
/// </summary>
internal partial class RecipeListColumnDefinition : ViewModelBase
{
    public RecipeListColumnDefinition(RecipeListColumn column)
    {
        Column = column;
        Title = RecipeListColumnDefinitions.Title(column);
        Width = RecipeListColumnDefinitions.Width(column);
        Alignment = RecipeListColumnDefinitions.Alignment(column);
    }

    public RecipeListColumn Column { get; }

    public string Title { get; }

    public double Width { get; }

    public TextAlignment Alignment { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HeaderText))]
    private string _sortSuffix = string.Empty;

    public string HeaderText => $"{Title}{SortSuffix}";
}
