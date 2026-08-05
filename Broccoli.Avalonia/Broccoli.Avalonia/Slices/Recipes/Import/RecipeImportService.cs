using Broccoli.Avalonia.Models;

namespace Broccoli.Avalonia.Slices.Recipes.Import;

public class RecipeImportService
{
    public async Task<ImportRecipeResult> ParseFileAsync(
        IImportFormat format, string fileName, string content, IReadOnlySet<string> existingRecipeNames)
    {
        var result = new ImportRecipeResult { FileName = fileName, IsSelected = true };
        try
        {
            Recipe recipe = await format.ParseAsync(content);
            if (existingRecipeNames.Contains(recipe.Name, StringComparer.OrdinalIgnoreCase))
            {
                result.Status = ImportStatus.Duplicate;
            }
            else
            {
                result.Recipe = recipe;
                result.Status = ImportStatus.ReadyToImport;
            }
        }
        catch (Exception ex)
        {
            result.Status = ImportStatus.ParseError;
            result.ErrorMessage = ex.Message;
            result.IsSelected = false;
        }
        return result;
    }

    public async Task<List<ImportRecipeResult>> ParseFilesAsync(
        IImportFormat format, IEnumerable<(string fileName, string content)> files,
        IReadOnlySet<string> existingRecipeNames)
    {
        var results = new List<ImportRecipeResult>();
        foreach ((string? fileName, string? content) in files)
        {
            results.Add(await ParseFileAsync(format, fileName, content, existingRecipeNames));
        }

        return results;
    }
}
