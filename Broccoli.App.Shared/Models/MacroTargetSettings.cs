using System.Text.Json.Serialization;

namespace Broccoli.Data.Models;

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

    [JsonPropertyName("goal")]
    public MacroGoal Goal { get; set; } = MacroGoal.Maintain;

    /// <summary>
    /// Calorie delta applied on top of TDEE. Negative = deficit, positive = surplus.
    /// Default 0 (maintain). Typical: Lose = -500, Gain = +250.
    /// </summary>
    [JsonPropertyName("goalCalorieDelta")]
    public int GoalCalorieDelta { get; set; } = 0;

    [JsonPropertyName("unitSystem")]
    public UnitSystem UnitSystem { get; set; } = UnitSystem.Metric;

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public enum BmrFormula
{
    MifflinStJeor = 0,
    HarrisBenedict = 1
}

public enum ProteinMethod
{
    RatioPercent = 0,
    GramsPerKg = 1
}

public enum MacroGoal
{
    Maintain = 0,
    Lose = 1,
    Gain = 2
}

public enum UnitSystem
{
    Metric = 0,
    Imperial = 1
}

public enum GenderType
{
    Male = 0,
    Female = 1,
    Other = 2
}

public enum ActivityLevel
{
    Sedentary = 0,
    LightlyActive = 1,
    ModeratelyActive = 2,
    VeryActive = 3,
    ExtraActive = 4
}

