namespace Broccoli.App.Shared.Platform;

public interface IWakeLockService
{
    Task AcquireAsync();
    Task ReleaseAsync();
}

