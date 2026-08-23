using Avalonia.Controls;

namespace Broccoli.Avalonia.Slices.Settings;

public partial class FoodEditDialog : Window
{
    public FoodEditDialog()
    {
        InitializeComponent();
        DataContextChanged += (_, _) =>
        {
            if (DataContext is FoodEditDialogViewModel viewModel)
            {
                viewModel.RequestClose = Close;
            }
        };
    }
}
