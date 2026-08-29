using Broccoli.Avalonia.Shared;
using Broccoli.Avalonia.Slices.Recipes;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Broccoli.Avalonia.Slices.Settings;

/// <summary>
/// One checkbox/row in the settings page's list-view column customisation: whether the column is
/// shown and its position among the other columns.
/// </summary>
public partial class RecipeListColumnOption : ViewModelBase
{
    public RecipeListColumnOption(RecipeListColumn column, bool isSelected)
    {
        Column = column;
        IsSelected = isSelected;
    }

    public RecipeListColumn Column { get; }

    public string Title => RecipeListColumnDefinitions.Title(Column);

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private bool _canMoveUp;

    [ObservableProperty]
    private bool _canMoveDown;
}
