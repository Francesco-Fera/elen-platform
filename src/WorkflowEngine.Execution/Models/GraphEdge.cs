namespace WorkflowEngine.Execution.Models;

public class GraphEdge
{
    public required string SourceNodeId { get; init; }
    public required string TargetNodeId { get; init; }
    public string SourceOutput { get; init; } = "default";
    public string TargetInput { get; init; } = "default";
}