using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Broccoli.Avalonia.Shared;

public partial class ErrorDialogViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _title = "Error";

    [ObservableProperty]
    private string _message = string.Empty;

    public Action? RequestClose { get; set; }

    [RelayCommand]
    private void Close() => RequestClose?.Invoke();
}
