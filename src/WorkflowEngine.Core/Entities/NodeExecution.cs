using WorkflowEngine.Core.Enums;

namespace WorkflowEngine.Core.Entities;

public class NodeExecution
{
    public Guid Id { get; set; }
    public Guid ExecutionId { get; set; }
    public string NodeId { get; set; } = string.Empty;
    public string NodeType { get; set; } = string.Empty;
    public string? NodeName { get; set; }
    public ExecutionStatus Status { get; set; }

    public string? InputDataJson { get; set; }
    public string? OutputDataJson { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorDetailsJson { get; set; }

    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int? DurationMs { get; set; }
    public int RetryCount { get; set; } = 0;
    public int ExecutionOrder { get; set; }

    // Performance metrics
    public int? MemoryUsageKb { get; set; }
    public int? CpuTimeMs { get; set; }

    // Navigation properties
    public WorkflowExecution Execution { get; set; } = null!;
}