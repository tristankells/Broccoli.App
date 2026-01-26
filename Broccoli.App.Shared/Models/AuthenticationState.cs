namespace Broccoli.App.Shared.Models;

public class AuthenticationState
{
    public bool IsAuthenticated { get; set; }
    public string? Username { get; set; }
    public string? UserId { get; set; }
    public DateTime? LoginTime { get; set; }
}
