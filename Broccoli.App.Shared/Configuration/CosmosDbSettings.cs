namespace Broccoli.App.Shared.Configuration;

/// <summary>
/// Configuration settings for CosmosDB connection.
/// Supports both emulator (development) and Azure CosmosDB (production).
/// </summary>
public class CosmosDbSettings
{
    public const string SectionName = "CosmosDb";
    
    /// <summary>
    /// CosmosDB endpoint URI (e.g., https://your-account.documents.azure.com:443/)
    /// </summary>
    public string EndpointUri { get; set; } = "https://localhost:8081";
    
    /// <summary>
    /// CosmosDB primary key for authentication
    /// </summary>
    public string PrimaryKey { get; set; } = string.Empty;
    
    /// <summary>
    /// Database name to use
    /// </summary>
    public string DatabaseName { get; set; } = "BroccoliAppDb";
    
    /// <summary>
    /// Optional: Full connection string (takes precedence over EndpointUri + PrimaryKey)
    /// </summary>
    public string? ConnectionString { get; set; }
    
    /// <summary>
    /// Whether to bypass SSL certificate validation (use for emulator only)
    /// </summary>
    public bool BypassSslValidation { get; set; } = false;
    
    /// <summary>
    /// Gets the connection string, either from ConnectionString property or builds from URI + Key
    /// </summary>
    public string GetConnectionString()
    {
        if (!string.IsNullOrEmpty(ConnectionString))
            return ConnectionString;
            
        return $"AccountEndpoint={EndpointUri};AccountKey={PrimaryKey};";
    }
    
    /// <summary>
    /// Determines if this is the emulator based on endpoint URI
    /// </summary>
    public bool IsEmulator()
    {
        return EndpointUri.Contains("localhost", StringComparison.OrdinalIgnoreCase) ||
               EndpointUri.Contains("127.0.0.1");
    }
}
