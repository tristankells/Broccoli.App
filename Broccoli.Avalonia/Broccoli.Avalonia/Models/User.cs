using System.Text.Json.Serialization;

namespace Broccoli.Avalonia.Models;

public class User
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("passwordHash")]
    public string PasswordHash { get; set; } = string.Empty;

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("lastLoginAt")]
    public DateTime? LastLoginAt { get; set; }

    // Partition key for CosmosDB
    [JsonPropertyName("partitionKey")]
    public string PartitionKey { get; set; } = "user";
}
