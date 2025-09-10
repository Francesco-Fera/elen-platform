using WorkflowEngine.Core.Enums;

namespace WorkflowEngine.Core.Entities;

public class OrganizationMember
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrganizationId { get; set; }
    public Guid UserId { get; set; }

    public OrganizationRole Role { get; set; } = OrganizationRole.Member;
    public bool IsActive { get; set; } = true;

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public Guid? InvitedBy { get; set; }

    // Navigation properties
    public virtual Organization Organization { get; set; } = null!;
    public virtual User User { get; set; } = null!;
    public virtual User? Inviter { get; set; }
}
