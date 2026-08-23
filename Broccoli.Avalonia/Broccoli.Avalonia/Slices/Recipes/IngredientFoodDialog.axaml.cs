using Avalonia.Controls;

namespace Broccoli.Avalonia.Slices.Recipes;

public partial class IngredientFoodDialog : Window
{
    public IngredientFoodDialog()
    {
        InitializeComponent();
        DataContextChanged += (_, _) =>
        {
            if (DataContext is IngredientFoodDialogViewModel viewModel)
            {
                viewModel.RequestClose = Close;
            }
        };
    }
}
