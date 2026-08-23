namespace Broccoli.Avalonia.Desktop.E2ETests;

/// <summary>
/// Creates and removes the throwaway data folder each test uses so E2E runs never touch the
/// real user data. A brand-new (empty) folder is the "reset" — the app always starts with no
/// recipes, foods or settings.
/// </summary>
public static class TestData
{
    /// <summary>Creates a unique, empty scratch folder for one test run.</summary>
    public static string CreateScratchDataFolder()
    {
        string folder = Path.Combine(Path.GetTempPath(), "broccoli-e2e", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        return folder;
    }

    /// <summary>Removes the scratch folder, ignoring failures (the app may still hold SQLite locks).</summary>
    public static void DeleteScratchDataFolder(string folder)
    {
        try
        {
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, recursive: true);
            }
        }
        catch (IOException)
        {
            // Best-effort: the app may still be closing and holding the database file.
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
