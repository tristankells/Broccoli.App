namespace Broccoli.App.Shared.Configuration;

/// <summary>
/// Feature flag settings. Loaded from the "FeatureFlags" config section.
/// </summary>
public class FeatureFlagsSettings
{
    public const string SectionName = "FeatureFlags";

    /// <summary>
    /// Enables inline CRUD editing and USDA import on the Food Database page.
    /// Should only be true in Development / DEBUG builds.
    /// </summary>
    public bool FoodDatabaseEditing { get; set; } = false;
}

