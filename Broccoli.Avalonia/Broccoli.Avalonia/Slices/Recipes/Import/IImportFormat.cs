using Broccoli.Avalonia.Models;

namespace Broccoli.Avalonia.Slices.Recipes.Import;

public interface IImportFormat
{
    string DisplayName { get; }

    string FileExtension { get; }

    IReadOnlyList<string> ExportInstructions { get; }

    bool IsPasteBased => false;

    Task<Recipe> ParseAsync(string fileContent);
}
