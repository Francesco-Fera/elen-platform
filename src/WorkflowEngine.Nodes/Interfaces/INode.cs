using WorkflowEngine.Nodes.Models;

namespace WorkflowEngine.Nodes.Interfaces;

public interface INode
{
    string Type { get; }
    string Name { get; }
    string Category { get; }
    NodeDefinition GetDefinition();
    Task<NodeExecutionResult> ExecuteAsync(NodeExecutionContext context);
}