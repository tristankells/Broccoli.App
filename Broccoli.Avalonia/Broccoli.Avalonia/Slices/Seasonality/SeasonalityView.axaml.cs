using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace Broccoli.Avalonia.Slices.Seasonality;

public partial class SeasonalityView : UserControl
{
    private static readonly FilePickerFileType JsonFileType = new("JSON") { Patterns = ["*.json"] };

    public SeasonalityView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => WireFilePickers();
    }

    private void WireFilePickers()
    {
        if (DataContext is SeasonalityViewModel viewModel)
        {
            viewModel.PickImportFilePath = PickImportFilePath;
            viewModel.PickExportFilePath = PickExportFilePath;
        }
    }

    private async Task<string?> PickImportFilePath()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
        {
            return null;
        }

        IReadOnlyList<IStorageFile> files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Import seasonality data",
            AllowMultiple = false,
            FileTypeFilter = new[] { JsonFileType },
        });

        return files.Count > 0 ? files[0].TryGetLocalPath() : null;
    }

    private async Task<string?> PickExportFilePath()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
        {
            return null;
        }

        IStorageFile? file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export seasonality data",
            SuggestedFileName = "seasonality.json",
            DefaultExtension = "json",
            FileTypeChoices = new[] { JsonFileType },
        });

        return file?.TryGetLocalPath();
    }
}
