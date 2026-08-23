using Avalonia.Controls;

namespace Broccoli.Avalonia.Slices.Recipes;

public partial class IngredientSearchDialog : Window
{
    public IngredientSearchDialog()
    {
        InitializeComponent();
        DataContextChanged += (_, _) =>
        {
            if (DataContext is IngredientSearchDialogViewModel viewModel)
            {
                viewModel.RequestClose = Close;
            }
        };
    }
}
