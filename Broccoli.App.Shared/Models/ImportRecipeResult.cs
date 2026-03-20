namespace Broccoli.Data.Models;

public enum ImportStatus
{
    ReadyToImport,
    Duplicate,
    ParseError
}

public class ImportRecipeResult
{
    /// <summary>The filename of the source file.</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>The parsed recipe, or null if parsing failed.</summary>
    public Recipe? Recipe { get; set; }

    /// <summary>The status of this import result.</summary>
    public ImportStatus Status { get; set; }

    /// <summary>Error message when Status is ParseError.</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Whether this recipe is selected for import.
    /// Defaults to true for ReadyToImport, false for Duplicate and ParseError.
    /// </summary>
    public bool IsSelected { get; set; }

    /// <summary>Populated after a save attempt: true = saved, false = failed, null = not yet attempted.</summary>
    public bool? SaveSuccess { get; set; }

    /// <summary>Error message from a failed save attempt.</summary>
    public string? SaveError { get; set; }
}

