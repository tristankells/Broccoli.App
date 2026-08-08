using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace Broccoli.Avalonia.Slices.Recipes;

public partial class RecipeEditView : UserControl
{
    public RecipeEditView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => WireImagePicker();
    }

    private void WireImagePicker()
    {
        if (DataContext is RecipeEditViewModel vm)
        {
            vm.PickImageFileAsync = PickImageFileAsync;
        }
    }

    private async Task<string?> PickImageFileAsync()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
        {
            return null;
        }

        IReadOnlyList<IStorageFile> files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Choose a recipe image",
            AllowMultiple = false,
            FileTypeFilter = new[] { FilePickerFileTypes.ImageAll },
        });

        return files.Count > 0 ? files[0].TryGetLocalPath() : null;
    }
}
