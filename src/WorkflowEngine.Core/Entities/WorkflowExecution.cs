using WorkflowEngine.Core.Enums;

namespace WorkflowEngine.Core.Entities;

public class WorkflowExecution
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WorkflowId { get; set; }
    public Guid? UserId { get; set; }

    public ExecutionStatus Status { get; set; } = ExecutionStatus.Pending;
    public ExecutionTrigger TriggerType { get; set; }

    // JSON data
    public string? InputDataJson { get; set; }
    public string? OutputDataJson { get; set; }
    public string? ErrorDataJson { get; set; }

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public TimeSpan? Duration { get; set; }

    // Navigation properties
    public virtual Workflow Workflow { get; set; } = null!;
    public virtual User? User { get; set; }
    public virtual ICollection<ExecutionLog> Logs { get; set; } = new List<ExecutionLog>();
}
