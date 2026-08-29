using System.ComponentModel;
using Broccoli.Avalonia.Shell;
using Broccoli.Avalonia.Slices.Settings.Sync;
using Moq;

namespace Broccoli.Avalonia.Tests.Shell;

[TestClass]
public class SyncStatusFooterViewModelTests
{
    [TestMethod]
    public void SummaryText_WhileSyncing_IndicatesSyncInProgress()
    {
        var status = new Mock<ISyncStatusService>();
        status.SetupGet(s => s.IsSyncing).Returns(true);

        var vm = new SyncStatusFooterViewModel(status.Object);

        Assert.AreEqual("Syncing to Drive…", vm.SummaryText);
    }

    [TestMethod]
    public void SummaryText_NotConnected_IndicatesBackupOff()
    {
        var status = new Mock<ISyncStatusService>();
        status.SetupGet(s => s.IsConnected).Returns(false);

        var vm = new SyncStatusFooterViewModel(status.Object);

        Assert.AreEqual("Backup off", vm.SummaryText);
        Assert.IsTrue(vm.ShowEnableHint);
        Assert.IsFalse(vm.ShowSyncIcon);
    }

    [TestMethod]
    public void SummaryText_ConnectedWithPendingChanges_IndicatesUnsynced()
    {
        var status = new Mock<ISyncStatusService>();
        status.SetupGet(s => s.IsConnected).Returns(true);
        status.SetupGet(s => s.HasUnsyncedChanges).Returns(true);
        status.SetupGet(s => s.LastSyncedAtUtc).Returns(DateTime.UtcNow.AddMinutes(-30));

        var vm = new SyncStatusFooterViewModel(status.Object);

        Assert.IsTrue(vm.SummaryText.StartsWith("Unsynced changes"));
        Assert.IsTrue(vm.ShowSyncIcon);
    }

    [TestMethod]
    public void SummaryText_ConnectedAndClean_ShowsRelativeLastSynced()
    {
        var status = new Mock<ISyncStatusService>();
        status.SetupGet(s => s.IsConnected).Returns(true);
        status.SetupGet(s => s.HasUnsyncedChanges).Returns(false);
        status.SetupGet(s => s.LastSyncedAtUtc).Returns(DateTime.UtcNow.AddHours(-3));

        var vm = new SyncStatusFooterViewModel(status.Object);

        Assert.AreEqual("Synced 3h ago", vm.SummaryText);
    }

    [TestMethod]
    public void SummaryText_NeverSynced_ShowsNever()
    {
        var status = new Mock<ISyncStatusService>();
        status.SetupGet(s => s.IsConnected).Returns(true);
        status.SetupGet(s => s.HasUnsyncedChanges).Returns(false);
        status.SetupGet(s => s.LastSyncedAtUtc).Returns((DateTime?)null);

        var vm = new SyncStatusFooterViewModel(status.Object);

        Assert.AreEqual("Synced never", vm.SummaryText);
    }

    [TestMethod]
    public void SummaryText_UpdatesWhenStatusServiceChanges()
    {
        var status = new Mock<ISyncStatusService>();
        status.SetupGet(s => s.IsConnected).Returns(false);

        var vm = new SyncStatusFooterViewModel(status.Object);
        Assert.AreEqual("Backup off", vm.SummaryText);

        status.SetupGet(s => s.IsConnected).Returns(true);
        status.SetupGet(s => s.HasUnsyncedChanges).Returns(false);
        status.SetupGet(s => s.LastSyncedAtUtc).Returns(DateTime.UtcNow.AddSeconds(-30));
        status.Raise(s => s.PropertyChanged += null, new PropertyChangedEventArgs(nameof(ISyncStatusService.IsConnected)));

        Assert.AreEqual("Synced just now", vm.SummaryText);
    }

    [TestMethod]
    public void SyncNowCommand_InvokesStatusService()
    {
        var status = new Mock<ISyncStatusService>();
        status.Setup(s => s.SyncNowAsync(It.IsAny<IProgress<SyncProgress>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SyncResult { Success = true });

        var vm = new SyncStatusFooterViewModel(status.Object);

        vm.SyncNowCommand.Execute(null);

        status.Verify(s => s.SyncNowAsync(It.IsAny<IProgress<SyncProgress>?>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
