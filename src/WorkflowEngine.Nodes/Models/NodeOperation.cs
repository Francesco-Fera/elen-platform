namespace WorkflowEngine.Nodes.Models;

public class NodeOperation
{
    public required string Name { get; init; }
    public required string DisplayName { get; init; }
    public string? Description { get; init; }
    public List<NodeParameter> Parameters { get; init; } = new();
}
