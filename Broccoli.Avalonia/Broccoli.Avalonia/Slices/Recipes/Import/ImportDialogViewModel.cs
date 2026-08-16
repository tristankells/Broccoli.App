using System.Collections.ObjectModel;
using Avalonia.Platform.Storage;
using Broccoli.Avalonia.Models;
using Broccoli.Avalonia.Shared;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Broccoli.Avalonia.Slices.Recipes.Import;

public partial class ImportDialogViewModel : ViewModelBase
{
    private readonly RecipeImportService _importService;
    private readonly IRecipeService _recipeService;
    private readonly IReadOnlyList<IImportFormat> _formats;

    private IReadOnlySet<string> _existingNames = new HashSet<string>();

    [ObservableProperty]
    private bool _isVisible;

    [ObservableProperty]
    private int _selectedFormatIndex;

    [ObservableProperty]
    private string _pasteContent = string.Empty;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _showPreview;

    public ImportDialogViewModel(
        RecipeImportService importService,
        IRecipeService recipeService,
        IEnumerable<IImportFormat> formats)
    {
        _importService = importService;
        _recipeService = recipeService;
        _formats = formats.ToList();
    }

    public ObservableCollection<ImportRecipeResult> Results { get; } = new();

    public IReadOnlyList<IImportFormat> Formats => _formats;

    public IImportFormat? SelectedFormat => _formats.Count > 0 ? _formats[SelectedFormatIndex] : null;

    public bool IsPasteBased => SelectedFormat?.IsPasteBased ?? false;

    public bool IsFileBased => !IsPasteBased;

    public Action? Closed { get; set; }

    public void Open(IReadOnlySet<string> existingNames)
    {
        _existingNames = existingNames;
        IsVisible = true;
        SelectedFormatIndex = 0;
        PasteContent = string.Empty;
        ShowPreview = false;
        Results.Clear();
        ErrorMessage = null;
        RefreshProperties();
    }

    private static global::Avalonia.Platform.Storage.IStorageProvider? GetStorage()
    {
        if (global::Avalonia.Application.Current?.ApplicationLifetime is
            global::Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.MainWindow?.StorageProvider;
        }

        return null;
    }

    [RelayCommand]
    private void Close()
    {
        IsVisible = false;
        Closed?.Invoke();
    }

    [RelayCommand]
    private void SelectFormat()
    {
        RefreshProperties();
    }

    [RelayCommand]
    private async Task PickFilesAsync()
    {
        if (SelectedFormat is null)
        {
            return;
        }

        IsBusy = true;
        ErrorMessage = null;
        try
        {
            var files = new List<(string, string)>();
            IStorageProvider? storage = GetStorage();
            if (storage is not null)
            {
                IReadOnlyList<IStorageFile> picked = await storage.OpenFilePickerAsync(new global::Avalonia.Platform.Storage.FilePickerOpenOptions
                {
                    Title = $"Select {SelectedFormat.DisplayName} files",
                    AllowMultiple = true,
                    FileTypeFilter = new[]
                    {
                        new global::Avalonia.Platform.Storage.FilePickerFileType(SelectedFormat.DisplayName)
                        { Patterns = new[] { $"*{SelectedFormat.FileExtension}" } },
                    },
                });
                foreach (IStorageFile f in picked)
                {
                    await using Stream stream = await f.OpenReadAsync();
                    using var reader = new StreamReader(stream);
                    files.Add((f.Name, await reader.ReadToEndAsync()));
                }
            }

            if (files.Count > 0)
            {
                Results.Clear();
                List<ImportRecipeResult> results = await _importService.ParseFilesAsync(SelectedFormat, files, _existingNames);
                foreach (ImportRecipeResult r in results)
                {
                    Results.Add(r);
                }

                ShowPreview = true;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ParsePasteAsync()
    {
        if (SelectedFormat is null || string.IsNullOrWhiteSpace(PasteContent))
        {
            return;
        }

        IsBusy = true;
        ErrorMessage = null;
        try
        {
            Results.Clear();
            ImportRecipeResult result = await _importService.ParseFileAsync(SelectedFormat, "pasted.txt", PasteContent, _existingNames);
            Results.Add(result);
            ShowPreview = true;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ImportSelectedAsync()
    {
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            foreach (ImportRecipeResult? r in Results.Where(r => r.IsSelected && r.Status == ImportStatus.ReadyToImport && r.Recipe is not null))
            {
                try
                {
                    _recipeService.Create(r.Recipe!);
                    r.SaveSuccess = true;
                }
                catch (Exception ex)
                {
                    r.SaveSuccess = false;
                    r.SaveError = ex.Message;
                }
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnSelectedFormatIndexChanged(int value) => RefreshProperties();

    private void RefreshProperties()
    {
        OnPropertyChanged(nameof(SelectedFormat));
        OnPropertyChanged(nameof(IsPasteBased));
        OnPropertyChanged(nameof(IsFileBased));
    }
}
