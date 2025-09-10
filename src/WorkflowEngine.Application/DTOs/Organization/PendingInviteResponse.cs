namespace WorkflowEngine.Application.DTOs.Organization;

public class PendingInviteResponse
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string InvitedByEmail { get; set; } = string.Empty;
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
}
