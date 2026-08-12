using Avalonia.Controls;

namespace Broccoli.Avalonia.Slices.Groceries;

public partial class AddToCartDialog : Window
{
    public AddToCartDialog()
    {
        InitializeComponent();
        DataContextChanged += (_, _) =>
        {
            if (DataContext is AddToCartDialogViewModel viewModel)
            {
                viewModel.RequestClose = Close;
            }
        };
    }
}
