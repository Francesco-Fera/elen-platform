namespace WorkflowEngine.Nodes.Models;

public class NodeParameter
{
    public required string Name { get; init; }
    public required string DisplayName { get; init; }
    public required string Type { get; init; }
    public bool Required { get; init; }
    public object? DefaultValue { get; init; }
    public string? Description { get; init; }
    public string? Placeholder { get; init; }
    public List<SelectOption>? Options { get; init; }
    public bool SupportsExpression { get; init; } = true;
}