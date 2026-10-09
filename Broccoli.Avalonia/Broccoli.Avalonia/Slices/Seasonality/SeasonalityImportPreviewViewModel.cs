using Broccoli.Avalonia.Shared;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Broccoli.Avalonia.Slices.Seasonality;

/// <summary>
/// Shows what a seasonality JSON import is about to do (item count, a sample of names, and any
/// items that will be skipped) and requires an explicit confirmation before replacing the dataset.
/// </summary>
public partial class SeasonalityImportPreviewViewModel : ViewModelBase
{
    public SeasonalityImportPreviewViewModel(IReadOnlyList<string> itemNames, IReadOnlyList<string> errors)
    {
        ItemCount = itemNames.Count;
        Errors = errors.ToList();
        SampleNames = itemNames.Take(MaxSampleNames).ToList();
        HasMoreNames = itemNames.Count > MaxSampleNames;
    }

    private const int MaxSampleNames = 30;

    public int ItemCount { get; }

    public int SkippedCount => Errors.Count;

    public bool HasErrors => Errors.Count > 0;

    public bool HasItems => ItemCount > 0;

    public bool HasMoreNames { get; }

    public string Summary => ItemCount == 0
        ? "No importable items were found in the file."
        : $"{ItemCount} produce item{(ItemCount == 1 ? string.Empty : "s")} ready to import";

    public List<string> SampleNames { get; }

    public string SampleNamesText => string.Join(", ", SampleNames);

    public List<string> Errors { get; }

    /// <summary>Called when the user confirms; performs the actual import.</summary>
    public Action? ConfirmAction { get; set; }

    public Action? RequestClose { get; set; }

    [RelayCommand]
    private void Confirm()
    {
        ConfirmAction?.Invoke();
        RequestClose?.Invoke();
    }

    [RelayCommand]
    private void Cancel() => RequestClose?.Invoke();
}
