using Broccoli.Avalonia.Slices.Settings;
using Broccoli.Avalonia.Storage;
using Google.Apis.Drive.v3;

namespace Broccoli.Avalonia.Slices.Settings.Sync;

public class StorageUsageService : IStorageUsageService
{
    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp",
    };

    private readonly IGoogleDriveAuthService _authService;

    public StorageUsageService(IGoogleDriveAuthService authService)
    {
        _authService = authService;
    }

    public StorageUsageSnapshot ComputeLocalUsage()
    {
        long markdownBytes = 0;
        long imageBytes = 0;
        long backupBytes = 0;

        if (Directory.Exists(AppPaths.RecipesFolder))
        {
            foreach (string filePath in Directory.EnumerateFiles(AppPaths.RecipesFolder, "*", SearchOption.AllDirectories))
            {
                long length = SafeFileLength(filePath);
                if (IsBackupFile(filePath))
                {
                    backupBytes += length;
                }
                else if (Path.GetFileName(filePath).Equals("recipe.md", StringComparison.OrdinalIgnoreCase))
                {
                    markdownBytes += length;
                }
                else if (ImageExtensions.Contains(Path.GetExtension(filePath)))
                {
                    imageBytes += length;
                }
            }
        }

        long databaseBytes = SafeFileLength(AppPaths.DatabaseFilePath)
            + SafeFileLength(AppPaths.DatabaseFilePath + "-wal")
            + SafeFileLength(AppPaths.DatabaseFilePath + "-shm");

        return new StorageUsageSnapshot(markdownBytes, imageBytes, backupBytes, databaseBytes);
    }

    public async Task<DriveQuota?> GetDriveQuotaAsync(CancellationToken cancellationToken = default)
    {
        DriveService? drive = await _authService.TryGetDriveServiceAsync(cancellationToken);
        if (drive is null)
        {
            return null;
        }

        try
        {
            AboutResource.GetRequest request = drive.About.Get();
            request.Fields = "storageQuota";
            Google.Apis.Drive.v3.Data.About about = await request.ExecuteAsync(cancellationToken);
            return new DriveQuota(
                about.StorageQuota.Usage ?? 0,
                about.StorageQuota.Limit ?? 0);
        }
        catch (Exception)
        {
            return null;
        }
        finally
        {
            drive.Dispose();
        }
    }

    /// <summary>True when the file lives under a recipe's <c>history</c> snapshot folder.</summary>
    private static bool IsBackupFile(string filePath)
    {
        string? directory = Path.GetDirectoryName(filePath);
        while (directory is not null)
        {
            if (Path.GetFileName(directory).Equals(AppPaths.RecipeHistoryFolderName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            directory = Path.GetDirectoryName(directory);
        }

        return false;
    }

    private static long SafeFileLength(string path)
    {
        try
        {
            return File.Exists(path) ? new FileInfo(path).Length : 0;
        }
        catch (IOException)
        {
            return 0;
        }
    }
}
