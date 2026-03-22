using Broccoli.Data.Models;
using Broccoli.App.Shared.Slices.Recipes;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Broccoli.App.Shared.Slices.Recipes.Import;

public partial class ImportRecipesDialog
{
    [Inject] private RecipeImportService ImportService { get; set; } = null!;
    [Inject] private IRecipeService RecipeService { get; set; } = null!;
    [Inject] private IEnumerable<IImportFormat> ImportFormats { get; set; } = null!;

    [Parameter] public bool IsVisible { get; set; }
    [Parameter] public IEnumerable<Recipe> ExistingRecipes { get; set; } = [];
    [Parameter] public EventCallback OnCancel { get; set; }

    /// <summary>Fired after all saves complete so the parent can refresh its list.</summary>
    [Parameter] public EventCallback OnConfirm { get; set; }

    // -- State --------------------------------------------------------------
    private int _currentStep = 1;
    private IImportFormat? _activeFormat;
    private int _selectedFileCount;
    private IReadOnlyList<IBrowserFile> _selectedFiles = [];
    private List<ImportRecipeResult> _importResults = [];
    private bool _isParsing;
    private bool _isSaving;
    private bool _saveComplete;

    // -- Lifecycle ----------------------------------------------------------
    protected override void OnParametersSet()
    {
        // Initialise the active format the first time the dialog becomes visible
        if (IsVisible && _activeFormat is null)
            _activeFormat = ImportFormats.FirstOrDefault();
    }

    // -- Step 1 handlers ---------------------------------------------------
    private void OnFormatChanged(ChangeEventArgs e)
    {
        var name = e.Value?.ToString();
        _activeFormat = ImportFormats.FirstOrDefault(f => f.DisplayName == name);
        // Reset any previously chosen files when the format changes
        _selectedFileCount = 0;
        _selectedFiles = [];
    }

    private void HandleFilesChanged(InputFileChangeEventArgs e)
    {
        _selectedFiles = e.GetMultipleFiles(200);
        _selectedFileCount = _selectedFiles.Count;
    }

    private async Task AdvanceToPreview()
    {
        if (_activeFormat is null || _selectedFiles.Count == 0) return;

        _isParsing = true;
        StateHasChanged();

        try
        {
            var existingNames = ExistingRecipes.Select(r => r.Name);
            var fileData = new List<(string FileName, string Content)>();

            foreach (var file in _selectedFiles)
            {
                try
                {
                    // Allow up to 10 MB per file
                    using var stream = file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024);
                    using var reader = new StreamReader(stream);
                    var content = await reader.ReadToEndAsync();
                    fileData.Add((file.Name, content));
                }
                catch (Exception ex)
                {
                    // Surface unreadable files as parse errors rather than crashing
                    fileData.Add((file.Name, $"__READ_ERROR__:{ex.Message}"));
                }
            }

            _importResults = await ImportService.ParseFilesAsync(_activeFormat, fileData, existingNames);
        }
        catch (Exception ex)
        {
            // Catastrophic failure — surface all files as errors
            _importResults = _selectedFiles
                .Select(f => new ImportRecipeResult
                {
                    FileName = f.Name,
                    Status = ImportStatus.ParseError,
                    ErrorMessage = ex.Message,
                    IsSelected = false
                })
                .ToList();
        }
        finally
        {
            _isParsing = false;
            _currentStep = 2;
        }
    }

    // -- Step 2 handlers ---------------------------------------------------
    private void GoBack()
    {
        _currentStep = 1;
        _importResults.Clear();
        _saveComplete = false;
    }

    private async Task HandleConfirm()
    {
        _isSaving = true;
        StateHasChanged();

        var toSave = _importResults
            .Where(r => r.IsSelected && r.Recipe is not null)
            .ToList();

        foreach (var result in toSave)
        {
            try
            {
                await RecipeService.AddAsync(result.Recipe!);
                result.SaveSuccess = true;
            }
            catch (Exception ex)
            {
                result.SaveSuccess = false;
                result.SaveError = ex.Message;
            }

            StateHasChanged();
        }

        _isSaving = false;
        _saveComplete = true;
        StateHasChanged();
    }

    private async Task HandleClose()
    {
        await OnConfirm.InvokeAsync();
        ResetState();
    }

    private async Task HandleCancel()
    {
        if (_isSaving) return;
        await OnCancel.InvokeAsync();
        ResetState();
    }

    private void HandleBackdropClick()
    {
        if (!_isSaving)
            _ = HandleCancel();
    }

    // -- Helpers -----------------------------------------------------------
    private int SelectedCount => _importResults.Count(r => r.IsSelected);

    private static string GetRowClass(ImportRecipeResult result) => result.Status switch
    {
        ImportStatus.Duplicate => "row-duplicate",
        ImportStatus.ParseError => "row-error",
        _ => "row-ready"
    };

    private void ResetState()
    {
        _currentStep = 1;
        _selectedFileCount = 0;
        _selectedFiles = [];
        _importResults.Clear();
        _isParsing = false;
        _isSaving = false;
        _saveComplete = false;
        _activeFormat = null;
    }
}


