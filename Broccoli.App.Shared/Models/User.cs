using Newtonsoft.Json;

namespace Broccoli.App.Shared.Models;

public class User
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonProperty("username")]
    public string Username { get; set; } = string.Empty;
    
    [JsonProperty("passwordHash")]
    public string PasswordHash { get; set; } = string.Empty;
    
    [JsonProperty("createdAt")]
    public DateTime CreatedAt { get; set; }
    
    [JsonProperty("lastLoginAt")]
    public DateTime? LastLoginAt { get; set; }
    
    // Partition key for CosmosDB
    [JsonProperty("partitionKey")]
    public string PartitionKey { get; set; } = "user";
}
