using WorkflowEngine.Application.DTOs.Workflow;
using WorkflowEngine.Execution.Interfaces;
using WorkflowEngine.Execution.Models;
using WorkflowEngine.Nodes.Models;

namespace WorkflowEngine.Execution.Engine;

public class ExecutionContextFactory : IExecutionContextFactory
{
    public Dictionary<string, object> CreateWorkflowContext(
        Guid executionId,
        Guid workflowId,
        string workflowName,
        Dictionary<string, object>? inputData)
    {
        var context = new Dictionary<string, object>
        {
            ["$execution"] = new Dictionary<string, object>
            {
                ["id"] = executionId,
                ["workflowId"] = workflowId,
                ["workflowName"] = workflowName,
                ["startedAt"] = DateTime.UtcNow
            }
        };

        if (inputData != null && inputData.Count > 0)
        {
            context["$input"] = inputData;
        }
        else
        {
            context["$input"] = new Dictionary<string, object>();
        }

        return context;
    }

    public Dictionary<string, object> GetInputDataForNode(
        WorkflowNode node,
        Dictionary<string, object> workflowContext,
        List<NodeConnectionDto> connections)
    {
        var inputData = new Dictionary<string, object>();

        var incomingConnections = connections
            .Where(c => c.TargetNodeId == node.Id)
            .ToList();

        if (incomingConnections.Count == 0)
        {
            if (workflowContext.TryGetValue("$input", out var globalInput) &&
                globalInput is Dictionary<string, object> inputDict)
            {
                foreach (var kvp in inputDict)
                {
                    inputData[kvp.Key] = kvp.Value;
                }
            }
            return inputData;
        }

        foreach (var connection in incomingConnections)
        {
            var sourceContextKey = $"$node.{connection.SourceNodeId}";

            if (!workflowContext.TryGetValue(sourceContextKey, out var sourceData))
                continue;

            if (sourceData is not Dictionary<string, object> sourceDict)
                continue;

            if (!sourceDict.TryGetValue("data", out var nodeOutputData))
                continue;

            if (nodeOutputData is not Dictionary<string, object> outputDict)
                continue;

            if (connection.SourceOutput == "default")
            {
                foreach (var kvp in outputDict)
                {
                    inputData[kvp.Key] = kvp.Value;
                }
            }
            else
            {
                if (outputDict.TryGetValue(connection.SourceOutput, out var specificOutput))
                {
                    inputData[connection.TargetInput] = specificOutput;
                }
            }
        }

        return inputData;
    }

    public void UpdateContextWithNodeOutput(
        Dictionary<string, object> workflowContext,
        string nodeId,
        NodeExecutionResult result)
    {
        var nodeContext = new Dictionary<string, object>
        {
            ["success"] = result.Success,
            ["data"] = result.OutputData,
            ["executedAt"] = DateTime.UtcNow
        };

        if (!result.Success && result.ErrorMessage != null)
        {
            nodeContext["error"] = result.ErrorMessage;
        }

        if (result.Metadata.Count > 0)
        {
            nodeContext["metadata"] = result.Metadata;
        }

        if (result.OutputData.TryGetValue("conditionalOutput", out var conditionalOutput))
        {
            nodeContext["conditionalOutput"] = conditionalOutput;
        }

        workflowContext[$"$node.{nodeId}"] = nodeContext;
    }
}