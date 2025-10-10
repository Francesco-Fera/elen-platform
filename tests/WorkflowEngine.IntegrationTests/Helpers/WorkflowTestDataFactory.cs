using System.Text.Json;
using WorkflowEngine.Application.DTOs.Workflow;
using WorkflowEngine.Core.Entities;
using WorkflowEngine.Core.Enums;

namespace WorkflowEngine.IntegrationTests.Helpers;

public static class WorkflowTestDataFactory
{
    public static Workflow CreateLinearWorkflow(Guid organizationId, Guid userId, string name = "Linear Test Workflow")
    {
        var triggerId = "trigger-1";
        var setVarId = "setvar-1";
        var httpId = "http-1";

        var nodes = new List<WorkflowNodeDto>
        {
            CreateManualTriggerNode(triggerId, "Start", 100, 100),
            CreateSetVariableNode(setVarId, "Set Test Var", 300, 100, new Dictionary<string, object>
            {
                ["variables"] = new List<Dictionary<string, object>>
                {
                    new() { ["name"] = "testValue", ["value"] = "linear-test" }
                }
            }),
            CreateHttpRequestNode(httpId, "HTTP Call", 500, 100, "https://api.example.com/test", "GET")
        };

        var connections = new List<NodeConnectionDto>
        {
            CreateConnection(triggerId, setVarId),
            CreateConnection(setVarId, httpId)
        };

        return CreateWorkflow(organizationId, userId, name, nodes, connections);
    }

    public static Workflow CreateConditionalWorkflow(Guid organizationId, Guid userId, string name = "Conditional Test Workflow")
    {
        var triggerId = "trigger-1";
        var ifId = "if-1";
        var trueBranchId = "true-branch-1";
        var falseBranchId = "false-branch-1";

        var nodes = new List<WorkflowNodeDto>
        {
            CreateManualTriggerNode(triggerId, "Start", 100, 100),
            CreateIfConditionNode(ifId, "Check Value", 300, 100, "{{$input.testValue}}", "equals", "true"),
            CreateSetVariableNode(trueBranchId, "True Branch", 500, 50, new Dictionary<string, object>
            {
                ["variables"] = new List<Dictionary<string, object>>
                {
                    new() { ["name"] = "branch", ["value"] = "true" }
                }
            }),
            CreateSetVariableNode(falseBranchId, "False Branch", 500, 150, new Dictionary<string, object>
            {
                ["variables"] = new List<Dictionary<string, object>>
                {
                    new() { ["name"] = "branch", ["value"] = "false" }
                }
            })
        };

        var connections = new List<NodeConnectionDto>
        {
            CreateConnection(triggerId, ifId),
            CreateConnection(ifId, trueBranchId, "true", "default"),
            CreateConnection(ifId, falseBranchId, "false", "default")
        };

        return CreateWorkflow(organizationId, userId, name, nodes, connections);
    }

    public static Workflow CreateFailingWorkflow(Guid organizationId, Guid userId, string name = "Failing Test Workflow")
    {
        var triggerId = "trigger-1";
        var setVarId = "setvar-1";
        var httpFailId = "http-fail-1";

        var nodes = new List<WorkflowNodeDto>
        {
            CreateManualTriggerNode(triggerId, "Start", 100, 100),
            CreateSetVariableNode(setVarId, "Set Var", 300, 100, new Dictionary<string, object>
            {
                ["variables"] = new List<Dictionary<string, object>>
                {
                    new() { ["name"] = "attempt", ["value"] = "1" }
                }
            }),
            CreateHttpRequestNode(httpFailId, "Failing HTTP", 500, 100, "https://invalid-domain-that-does-not-exist-12345.com/fail", "GET")
        };

        var connections = new List<NodeConnectionDto>
        {
            CreateConnection(triggerId, setVarId),
            CreateConnection(setVarId, httpFailId)
        };

        return CreateWorkflow(organizationId, userId, name, nodes, connections);
    }

    public static Workflow CreateParallelWorkflow(Guid organizationId, Guid userId, string name = "Parallel Test Workflow")
    {
        var triggerId = "trigger-1";
        var branch1Id = "branch-1";
        var branch2Id = "branch-2";
        var mergeId = "merge-1";

        var nodes = new List<WorkflowNodeDto>
        {
            CreateManualTriggerNode(triggerId, "Start", 100, 100),
            CreateSetVariableNode(branch1Id, "Branch 1", 300, 50, new Dictionary<string, object>
            {
                ["variables"] = new List<Dictionary<string, object>>
                {
                    new() { ["name"] = "branch1", ["value"] = "executed" }
                }
            }),
            CreateSetVariableNode(branch2Id, "Branch 2", 300, 150, new Dictionary<string, object>
            {
                ["variables"] = new List<Dictionary<string, object>>
                {
                    new() { ["name"] = "branch2", ["value"] = "executed" }
                }
            }),
            CreateSetVariableNode(mergeId, "Merge", 500, 100, new Dictionary<string, object>
            {
                ["variables"] = new List<Dictionary<string, object>>
                {
                    new() { ["name"] = "merged", ["value"] = "true" }
                }
            })
        };

        var connections = new List<NodeConnectionDto>
        {
            CreateConnection(triggerId, branch1Id),
            CreateConnection(triggerId, branch2Id),
            CreateConnection(branch1Id, mergeId),
            CreateConnection(branch2Id, mergeId)
        };

        return CreateWorkflow(organizationId, userId, name, nodes, connections);
    }

    public static Workflow CreateSlowWorkflow(Guid organizationId, Guid userId, int delaySeconds = 5, string name = "Slow Test Workflow")
    {
        var triggerId = "trigger-1";
        var setVarId = "setvar-1";

        var nodes = new List<WorkflowNodeDto>
        {
            CreateManualTriggerNode(triggerId, "Start", 100, 100),
            CreateSetVariableNode(setVarId, "Slow Node", 300, 100, new Dictionary<string, object>
            {
                ["variables"] = new List<Dictionary<string, object>>
                {
                    new() { ["name"] = "delay", ["value"] = delaySeconds }
                }
            }, new Dictionary<string, object>
            {
                ["timeout"] = delaySeconds * 2000,
                ["simulateDelay"] = delaySeconds * 1000
            })
        };

        var connections = new List<NodeConnectionDto>
        {
            CreateConnection(triggerId, setVarId)
        };

        return CreateWorkflow(organizationId, userId, name, nodes, connections);
    }

    public static WorkflowNodeDto CreateManualTriggerNode(string id, string name, int x, int y)
    {
        return new WorkflowNodeDto
        {
            Id = id,
            Type = "manual_trigger",
            Name = name,
            Position = new NodePositionDto { X = x, Y = y },
            Parameters = new Dictionary<string, object>(),
            Configuration = new Dictionary<string, object>()
        };
    }

    public static WorkflowNodeDto CreateSetVariableNode(
        string id,
        string name,
        int x,
        int y,
        Dictionary<string, object> parameters,
        Dictionary<string, object>? configuration = null)
    {
        var serializedParams = JsonSerializer.Serialize(parameters);
        var deserializedParams = JsonSerializer.Deserialize<Dictionary<string, object>>(serializedParams);

        return new WorkflowNodeDto
        {
            Id = id,
            Type = "set_variable",
            Name = name,
            Position = new NodePositionDto { X = x, Y = y },
            Parameters = deserializedParams ?? parameters,
            Configuration = configuration ?? new Dictionary<string, object>()
        };
    }

    public static WorkflowNodeDto CreateIfConditionNode(
        string id,
        string name,
        int x,
        int y,
        string value1,
        string operatorType,
        string value2)
    {
        var parameters = new Dictionary<string, object>
        {
            ["value1"] = value1,
            ["operator"] = operatorType,
            ["value2"] = value2
        };

        var serializedParams = JsonSerializer.Serialize(parameters);
        var deserializedParams = JsonSerializer.Deserialize<Dictionary<string, object>>(serializedParams);

        return new WorkflowNodeDto
        {
            Id = id,
            Type = "if_condition",
            Name = name,
            Position = new NodePositionDto { X = x, Y = y },
            Parameters = deserializedParams ?? parameters,
            Configuration = new Dictionary<string, object>()
        };
    }

    public static WorkflowNodeDto CreateHttpRequestNode(
        string id,
        string name,
        int x,
        int y,
        string url,
        string method,
        Dictionary<string, object>? additionalParams = null)
    {
        var parameters = new Dictionary<string, object>
        {
            ["url"] = url,
            ["method"] = method,
            ["authentication"] = "none"
        };

        if (additionalParams != null)
        {
            foreach (var (key, value) in additionalParams)
            {
                parameters[key] = value;
            }
        }

        return new WorkflowNodeDto
        {
            Id = id,
            Type = "http_request",
            Name = name,
            Position = new NodePositionDto { X = x, Y = y },
            Parameters = parameters,
            Configuration = new Dictionary<string, object>()
        };
    }

    public static NodeConnectionDto CreateConnection(
        string sourceId,
        string targetId,
        string sourceOutput = "default",
        string targetInput = "default")
    {
        return new NodeConnectionDto
        {
            SourceNodeId = sourceId,
            TargetNodeId = targetId,
            SourceOutput = sourceOutput,
            TargetInput = targetInput
        };
    }

    private static Workflow CreateWorkflow(
        Guid organizationId,
        Guid userId,
        string name,
        List<WorkflowNodeDto> nodes,
        List<NodeConnectionDto> connections)
    {
        return new Workflow
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = $"Test workflow: {name}",
            Status = WorkflowStatus.Active,
            Visibility = WorkflowVisibility.Private,
            IsTemplate = false,
            Version = 1,
            OrganizationId = organizationId,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow,
            NodesJson = JsonSerializer.Serialize(nodes),
            ConnectionsJson = JsonSerializer.Serialize(connections),
            SettingsJson = "{}"
        };
    }


}