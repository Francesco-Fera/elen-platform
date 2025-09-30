using WorkflowEngine.Application.DTOs.Workflow;
using WorkflowEngine.Execution.Models;
using WorkflowEngine.Nodes.Models;

namespace WorkflowEngine.Execution.Interfaces;

public interface IExecutionContextFactory
{
    Dictionary<string, object> CreateWorkflowContext(Guid executionId, Guid workflowId, string workflowName, Dictionary<string, object>? inputData);

    Dictionary<string, object> GetInputDataForNode(WorkflowNode node, Dictionary<string, object> workflowContext, List<NodeConnectionDto> connections);

    void UpdateContextWithNodeOutput(Dictionary<string, object> workflowContext, string nodeId, NodeExecutionResult result);
}