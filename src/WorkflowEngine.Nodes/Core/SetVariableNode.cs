using Microsoft.Extensions.Logging;
using WorkflowEngine.Nodes.Attributes;
using WorkflowEngine.Nodes.Base;
using WorkflowEngine.Nodes.Interfaces;
using WorkflowEngine.Nodes.Models;

namespace WorkflowEngine.Nodes.Core;

[NodeType("set_variable")]
[NodeCategory("Processing")]
public class SetVariableNode : BaseProcessingNode
{
    public SetVariableNode(ILogger<SetVariableNode> logger, IExpressionEvaluator expressionEvaluator)
        : base(logger, expressionEvaluator)
    {
    }

    public override string Type => "set_variable";
    public override string Name => "Set Variable";

    public override NodeDefinition GetDefinition()
    {
        return new NodeDefinition
        {
            Type = Type,
            Name = Name,
            Category = Category,
            Description = "Set workflow variables",
            Operations = new List<NodeOperation>
            {
                new()
                {
                    Name = "set",
                    DisplayName = "Set Variables",
                    Parameters = new List<NodeParameter>
                    {
                        new()
                        {
                            Name = "variables",
                            DisplayName = "Variables",
                            Type = "array",
                            Required = true,
                            Description = "Array of {name, value} objects"
                        }
                    }
                }
            }
        };
    }

    protected override Task<NodeExecutionResult> ExecuteInternalAsync(NodeExecutionContext context)
    {
        var variables = GetRequiredParameter<List<Dictionary<string, object>>>(context, "variables");
        var output = new Dictionary<string, object>();

        foreach (var variable in variables)
        {
            if (variable.TryGetValue("name", out var nameObj) &&
                variable.TryGetValue("value", out var value))
            {
                var name = nameObj?.ToString();
                if (!string.IsNullOrEmpty(name))
                {
                    output[name] = value;
                }
            }
        }

        return Task.FromResult(NodeExecutionResult.Ok(output));
    }
}