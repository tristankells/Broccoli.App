namespace Broccoli.Avalonia.Slices.Settings;

public interface IFoodFileService
{
    Task ExportFoodsAsync(string filename, string jsonContent);

    Task<string?> ImportFoodsAsync();
}
