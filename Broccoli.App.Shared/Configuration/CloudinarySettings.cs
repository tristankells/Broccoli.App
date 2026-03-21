namespace Broccoli.App.Shared.Configuration;

/// <summary>
/// Configuration settings for Cloudinary image storage.
/// Values are found in Cloudinary Dashboard → Settings → API Keys.
/// </summary>
public class CloudinarySettings
{
    public const string SectionName = "Cloudinary";

    /// <summary>Your Cloudinary cloud name, e.g. "myapp"</summary>
    public string CloudName { get; set; } = string.Empty;

    /// <summary>Your Cloudinary API key</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Your Cloudinary API secret</summary>
    public string ApiSecret { get; set; } = string.Empty;

    /// <summary>
    /// Base folder inside your Cloudinary media library where recipe images are stored.
    /// Images are nested as {Folder}/{recipeId}/{guid}
    /// </summary>
    public string Folder { get; set; } = "recipes";
}

