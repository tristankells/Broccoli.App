using Avalonia.Controls;

namespace Broccoli.Avalonia.Shared;

public partial class StartupErrorDialog : Window
{
    public StartupErrorDialog()
    {
        InitializeComponent();
        DataContextChanged += (_, _) =>
        {
            if (DataContext is StartupErrorDialogViewModel viewModel)
            {
                viewModel.RequestClose = Close;
            }
        };
    }
}
