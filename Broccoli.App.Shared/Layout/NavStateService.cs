using Microsoft.JSInterop;

namespace Broccoli.App.Shared.Layout;

public class NavStateService : INavStateService
{
    private readonly IJSRuntime _js;
    private const string StorageKey = "navCollapsed";

    public bool IsCollapsed { get; private set; }
    public event Action? OnChanged;

    public NavStateService(IJSRuntime js)
    {
        _js = js;
    }

    public async Task InitializeAsync()
    {
        try
        {
            var stored = await _js.InvokeAsync<string?>("localStorage.getItem", StorageKey);
            IsCollapsed = stored == "true";
        }
        catch
        {
            IsCollapsed = false;
        }
    }

    public void Toggle()
    {
        IsCollapsed = !IsCollapsed;
        _ = PersistAsync(IsCollapsed);
        OnChanged?.Invoke();
    }

    private async Task PersistAsync(bool value)
    {
        try
        {
            await _js.InvokeVoidAsync("localStorage.setItem", StorageKey, value ? "true" : "false");
        }
        catch { /* ignore — localStorage may be unavailable (e.g. MAUI) */ }
    }
}

