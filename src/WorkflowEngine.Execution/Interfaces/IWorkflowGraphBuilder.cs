using WorkflowEngine.Application.DTOs.Workflow;
using WorkflowEngine.Execution.Models;

namespace WorkflowEngine.Execution.Interfaces;

public interface IWorkflowGraphBuilder
{
    WorkflowGraph BuildGraph(List<WorkflowNodeDto> nodes, List<NodeConnectionDto> connections);
    Task<bool> ValidateGraphAsync(WorkflowGraph graph, CancellationToken cancellationToken = default);
}