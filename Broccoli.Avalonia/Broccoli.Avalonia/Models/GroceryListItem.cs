using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Broccoli.Avalonia.Models;

public partial class GroceryListItem : ObservableObject
{
    [JsonIgnore]
    [ObservableProperty]
    private bool _isEditing;

    [JsonIgnore]
    [ObservableProperty]
    private string _editText = string.Empty;

    /// <summary>
    /// Unique identifier of the grocery list item
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Name / description of the grocery item (e.g. "2 cups Flour", "1 drizzle of oil")
    /// </summary>
    [ObservableProperty]
    [JsonPropertyName("name")]
    private string _name = string.Empty;

    /// <summary>
    /// Whether the item has been purchased / checked off
    /// </summary>
    [ObservableProperty]
    [JsonPropertyName("isChecked")]
    private bool _isChecked;

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

    /// <summary>
    /// Approximate weight/item conversion hint based on the food database.
    /// e.g. "(~305g)" for item-based inputs or "(~5 medium carrots)" for gram-based inputs.
    /// Null when the food cannot be matched or has no meaningful unit conversion.
    /// </summary>
    [ObservableProperty]
    private string? _quantityHint;
}
