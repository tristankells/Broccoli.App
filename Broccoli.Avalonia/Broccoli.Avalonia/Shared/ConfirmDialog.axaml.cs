using Avalonia.Controls;

namespace Broccoli.Avalonia.Shared;

public partial class ConfirmDialog : Window
{
    public ConfirmDialog()
    {
        InitializeComponent();
        DataContextChanged += (_, _) =>
        {
            if (DataContext is ConfirmDialogViewModel viewModel)
            {
                viewModel.RequestClose = Close;
            }
        };
    }
}
