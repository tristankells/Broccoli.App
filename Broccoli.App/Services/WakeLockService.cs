using Broccoli.App.Shared.Platform;

namespace Broccoli.App.Services;

public class WakeLockService : IWakeLockService
{
    public Task AcquireAsync()
    {
        try
        {
            DeviceDisplay.Current.KeepScreenOn = true;
        }
        catch { /* ignore — may not be available on all platforms */ }
        return Task.CompletedTask;
    }

    public Task ReleaseAsync()
    {
        try
        {
            DeviceDisplay.Current.KeepScreenOn = false;
        }
        catch { /* ignore */ }
        return Task.CompletedTask;
    }
}

