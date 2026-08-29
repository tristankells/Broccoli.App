using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Broccoli.Avalonia.Shared;

public partial class ConfirmDialogViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _title = "Confirm";

    [ObservableProperty]
    private string _message = string.Empty;

    [ObservableProperty]
    private string _confirmText = "OK";

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
