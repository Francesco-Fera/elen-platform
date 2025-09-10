namespace WorkflowEngine.Application.DTOs.Auth;

public class AuthResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public Core.Entities.User User { get; set; } = null!;
    public Core.Entities.Organization? CurrentOrganization { get; set; }
}