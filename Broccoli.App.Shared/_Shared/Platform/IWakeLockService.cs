namespace Broccoli.App.Shared._Shared.Platform;

public interface IWakeLockService
{
    Task AcquireAsync();
    Task ReleaseAsync();
}

