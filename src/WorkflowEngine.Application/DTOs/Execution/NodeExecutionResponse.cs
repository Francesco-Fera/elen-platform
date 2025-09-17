using WorkflowEngine.Core.Enums;

namespace WorkflowEngine.Application.DTOs.Execution;

public class NodeExecutionResponse
{
    public string NodeId { get; set; } = string.Empty;
    public string NodeType { get; set; } = string.Empty;
    public string NodeName { get; set; } = string.Empty;
    public ExecutionStatus Status { get; set; }

    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int? DurationMs { get; set; }

    public Dictionary<string, object>? InputData { get; set; }
    public Dictionary<string, object>? OutputData { get; set; }

    public string? ErrorMessage { get; set; }
    public Dictionary<string, object>? ErrorDetails { get; set; }

    public int ExecutionOrder { get; set; }
    public int RetryCount { get; set; }
}
