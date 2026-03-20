using Broccoli.Data.Models;

namespace Broccoli.App.Shared.Services;

/// <summary>
/// Abstraction for an import format. Each implementation handles one source format
/// (e.g. Paprika HTML, Mealie JSON). Register all implementations in DI as IImportFormat
/// and they will appear automatically in the import dialog dropdown.
/// </summary>
public interface IImportFormat
{
    /// <summary>Display name shown in the format dropdown, e.g. "Paprika — HTML Export".</summary>
    string DisplayName { get; }

    /// <summary>File extension(s) accepted, e.g. ".html". Used for the file input accept attribute.</summary>
    string FileExtension { get; }

    /// <summary>Ordered step-by-step export instructions shown in the dialog.</summary>
    IReadOnlyList<string> ExportInstructions { get; }

    /// <summary>
    /// Parses raw file content into a Recipe.
    /// Throws an exception with a user-friendly message on parse failure.
    /// </summary>
    Task<Recipe> ParseAsync(string fileContent);
}

