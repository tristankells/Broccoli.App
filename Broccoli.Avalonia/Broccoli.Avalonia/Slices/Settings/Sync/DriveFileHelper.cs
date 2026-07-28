using Google.Apis.Drive.v3;
using Google.Apis.Upload;
using DriveFile = Google.Apis.Drive.v3.Data.File;

namespace Broccoli.Avalonia.Slices.Settings.Sync;

/// <summary>
/// Thin helper over the raw Drive API v3 client for the handful of operations the sync service
/// needs: find-or-create folders, find/create/update files by name within a parent folder, and
/// upload/download file content. Kept separate from <see cref="GoogleDriveSyncService"/> so the
/// sync algorithm itself reads as orchestration rather than being tangled up with API plumbing.
/// </summary>
internal static class DriveFileHelper
{
    private const string FolderMimeType = "application/vnd.google-apps.folder";

    public static async Task<string> FindOrCreateFolderAsync(
        DriveService drive, string name, string? parentId, CancellationToken ct)
    {
        var existing = await FindChildAsync(drive, name, parentId, FolderMimeType, ct);
        if (existing is not null)
        {
            return existing.Id;
        }

        var folder = new DriveFile
        {
            Name = name,
            MimeType = FolderMimeType,
            Parents = parentId is null ? null : [parentId]
        };

        var request = drive.Files.Create(folder);
        request.Fields = "id";
        var created = await request.ExecuteAsync(ct);
        return created.Id;
    }

    /// <summary>Finds a direct child of <paramref name="parentId"/> by exact name (file or sub-folder).</summary>
    public static Task<DriveFile?> FindChildAsync(
        DriveService drive, string name, string? parentId, CancellationToken ct) =>
        FindChildAsync(drive, name, parentId, mimeType: null, ct);

    private static async Task<DriveFile?> FindChildAsync(
        DriveService drive, string name, string? parentId, string? mimeType, CancellationToken ct)
    {
        var escapedName = name.Replace("'", "\\'");
        var q = $"name = '{escapedName}' and trashed = false";
        if (parentId is not null)
        {
            q += $" and '{parentId}' in parents";
        }
        if (mimeType is not null)
        {
            q += $" and mimeType = '{mimeType}'";
        }

        var request = drive.Files.List();
        request.Q = q;
        request.Fields = "files(id, name, modifiedTime)";
        request.Spaces = "drive";
        var result = await request.ExecuteAsync(ct);
        return result.Files.FirstOrDefault();
    }

    public static async Task<List<DriveFile>> ListChildrenAsync(
        DriveService drive, string parentId, CancellationToken ct)
    {
        var request = drive.Files.List();
        request.Q = $"'{parentId}' in parents and trashed = false";
        request.Fields = "files(id, name, mimeType, modifiedTime)";
        request.Spaces = "drive";
        request.PageSize = 1000;
        var result = await request.ExecuteAsync(ct);
        return result.Files.ToList();
    }

    /// <summary>Creates the file if it doesn't exist in the parent folder, otherwise updates its content.</summary>
    public static async Task<string> UploadOrUpdateFileAsync(
        DriveService drive, string name, string parentId, Stream content, string mimeType, CancellationToken ct)
    {
        var existing = await FindChildAsync(drive, name, parentId, mimeType: null, ct);

        if (existing is null)
        {
            var file = new DriveFile { Name = name, Parents = [parentId] };
            var createRequest = drive.Files.Create(file, content, mimeType);
            createRequest.Fields = "id";
            var progress = await createRequest.UploadAsync(ct);
            ThrowIfFailed(progress);
            return createRequest.ResponseBody.Id;
        }
        else
        {
            var updateRequest = drive.Files.Update(new DriveFile(), existing.Id, content, mimeType);
            var progress = await updateRequest.UploadAsync(ct);
            ThrowIfFailed(progress);
            return existing.Id;
        }
    }

    public static async Task<Stream> DownloadFileAsync(DriveService drive, string fileId, CancellationToken ct)
    {
        var memoryStream = new MemoryStream();
        await drive.Files.Get(fileId).DownloadAsync(memoryStream, ct);
        memoryStream.Position = 0;
        return memoryStream;
    }

    public static async Task TrashFileAsync(DriveService drive, string fileId, CancellationToken ct)
    {
        var file = new DriveFile { Trashed = true };
        await drive.Files.Update(file, fileId).ExecuteAsync(ct);
    }

    private static void ThrowIfFailed(IUploadProgress progress)
    {
        if (progress.Status == UploadStatus.Failed)
        {
            throw progress.Exception ?? new IOException("Google Drive upload failed.");
        }
    }
}
