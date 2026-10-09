using Avalonia.Controls;

namespace Broccoli.Avalonia.Slices.Seasonality;

public partial class SeasonalityImportPreviewDialog : Window
{
    public SeasonalityImportPreviewDialog()
    {
        InitializeComponent();
        DataContextChanged += (_, _) =>
        {
            if (DataContext is SeasonalityImportPreviewViewModel viewModel)
            {
                viewModel.RequestClose = Close;
            }
        };
    }
}
