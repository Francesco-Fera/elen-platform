namespace WorkflowEngine.Execution.Models;

public class NodeExecutionSummary
{
    public required string NodeId { get; init; }
    public required string NodeType { get; init; }
    public required string NodeName { get; init; }
    public bool Success { get; init; }
    public TimeSpan Duration { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
}