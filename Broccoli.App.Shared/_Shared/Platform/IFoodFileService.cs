namespace Broccoli.App.Shared._Shared.Platform;

/// <summary>
/// Abstraction for platform-specific food database file I/O (export/import).
/// Implemented separately for Web (JS interop) and MAUI (native file picker / share sheet).
/// </summary>
public interface IFoodFileService
{
    /// <summary>
    /// Exports the given JSON content as a file named <paramref name="filename"/>.
    /// On Web: triggers a browser download. On MAUI: opens the native share sheet.
    /// </summary>
    Task ExportFoodsAsync(string filename, string jsonContent);

    /// <summary>
    /// Lets the user pick a JSON file to import.
    /// Returns the raw JSON string content, or <c>null</c> if the user cancelled.
    /// </summary>
    Task<string?> ImportFoodsAsync();
}
