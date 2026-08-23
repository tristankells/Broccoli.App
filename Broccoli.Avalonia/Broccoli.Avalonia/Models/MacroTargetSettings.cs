using System.Text.Json.Serialization;

namespace Broccoli.Avalonia.Models;

public enum BmrFormula
{
    MifflinStJeor = 0,
    HarrisBenedict = 1,
}

public enum ProteinMethod
{
    RatioPercent = 0,
    GramsPerKg = 1,
}

public enum MacroGoal
{
    Maintain = 0,
    Lose = 1,
    Gain = 2,
}

public enum UnitSystem
{
    Metric = 0,
    Imperial = 1,
}

public enum GenderType
{
    Male = 0,
    Female = 1,
    Other = 2,
}

public enum ActivityLevel
{
    Sedentary = 0,
    LightlyActive = 1,
    ModeratelyActive = 2,
    VeryActive = 3,
    ExtraActive = 4,
}

/// <summary>How the recipe auto-balance dialog computes ingredient adjustments.</summary>
public enum AutoBalanceStrategy
{
    /// <summary>Scale the leading contributor of each selected macro to close that macro's gap.</summary>
    IndependentSinglePass = 0,

    /// <summary>Solve a linear system over the leading contributors to hit all targets exactly.</summary>
    LinearSolve = 1,
}

public class MacroTargetSettings
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("partitionKey")]
    public string PartitionKey { get; set; } = "macrotargetsettings";

    [JsonPropertyName("bmrFormula")]
    public BmrFormula BmrFormula { get; set; } = BmrFormula.MifflinStJeor;

    [JsonPropertyName("proteinMethod")]
    public ProteinMethod ProteinMethod { get; set; } = ProteinMethod.RatioPercent;

    /// <summary>Percentage of recommended calories allocated to protein (used when ProteinMethod = RatioPercent).</summary>
    [JsonPropertyName("proteinPercent")]
    public double ProteinPercent { get; set; } = 30;

    /// <summary>Percentage of recommended calories allocated to carbohydrates.</summary>
    [JsonPropertyName("carbPercent")]
    public double CarbPercent { get; set; } = 40;

    /// <summary>Percentage of recommended calories allocated to fat.</summary>
    [JsonPropertyName("fatPercent")]
    public double FatPercent { get; set; } = 30;

    /// <summary>Grams of protein per kg of bodyweight (used when ProteinMethod = GramsPerKg).</summary>
    [JsonPropertyName("proteinGramsPerKg")]
    public double ProteinGramsPerKg { get; set; } = 1.8;

    [JsonPropertyName("unitSystem")]
    public UnitSystem UnitSystem { get; set; } = UnitSystem.Metric;

    /// <summary>When true, the Recipe Detail page shows a meal macro comparison panel.</summary>
    [JsonPropertyName("recipeMealComparisonEnabled")]
    public bool RecipeMealComparisonEnabled { get; set; } = false;

    /// <summary>The MacroTarget.Id of the profile to compare against on the Recipe Detail page.</summary>
    [JsonPropertyName("recipeMealComparisonPersonId")]
    public string RecipeMealComparisonPersonId { get; set; } = string.Empty;

    /// <summary>Strategy used by the recipe edit page's auto-balance feature.</summary>
    [JsonPropertyName("autoBalanceStrategy")]
    public AutoBalanceStrategy AutoBalanceStrategy { get; set; } = AutoBalanceStrategy.IndependentSinglePass;

    [JsonPropertyName("showCardImage")]
    public bool ShowCardImage { get; set; } = true;

    [JsonPropertyName("showCardTags")]
    public bool ShowCardTags { get; set; } = true;

    [JsonPropertyName("showCardSeasonality")]
    public bool ShowCardSeasonality { get; set; } = true;

    [JsonPropertyName("showCardNutrition")]
    public bool ShowCardNutrition { get; set; } = true;

    [JsonPropertyName("showCardCalorieMatch")]
    public bool ShowCardCalorieMatch { get; set; } = false;

    /// <summary>When false, the Seasonality tab is hidden from the navigation drawer.</summary>
    [JsonPropertyName("showSeasonalityNavItem")]
    public bool ShowSeasonalityNavItem { get; set; } = true;

    [JsonPropertyName("calorieMatchTolerancePercent")]
    public double CalorieMatchTolerancePercent { get; set; } = 15;

    /// <summary>
    /// Maximum number of ingredient history snapshots to retain per recipe. The oldest
    /// (first) snapshot is always kept, so this value means "first + (N-1) most recent".
    /// </summary>
    [JsonPropertyName("recipeHistoryBackupCount")]
    public int RecipeHistoryBackupCount { get; set; } = 10;

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
