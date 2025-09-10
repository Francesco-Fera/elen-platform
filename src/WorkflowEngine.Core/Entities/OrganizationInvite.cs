using System.ComponentModel.DataAnnotations;
using WorkflowEngine.Core.Enums;

namespace WorkflowEngine.Core.Entities;

public class OrganizationInvite
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrganizationId { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    public OrganizationRole Role { get; set; } = OrganizationRole.Member;
    public string InviteToken { get; set; } = string.Empty;

    public InviteStatus Status { get; set; } = InviteStatus.Pending;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid InvitedBy { get; set; }
    public Guid? AcceptedBy { get; set; }
    public DateTime? AcceptedAt { get; set; }

    // Navigation properties
    public virtual Organization Organization { get; set; } = null!;
    public virtual User Inviter { get; set; } = null!;
    public virtual User? AcceptedByUser { get; set; }
}
