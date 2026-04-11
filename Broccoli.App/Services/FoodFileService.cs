using Broccoli.App.Shared.Platform;

namespace Broccoli.App.Services;

public class FoodFileService : IFoodFileService
{
    public async Task ExportFoodsAsync(string filename, string jsonContent)
    {
        string path = Path.Combine(FileSystem.CacheDirectory, filename);
        await File.WriteAllTextAsync(path, jsonContent);
        await Share.Default.RequestAsync(new ShareFileRequest
        {
            Title = "Export Food Database",
            File  = new ShareFile(path)
        });
    }

    public async Task<string?> ImportFoodsAsync()
    {
        var options = new PickOptions
        {
            PickerTitle = "Select food database JSON file",
            FileTypes   = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.WinUI,    new[] { ".json" } },
                { DevicePlatform.Android,  new[] { "application/json" } },
                { DevicePlatform.iOS,      new[] { "public.json" } },
                { DevicePlatform.MacCatalyst, new[] { "public.json" } }
            })
        };

        var result = await FilePicker.Default.PickAsync(options);
        if (result == null) return null;

        await using var stream = await result.OpenReadAsync();
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }
}
