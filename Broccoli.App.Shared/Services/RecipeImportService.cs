using Broccoli.Data.Models;

namespace Broccoli.App.Shared.Services;

/// <summary>
/// Orchestrates recipe file parsing and duplicate detection.
/// Delegates actual parsing to the provided IImportFormat implementation.
/// </summary>
public class RecipeImportService
{
    /// <summary>
    /// Parses a single file using the given format and checks for duplicates by name.
    /// Returns an ImportRecipeResult with Status and IsSelected pre-set.
    /// </summary>
    public async Task<ImportRecipeResult> ParseFileAsync(
        IImportFormat format,
        string fileName,
        string fileContent,
        IEnumerable<string> existingRecipeNames)
    {
        try
        {
            var recipe = await format.ParseAsync(fileContent);

            var isDuplicate = existingRecipeNames
                .Any(n => string.Equals(n, recipe.Name, StringComparison.OrdinalIgnoreCase));

            return new ImportRecipeResult
            {
                FileName = fileName,
                Recipe = recipe,
                Status = isDuplicate ? ImportStatus.Duplicate : ImportStatus.ReadyToImport,
                IsSelected = !isDuplicate
            };
        }
        catch (Exception ex)
        {
            return new ImportRecipeResult
            {
                FileName = fileName,
                Status = ImportStatus.ParseError,
                ErrorMessage = ex.Message,
                IsSelected = false
            };
        }
    }

    /// <summary>
    /// Convenience method: processes a batch of (FileName, Content) pairs in order.
    /// </summary>
    public async Task<List<ImportRecipeResult>> ParseFilesAsync(
        IImportFormat format,
        IEnumerable<(string FileName, string Content)> files,
        IEnumerable<string> existingRecipeNames)
    {
        // Materialise names once so the enumerable isn't re-evaluated per file
        var nameSet = existingRecipeNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var results = new List<ImportRecipeResult>();

        foreach (var (fileName, content) in files)
        {
            var result = await ParseFileAsync(format, fileName, content, nameSet);
            results.Add(result);
        }

        return results;
    }
}

