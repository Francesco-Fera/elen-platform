namespace WorkflowEngine.Execution.Models;

public class WorkflowNode
{
    public required string Id { get; init; }
    public required string Type { get; init; }
    public required string Name { get; init; }
    public Dictionary<string, object> Parameters { get; init; } = new();
    public Dictionary<string, object> Configuration { get; init; } = new();
}