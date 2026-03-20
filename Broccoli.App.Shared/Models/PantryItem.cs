using System.Text.Json.Serialization;

namespace Broccoli.Data.Models;

public class PantryItem
{
    /// <summary>
    /// Unique identifier of the pantry item
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Name of the pantry item (e.g. "Salt", "Flour")
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Category of the pantry item: AlwaysHave (staples) or CheckIfHave (optional)
    /// </summary>
    [JsonPropertyName("category")]
    public PantryCategory Category { get; set; } = PantryCategory.CheckIfHave;

    /// <summary>
    /// ID of the user who owns this pantry item
    /// </summary>
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Partition key for CosmosDB (always "user")
    /// </summary>
    [JsonPropertyName("partitionKey")]
    public string PartitionKey { get; set; } = "user";

    /// <summary>
    /// Timestamp when the item was added to the pantry
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum PantryCategory
{
    /// <summary>
    /// Staples the user always has at home (salt, flour, water, sugar, oil, etc.)
    /// These are automatically unchecked when adding recipe ingredients to the grocery list.
    /// </summary>
    AlwaysHave = 0,

    /// <summary>
    /// Items the user may or may not have (ketchup, mustard, specific fruits, etc.)
    /// These are checked by default when adding recipe ingredients to the grocery list.
    /// </summary>
    CheckIfHave = 1
}

