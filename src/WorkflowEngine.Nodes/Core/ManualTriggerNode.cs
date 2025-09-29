using Microsoft.Extensions.Logging;
using WorkflowEngine.Nodes.Attributes;
using WorkflowEngine.Nodes.Base;
using WorkflowEngine.Nodes.Interfaces;
using WorkflowEngine.Nodes.Models;

namespace WorkflowEngine.Nodes.Core;

[NodeType("manual_trigger")]
[NodeCategory("Triggers")]
public class ManualTriggerNode : BaseTriggerNode
{
    public ManualTriggerNode(ILogger<ManualTriggerNode> logger, IExpressionEvaluator expressionEvaluator)
        : base(logger, expressionEvaluator)
    {
    }

    public override string Type => "manual_trigger";
    public override string Name => "Manual Trigger";

    public override NodeDefinition GetDefinition()
    {
        return new NodeDefinition
        {
            Type = Type,
            Name = Name,
            Category = Category,
            Description = "Manually trigger workflow execution",
            Operations = new List<NodeOperation>
            {
                new()
                {
                    Name = "trigger",
                    DisplayName = "Trigger",
                    Description = "Start workflow execution manually",
                    Parameters = new List<NodeParameter>()
                }
            }
        };
    }

    protected override Task<Dictionary<string, object>> GetTriggerDataAsync(NodeExecutionContext context)
    {
        return Task.FromResult(new Dictionary<string, object>
        {
            ["triggeredBy"] = context.UserId.ToString(),
            ["inputData"] = context.InputData
        });
    }
}