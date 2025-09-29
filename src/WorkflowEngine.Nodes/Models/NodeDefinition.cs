namespace WorkflowEngine.Nodes.Models;

public class NodeDefinition
{
    public required string Type { get; init; }
    public required string Name { get; init; }
    public required string Category { get; init; }
    public string? Description { get; init; }
    public string? Icon { get; init; }
    public string? Documentation { get; init; }
    public List<NodeOperation> Operations { get; init; } = new();
    public List<string>? RequiredCredentials { get; init; }
    public Dictionary<string, object>? Defaults { get; init; }
}