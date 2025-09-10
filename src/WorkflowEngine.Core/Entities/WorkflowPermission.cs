using WorkflowEngine.Core.Enums;

namespace WorkflowEngine.Core.Entities;

public class WorkflowPermission
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WorkflowId { get; set; }
    public Guid UserId { get; set; }

    public WorkflowPermissionType Permission { get; set; } = WorkflowPermissionType.View;
    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;
    public Guid GrantedBy { get; set; }

    // Navigation properties
    public virtual Workflow Workflow { get; set; } = null!;
    public virtual User User { get; set; } = null!;
    public virtual User Granter { get; set; } = null!;
}
