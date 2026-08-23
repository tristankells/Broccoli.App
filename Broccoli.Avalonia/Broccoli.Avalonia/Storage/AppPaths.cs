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

    /// <summary>Sub-folder name inside a recipe's folder that holds its ingredient-history snapshots.</summary>
    public const string RecipeHistoryFolderName = "history";

    private static string? _rootOverride;

    /// <summary>
    /// Redirects the app to an alternative data folder (e.g. a throwaway directory used by
    /// end-to-end tests so they never touch the real user data). Must be called before any
    /// path is first resolved. The desktop host wires this up from the <c>--appdata</c>
    /// command-line argument.
    /// </summary>
    public static void OverrideRootFolder(string rootFolder) => _rootOverride = rootFolder;

    /// <summary>
    /// The root app-data folder, e.g. %LocalAppData%\Broccoli on Windows,
    /// ~/Library/Application Support/Broccoli on macOS, ~/.local/share/Broccoli on Linux.
    /// Created on first access if it doesn't already exist.
    /// </summary>
    public static string RootFolder
    {
        get
        {
            string root = _rootOverride ?? Path.Combine(
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
    /// Optional user-editable JSON file that can override the embedded Google OAuth client id used
    /// for the Drive backup "installed app" login flow. Not required for normal use — the default
    /// client id ships in the app binary (see <c>GoogleDriveAuthService.DefaultClientId</c>).
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
    /// Folder holding a recipe's ingredient-history snapshots (one Markdown file per captured
    /// version). Not created eagerly — it only appears once the first snapshot is written.
    /// </summary>
    public static string RecipeHistoryFolder(string recipeId) =>
        Path.Combine(RecipeFolder(recipeId), RecipeHistoryFolderName);
}
