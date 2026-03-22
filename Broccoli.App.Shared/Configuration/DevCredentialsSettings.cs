namespace Broccoli.App.Shared.Configuration;

/// <summary>
/// Development-only credentials that are auto-submitted on the Login page.
/// Populate the "DevCredentials" section in appsettings.Development.json.
/// When Username is empty (the default), auto-login is skipped entirely.
/// Never set these values in appsettings.json or appsettings.Production.json.
/// </summary>
public class DevCredentialsSettings
{
    public const string SectionName = "DevCredentials";

    /// <summary>
    /// Development username to auto-login with. Leave empty to disable auto-login.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Development password to auto-login with.
    /// </summary>
    public string Password { get; set; } = string.Empty;
}

