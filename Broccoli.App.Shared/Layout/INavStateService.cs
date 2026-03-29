namespace Broccoli.App.Shared.Layout;

public interface INavStateService
{
    bool IsCollapsed { get; }
    event Action? OnChanged;
    void Toggle();
    Task InitializeAsync();
}

