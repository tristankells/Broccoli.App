using System.Text.Json.Serialization;

namespace Broccoli.Data.Models;

public class GroceryListItem
{
    /// <summary>
    /// Unique identifier of the grocery list item
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Name / description of the grocery item (e.g. "2 cups Flour", "1 drizzle of oil")
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Whether the item has been purchased / checked off
    /// </summary>
    [JsonPropertyName("isChecked")]
    public bool IsChecked { get; set; } = false;

    /// <summary>
    /// ID of the user who owns this grocery list item
    /// </summary>
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Partition key for CosmosDB (always "user")
    /// </summary>
    [JsonPropertyName("partitionKey")]
    public string PartitionKey { get; set; } = "user";

    /// <summary>
    /// Timestamp when the item was added to the grocery list
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

