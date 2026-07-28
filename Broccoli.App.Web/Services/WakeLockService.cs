using Broccoli.App.Shared._Shared.Platform;
using Microsoft.JSInterop;

namespace Broccoli.App.Web.Services;

public class WakeLockService : IWakeLockService
{
    private readonly IJSRuntime _js;

    public WakeLockService(IJSRuntime js)
    {
        _js = js;
    }

    public async Task AcquireAsync()
    {
        try
        {
            await _js.InvokeVoidAsync("wakeLock.acquire");
        }
        catch { /* silently ignore — browser may not support Wake Lock API */ }
    }

    public async Task ReleaseAsync()
    {
        try
        {
            await _js.InvokeVoidAsync("wakeLock.release");
        }
        catch { /* silently ignore */ }
    }
}

