namespace Broccoli.App.Shared.Slices.Recipes;

/// <summary>
/// Service for uploading and deleting recipe images in Supabase Storage.
/// </summary>
public interface IRecipeImageService
{
    /// <summary>
    /// Uploads an image for a recipe and returns the public CDN URL.
    /// </summary>
    /// <param name="imageStream">The image file stream.</param>
    /// <param name="fileName">Original file name, used to determine content type.</param>
    /// <param name="recipeId">The recipe ID, used to namespace the blob path.</param>
    /// <returns>Public URL of the uploaded image.</returns>
    Task<string> UploadAsync(Stream imageStream, string fileName, string recipeId);

    /// <summary>
    /// Deletes an image from Supabase Storage by its public URL.
    /// Fails silently if the URL is not a recognised Supabase URL.
    /// </summary>
    /// <param name="imageUrl">The public URL previously returned by UploadAsync.</param>
    Task DeleteAsync(string imageUrl);
}

