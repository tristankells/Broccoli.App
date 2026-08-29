using Broccoli.Avalonia.Slices.Settings;
using Broccoli.Avalonia.Slices.Settings.Sync;
using Broccoli.Avalonia.Storage;
using Moq;

namespace Broccoli.Avalonia.Tests.Slices.Settings.Sync;

[TestClass]
public class GoogleDriveSyncServiceHasPendingChangesTests
{
    private string _scratch = null!;

    [TestInitialize]
    public void SetUp()
    {
        _scratch = Path.Combine(Path.GetTempPath(), "broccoli-sync-dirty-test", Guid.NewGuid().ToString("N"));
        AppPaths.OverrideRootFolder(_scratch);
    }

    [TestCleanup]
    public void TearDown()
    {
        AppPaths.OverrideRootFolder(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Broccoli"));
        if (Directory.Exists(_scratch))
        {
            Directory.Delete(_scratch, recursive: true);
        }
    }

    [TestMethod]
    public void HasPendingChanges_NoData_IsFalse()
    {
        SetLastSynced(DateTime.UtcNow.AddHours(-1));

        Assert.IsFalse(CreateService().HasPendingChanges());
    }

    [TestMethod]
    public void HasPendingChanges_RecipeChangedAfterSync_IsTrue()
    {
        SetLastSynced(DateTime.UtcNow.AddHours(-1));
        WriteRecipeChangedSinceLastSync("recipe-1");

        Assert.IsTrue(CreateService().HasPendingChanges());
    }

    [TestMethod]
    public void HasPendingChanges_RecipeUnchangedSinceSync_IsFalse()
    {
        DateTime lastSync = DateTime.UtcNow;
        SetLastSynced(lastSync);
        string mdPath = AppPaths.RecipeMarkdownFilePath("recipe-1");
        File.WriteAllText(mdPath, "name: Pasta\n");
        File.SetLastWriteTimeUtc(mdPath, lastSync.AddHours(-2));

        Assert.IsFalse(CreateService().HasPendingChanges());
    }

    [TestMethod]
    public void HasPendingChanges_DatabaseChangedAfterSync_IsTrue()
    {
        SetLastSynced(DateTime.UtcNow.AddHours(-1));
        File.WriteAllText(AppPaths.DatabaseFilePath, "sqlite");
        File.SetLastWriteTimeUtc(AppPaths.DatabaseFilePath, DateTime.UtcNow);

        Assert.IsTrue(CreateService().HasPendingChanges());
    }

    [TestMethod]
    public void HasPendingChanges_TombstoneChangedAfterSync_IsTrue()
    {
        SetLastSynced(DateTime.UtcNow.AddHours(-1));
        File.WriteAllText(AppPaths.TombstonesFilePath, "[]");
        File.SetLastWriteTimeUtc(AppPaths.TombstonesFilePath, DateTime.UtcNow);

        Assert.IsTrue(CreateService().HasPendingChanges());
    }

    [TestMethod]
    public void HasPendingChanges_NeverSynced_WithData_IsTrue()
    {
        File.WriteAllText(AppPaths.DatabaseFilePath, "sqlite");

        Assert.IsTrue(CreateService().HasPendingChanges());
    }

    private static GoogleDriveSyncService CreateService() =>
        new(new Mock<IGoogleDriveAuthService>().Object);

    private static void SetLastSynced(DateTime utc) =>
        File.WriteAllText(
            AppPaths.SyncStateFilePath,
            $"{{\"LastSyncedAtUtc\":\"{utc:O}\"}}");

    private static void WriteRecipeChangedSinceLastSync(string recipeId)
    {
        string mdPath = AppPaths.RecipeMarkdownFilePath(recipeId);
        File.WriteAllText(mdPath, $"name: {recipeId}\n");
        File.SetLastWriteTimeUtc(mdPath, DateTime.UtcNow);
    }
}
