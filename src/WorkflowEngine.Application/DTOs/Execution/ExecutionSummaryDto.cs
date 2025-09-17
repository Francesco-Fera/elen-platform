using WorkflowEngine.Core.Enums;

namespace WorkflowEngine.Application.DTOs.Execution;

public class ExecutionSummaryDto
{
    public Guid Id { get; set; }
    public Guid WorkflowId { get; set; }
    public string WorkflowName { get; set; } = string.Empty;
    public ExecutionStatus Status { get; set; }
    public ExecutionTrigger TriggerType { get; set; }

    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int? DurationMs { get; set; }

    public string? ErrorMessage { get; set; }
    public Guid? ExecutedBy { get; set; }
    public string? ExecutorName { get; set; }
}