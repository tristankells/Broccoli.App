using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Broccoli.App.Shared.Configuration;
using Microsoft.Extensions.Logging;

namespace Broccoli.App.Shared.Services;

/// <summary>
/// Uploads and deletes recipe images using the Cloudinary SDK.
/// Images are stored at: {settings.Folder}/{recipeId}/{guid}
/// </summary>
public class CloudinaryImageService : IRecipeImageService
{
    private readonly Cloudinary _cloudinary;
    private readonly CloudinarySettings _settings;
    private readonly ILogger<CloudinaryImageService> _logger;

    public CloudinaryImageService(CloudinarySettings settings, ILogger<CloudinaryImageService> logger)
    {
        _settings = settings;
        _logger = logger;

        var account = new Account(settings.CloudName, settings.ApiKey, settings.ApiSecret);
        _cloudinary = new Cloudinary(account) { Api = { Secure = true } };
    }

    /// <inheritdoc />
    public async Task<string> UploadAsync(Stream imageStream, string fileName, string recipeId)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        if (ext is not ".jpg" and not ".jpeg" and not ".png")
            throw new InvalidOperationException("Only JPG/PNG images are supported.");

        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(fileName, imageStream),
            Folder = $"{_settings.Folder}/{recipeId}",
            PublicId = Guid.NewGuid().ToString()
        };

        var result = await _cloudinary.UploadAsync(uploadParams);

        if (result.Error != null)
        {
            _logger.LogError("Cloudinary upload failed: {Message}", result.Error.Message);
            throw new InvalidOperationException($"Image upload failed: {result.Error.Message}");
        }

        _logger.LogInformation("Uploaded image to Cloudinary: {PublicId}", result.PublicId);
        return result.SecureUrl.ToString();
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string imageUrl)
    {
        var publicId = ExtractPublicId(imageUrl);
        if (string.IsNullOrEmpty(publicId))
        {
            _logger.LogWarning("Could not extract Cloudinary public ID from URL: {Url}", imageUrl);
            return;
        }

        var result = await _cloudinary.DestroyAsync(new DeletionParams(publicId));

        if (result.Result == "ok")
            _logger.LogInformation("Deleted image from Cloudinary: {PublicId}", publicId);
        else
            _logger.LogWarning("Cloudinary delete returned '{Result}' for public ID: {PublicId}", result.Result, publicId);
    }

    /// <summary>
    /// Extracts the Cloudinary public ID from a secure URL.
    /// URL format: https://res.cloudinary.com/{cloud_name}/image/upload/[v{version}/]{public_id}.{ext}
    /// </summary>
    private static string ExtractPublicId(string imageUrl)
    {
        try
        {
            var uri = new Uri(imageUrl);
            var path = uri.AbsolutePath;

            const string UploadMarker = "/upload/";
            var uploadIndex = path.IndexOf(UploadMarker, StringComparison.OrdinalIgnoreCase);
            if (uploadIndex < 0) return string.Empty;

            var afterUpload = path[(uploadIndex + UploadMarker.Length)..];

            // Strip optional version segment, e.g. "v1234567890/"
            if (afterUpload.Length > 1 && afterUpload[0] == 'v' && char.IsDigit(afterUpload[1]))
            {
                var slashIndex = afterUpload.IndexOf('/');
                if (slashIndex > 0)
                    afterUpload = afterUpload[(slashIndex + 1)..];
            }

            // Strip file extension
            var dotIndex = afterUpload.LastIndexOf('.');
            if (dotIndex > 0)
                afterUpload = afterUpload[..dotIndex];

            return afterUpload;
        }
        catch
        {
            return string.Empty;
        }
    }
}

