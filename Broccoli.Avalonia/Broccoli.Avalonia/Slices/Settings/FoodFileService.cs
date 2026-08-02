using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;

namespace Broccoli.Avalonia.Slices.Settings;

public class FoodFileService : IFoodFileService
{
    public async Task ExportFoodsAsync(string filename, string jsonContent)
    {
        var storage = GetStorage();
        if (storage is null)
        {
            return;
        }

        var file = await storage.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export Food Database",
            DefaultExtension = "json",
            SuggestedFileName = filename,
            FileTypeChoices = new[]
            {
                new FilePickerFileType("JSON files") { Patterns = new[] { "*.json" } }
            }
        });

        if (file is null)
        {
            return;
        }

        await using var stream = await file.OpenWriteAsync();
        await using var writer = new StreamWriter(stream);
        await writer.WriteAsync(jsonContent);
    }

    public async Task<string?> ImportFoodsAsync()
    {
        var storage = GetStorage();
        if (storage is null)
        {
            return null;
        }

        var files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Import Food Database",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("JSON files") { Patterns = new[] { "*.json" } }
            }
        });

        if (files.Count == 0)
        {
            return null;
        }

        await using var stream = await files[0].OpenReadAsync();
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }

    private static IStorageProvider? GetStorage()
    {
        if (global::Avalonia.Application.Current?.ApplicationLifetime is
            IClassicDesktopStyleApplicationLifetime desktop &&
            desktop.MainWindow is { } window)
        {
            return window.StorageProvider;
        }
        return null;
    }
}
