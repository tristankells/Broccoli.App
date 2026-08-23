using Avalonia.Controls;

namespace Broccoli.Avalonia.Slices.Recipes;

public partial class AutoBalanceDialog : Window
{
    public AutoBalanceDialog()
    {
        InitializeComponent();
        DataContextChanged += (_, _) =>
        {
            if (DataContext is AutoBalanceDialogViewModel viewModel)
            {
                viewModel.RequestClose = Close;
            }
        };
    }
}
