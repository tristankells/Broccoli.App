using System.ComponentModel;
using Broccoli.Avalonia.Shared;
using Broccoli.Avalonia.Slices.Settings;
using Broccoli.Avalonia.Slices.Settings.Sync;
using CommunityToolkit.Mvvm.Messaging;
using Moq;

namespace Broccoli.Avalonia.Tests.Slices.Settings.Sync;

[TestClass]
public class SyncStatusServiceTests
{
    private readonly Mock<IGoogleDriveSyncService> _syncServiceMock = new();
    private readonly Mock<IGoogleDriveAuthService> _authServiceMock = new();
    private GoogleDriveAccountInfo? _storedAccount;

    private SyncStatusService CreateService()
    {
        _authServiceMock.Setup(a => a.GetStoredAccount()).Returns(() => _storedAccount);
        return new SyncStatusService(_syncServiceMock.Object, _authServiceMock.Object);
    }

    [TestMethod]
    public void RefreshStatus_NotConnected_HasNoUnsyncedChanges()
    {
        _storedAccount = null;
        _syncServiceMock.Setup(s => s.HasPendingChanges()).Returns(true);

        SyncStatusService service = CreateService();

        Assert.IsFalse(service.IsConnected);
        Assert.IsFalse(service.HasUnsyncedChanges);
    }

    [TestMethod]
    public void RefreshStatus_ConnectedAndDirty_FlagsUnsyncedChanges()
    {
        _storedAccount = new GoogleDriveAccountInfo { Email = "me@example.com" };
        _syncServiceMock.Setup(s => s.HasPendingChanges()).Returns(true);

        SyncStatusService service = CreateService();

        Assert.IsTrue(service.IsConnected);
        Assert.IsTrue(service.HasUnsyncedChanges);
    }

    [TestMethod]
    public void RefreshStatus_ConnectedAndClean_NoUnsyncedChanges()
    {
        _storedAccount = new GoogleDriveAccountInfo { Email = "me@example.com" };
        _syncServiceMock.Setup(s => s.HasPendingChanges()).Returns(false);

        SyncStatusService service = CreateService();

        Assert.IsTrue(service.IsConnected);
        Assert.IsFalse(service.HasUnsyncedChanges);
    }

    [TestMethod]
    public void RefreshStatus_SurfacesLastSyncedTime()
    {
        DateTime lastSynced = DateTime.UtcNow.AddHours(-2);
        _syncServiceMock.Setup(s => s.LastSyncedAtUtc).Returns(lastSynced);

        SyncStatusService service = CreateService();

        Assert.AreEqual(lastSynced, service.LastSyncedAtUtc);
    }

    [TestMethod]
    public void SyncNowAsync_IsSyncingOnlyWhileRunning()
    {
        var completion = new TaskCompletionSource<SyncResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        _syncServiceMock.Setup(s => s.SyncAsync(It.IsAny<IProgress<SyncProgress>?>(), It.IsAny<CancellationToken>()))
            .Returns(completion.Task);
        SyncStatusService service = CreateService();

        Task<SyncResult> syncTask = service.SyncNowAsync();
        Assert.IsTrue(service.IsSyncing);

        completion.SetResult(new SyncResult { Success = true });
        SyncResult result = syncTask.GetAwaiter().GetResult();

        Assert.IsFalse(service.IsSyncing);
        Assert.IsTrue(result.Success);
        Assert.IsNull(service.LastSyncError);
    }

    [TestMethod]
    public void SyncNowAsync_OnFailure_SetsLastSyncError()
    {
        _syncServiceMock.Setup(s => s.SyncAsync(It.IsAny<IProgress<SyncProgress>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SyncResult { Success = false, ErrorMessage = "boom" });
        SyncStatusService service = CreateService();

        SyncResult result = service.SyncNowAsync().GetAwaiter().GetResult();

        Assert.IsFalse(result.Success);
        Assert.AreEqual("boom", service.LastSyncError);
    }

    [TestMethod]
    public void SyncNowAsync_WhileAlreadySyncing_BacksOff()
    {
        var completion = new TaskCompletionSource<SyncResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        _syncServiceMock.Setup(s => s.SyncAsync(It.IsAny<IProgress<SyncProgress>?>(), It.IsAny<CancellationToken>()))
            .Returns(completion.Task);
        SyncStatusService service = CreateService();

        Task<SyncResult> first = service.SyncNowAsync();
        SyncResult second = service.SyncNowAsync().GetAwaiter().GetResult();

        Assert.IsFalse(second.Success);
        Assert.IsTrue(second.ErrorMessage!.Contains("already in progress"));
        _syncServiceMock.Verify(s => s.SyncAsync(It.IsAny<IProgress<SyncProgress>?>(), It.IsAny<CancellationToken>()), Times.Once);

        completion.SetResult(new SyncResult { Success = true });
        first.GetAwaiter().GetResult();
    }

    [TestMethod]
    public void StorageChangedMessage_TriggersRefresh()
    {
        _storedAccount = new GoogleDriveAccountInfo { Email = "me@example.com" };
        _syncServiceMock.Setup(s => s.HasPendingChanges()).Returns(false);
        SyncStatusService service = CreateService();
        Assert.IsFalse(service.HasUnsyncedChanges);

        _syncServiceMock.Setup(s => s.HasPendingChanges()).Returns(true);
        WeakReferenceMessenger.Default.Send(new StorageChangedMessage());

        Assert.IsTrue(service.HasUnsyncedChanges);
    }
}
