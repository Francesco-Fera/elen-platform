using WorkflowEngine.Execution.Models;
using WorkflowEngine.Nodes.Models;

namespace WorkflowEngine.Execution.Interfaces;

public interface INodeExecutor
{
    Task<NodeExecutionResult> ExecuteNodeAsync(
        WorkflowNode node,
        NodeExecutionContext context,
        CancellationToken cancellationToken = default);
}