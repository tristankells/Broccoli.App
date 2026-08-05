namespace Broccoli.Avalonia.Storage;

/// <summary>
/// Resolves and bootstraps the local, platform-appropriate application data folder.
/// Nothing here requires any user setup — the folder structure is created automatically
/// on first access so the app works fully offline from the very first launch.
///
/// Layout:
///   {AppData}/Broccoli/
///       broccoli.db      (SQLite: everything except Recipes; see <see cref="BroccoliDbContext"/>)
///       Recipes/
///           {recipe-id}/
///               recipe.md
///               *.jpg|png (recipe images)
/// </summary>
public static class AppPaths
{
    private const string AppFolderName = "Broccoli";
    private const string DatabaseFileName = "broccoli.db";
    private const string RecipesFolderName = "Recipes";

    /// <summary>
    /// The root app-data folder, e.g. %LocalAppData%\Broccoli on Windows,
    /// ~/Library/Application Support/Broccoli on macOS, ~/.local/share/Broccoli on Linux.
    /// Created on first access if it doesn't already exist.
    /// </summary>
    public static string RootFolder
    {
        get
        {
            string root = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                AppFolderName);
            Directory.CreateDirectory(root);
            return root;
        }
    }

    /// <summary>Full path to the SQLite database file.</summary>
    public static string DatabaseFilePath => Path.Combine(RootFolder, DatabaseFileName);

    /// <summary>
    /// Root folder containing one sub-folder per recipe (Markdown + images).
    /// Created on first access if it doesn't already exist.
    /// </summary>
    public static string RecipesFolder
    {
        get
        {
            string folder = Path.Combine(RootFolder, RecipesFolderName);
            Directory.CreateDirectory(folder);
            return folder;
        }
    }

    /// <summary>Folder for a specific recipe's markdown file and images.</summary>
    public static string RecipeFolder(string recipeId)
    {
        string folder = Path.Combine(RecipesFolder, recipeId);
        Directory.CreateDirectory(folder);
        return folder;
    }

    /// <summary>Full path to a specific recipe's markdown file.</summary>
    public static string RecipeMarkdownFilePath(string recipeId) =>
        Path.Combine(RecipeFolder(recipeId), "recipe.md");

    /// <summary>
    /// Folder where the Google OAuth token cache is stored (managed by <c>Google.Apis.Auth</c>'s
    /// <c>FileDataStore</c>). Deleting this folder fully signs the user out.
    /// </summary>
    public static string GoogleDriveTokenFolder
    {
        get
        {
            string folder = Path.Combine(RootFolder, "GoogleDriveTokens");
            Directory.CreateDirectory(folder);
            return folder;
        }
    }

    /// <summary>Small JSON file recording which Google account (if any) is connected for backup.</summary>
    public static string GoogleDriveAccountFilePath => Path.Combine(RootFolder, "google-drive-account.json");

    /// <summary>
    /// User-editable JSON file holding the Google OAuth client id/secret used for the Drive
    /// backup "installed app" login flow. Not baked into the app: users/operators who want to
    /// enable Drive backup provide their own OAuth client registered in Google Cloud Console.
    /// </summary>
    public static string GoogleDriveOAuthConfigFilePath => Path.Combine(RootFolder, "google-drive-oauth.json");

    /// <summary>
    /// Local record of this device's sync progress (last synced manifest version, Drive folder
    /// ids, this device's random id). See <c>Services.Sync.SyncState</c>.
    /// </summary>
    public static string SyncStateFilePath => Path.Combine(RootFolder, "sync-state.json");

    /// <summary>
    /// Local (merged-with-remote) list of deleted recipe ids, so deletions propagate between
    /// devices instead of a device with a stale local copy silently re-uploading a "deleted" recipe.
    /// </summary>
    public static string TombstonesFilePath => Path.Combine(RootFolder, "tombstones.json");

    /// <summary>Folder used to stage/keep conflict copies (recipes and/or the database) that need user resolution.</summary>
    public static string ConflictsFolder
    {
        get
        {
            string folder = Path.Combine(RootFolder, "Conflicts");
            Directory.CreateDirectory(folder);
            return folder;
        }
    }
}
