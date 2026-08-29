using Broccoli.Avalonia.Storage;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Broccoli.Avalonia.Tests.Storage;

[TestClass]
public class DatabaseMigrationTests
{
    [TestMethod]
    public void Migrations_ApplyCleanly_FromEmptyDatabase()
    {
        string scratch = Path.Combine(Path.GetTempPath(), "broccoli-dbmigration-test", Guid.NewGuid().ToString("N"));
        try
        {
            AppPaths.OverrideRootFolder(scratch);

            using (BroccoliDbContext db = BroccoliDbContext.CreateForApp())
            {
                db.Database.Migrate();
            }

            // Reading settings must not throw (this is the code path that crashed when the
            // ShowRecipesAsList column was missing from the schema).
            using (BroccoliDbContext check = BroccoliDbContext.CreateForApp())
            {
                _ = check.MacroTargetSettings.FirstOrDefault();
            }
        }
        finally
        {
            Cleanup(scratch);
        }
    }

    /// <summary>
    /// Regression test for the "entity property added without a migration" bug: an existing
    /// database created before <c>ShowRecipesAsList</c> existed must be upgraded in place by
    /// <c>Migrate()</c>, and the settings read that used to throw ("no such column") must work.
    /// </summary>
    [TestMethod]
    public void Migrations_UpgradeExistingDatabase_AddsShowRecipesAsListColumn()
    {
        string scratch = Path.Combine(Path.GetTempPath(), "broccoli-dbmigration-test", Guid.NewGuid().ToString("N"));
        try
        {
            AppPaths.OverrideRootFolder(scratch);

            // Build a database at the schema state that existed before ShowRecipesAsList was added.
            using (BroccoliDbContext old = BroccoliDbContext.CreateForApp())
            {
                old.Database.Migrate("20260828000000_AddMatchedFoodInfoToGroceryListItem");
            }

            // Simulate the next app launch applying the pending migration.
            using (BroccoliDbContext upgraded = BroccoliDbContext.CreateForApp())
            {
                upgraded.Database.Migrate();
            }

            using (BroccoliDbContext check = BroccoliDbContext.CreateForApp())
            {
                bool columnExists = check.Database.SqlQueryRaw<bool>(
                        "SELECT 1 FROM pragma_table_info('MacroTargetSettings') WHERE name = 'ShowRecipesAsList'")
                    .Any();
                Assert.IsTrue(columnExists, "ShowRecipesAsList column missing after upgrade");

                // The settings read that used to throw must now succeed.
                _ = check.MacroTargetSettings.FirstOrDefault();
            }
        }
        finally
        {
            Cleanup(scratch);
        }
    }

    private static void Cleanup(string scratch)
    {
        // Release pooled SQLite connections so the database file can be deleted.
        SqliteConnection.ClearAllPools();
        AppPaths.OverrideRootFolder(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Broccoli"));
        if (Directory.Exists(scratch))
        {
            Directory.Delete(scratch, recursive: true);
        }
    }
}
