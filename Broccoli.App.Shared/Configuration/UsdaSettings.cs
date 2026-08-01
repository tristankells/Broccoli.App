namespace Broccoli.App.Shared.Configuration;

/// <summary>
/// Settings for the USDA FoodData Central API.
/// </summary>
public class UsdaSettings
{
    public const string SectionName = "Usda";

    /// <summary>FDC API key — obtain from https://fdc.nal.usda.gov/api-key-signup.html</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Base URL for the FDC v1 API, without a trailing slash.</summary>
    public string BaseUrl { get; set; } = "https://api.nal.usda.gov/fdc/v1";
}

