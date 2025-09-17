using WorkflowEngine.Core.Enums;

namespace WorkflowEngine.Application.DTOs.Execution;

public class ExecutionResponse
{
    public Guid Id { get; set; }
    public Guid WorkflowId { get; set; }
    public string WorkflowName { get; set; } = string.Empty;
    public ExecutionStatus Status { get; set; }
    public ExecutionTrigger TriggerType { get; set; }

    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int? DurationMs { get; set; }

    public Dictionary<string, object>? InputData { get; set; }
    public Dictionary<string, object>? OutputData { get; set; }

    public string? ErrorMessage { get; set; }
    public Dictionary<string, object>? ErrorDetails { get; set; }

    public Guid? ExecutedBy { get; set; }
    public string? ExecutorName { get; set; }

    // Node executions (optional for detailed view)
    public List<NodeExecutionResponse>? NodeExecutions { get; set; }
}