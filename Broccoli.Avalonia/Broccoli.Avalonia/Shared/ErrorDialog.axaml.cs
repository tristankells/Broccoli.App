using Avalonia.Controls;

namespace Broccoli.Avalonia.Shared;

public partial class ErrorDialog : Window
{
    public ErrorDialog()
    {
        InitializeComponent();
        DataContextChanged += (_, _) =>
        {
            if (DataContext is ErrorDialogViewModel viewModel)
            {
                viewModel.RequestClose = Close;
            }
        };
    }

    /// <summary>Shows a simple error dialog with the given title and message.</summary>
    public static void Show(string title, string message)
    {
        var dialog = new ErrorDialog
        {
            DataContext = new ErrorDialogViewModel { Title = title, Message = message },
        };
        dialog.Show();
    }
}
