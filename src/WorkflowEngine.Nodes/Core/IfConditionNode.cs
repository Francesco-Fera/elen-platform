using Microsoft.Extensions.Logging;
using WorkflowEngine.Nodes.Attributes;
using WorkflowEngine.Nodes.Base;
using WorkflowEngine.Nodes.Interfaces;
using WorkflowEngine.Nodes.Models;

namespace WorkflowEngine.Nodes.Core;

[NodeType("if_condition")]
[NodeCategory("Processing")]
public class IfConditionNode : BaseProcessingNode
{
    public IfConditionNode(ILogger<IfConditionNode> logger, IExpressionEvaluator expressionEvaluator)
        : base(logger, expressionEvaluator)
    {
    }

    public override string Type => "if_condition";
    public override string Name => "IF Condition";

    public override NodeDefinition GetDefinition()
    {
        return new NodeDefinition
        {
            Type = Type,
            Name = Name,
            Category = Category,
            Description = "Conditional branching based on comparison",
            Operations = new List<NodeOperation>
            {
                new()
                {
                    Name = "compare",
                    DisplayName = "Compare Values",
                    Parameters = new List<NodeParameter>
                    {
                        new() { Name = "value1", DisplayName = "Value 1", Type = "string", Required = true },
                        new()
                        {
                            Name = "operator",
                            DisplayName = "Operator",
                            Type = "select",
                            Required = true,
                            Options = new List<SelectOption>
                            {
                                new() { Value = "equals", Label = "Equals" },
                                new() { Value = "notEquals", Label = "Not Equals" },
                                new() { Value = "greaterThan", Label = "Greater Than" },
                                new() { Value = "lessThan", Label = "Less Than" },
                                new() { Value = "contains", Label = "Contains" },
                                new() { Value = "notContains", Label = "Not Contains" }
                            }
                        },
                        new() { Name = "value2", DisplayName = "Value 2", Type = "string", Required = true }
                    }
                }
            }
        };
    }

    protected override Task<NodeExecutionResult> ExecuteInternalAsync(NodeExecutionContext context)
    {
        var value1 = GetRequiredParameter<string>(context, "value1");
        var operatorType = GetRequiredParameter<string>(context, "operator");
        var value2 = GetRequiredParameter<string>(context, "value2");

        var result = operatorType switch
        {
            "equals" => value1 == value2,
            "notEquals" => value1 != value2,
            "greaterThan" => CompareNumeric(value1, value2) > 0,
            "lessThan" => CompareNumeric(value1, value2) < 0,
            "contains" => value1.Contains(value2, StringComparison.OrdinalIgnoreCase),
            "notContains" => !value1.Contains(value2, StringComparison.OrdinalIgnoreCase),
            _ => false
        };

        return Task.FromResult(NodeExecutionResult.Ok(new Dictionary<string, object>
        {
            ["result"] = result,
            ["value1"] = value1,
            ["value2"] = value2,
            ["operator"] = operatorType
        }));
    }

    private int CompareNumeric(string val1, string val2)
    {
        if (double.TryParse(val1, out var num1) && double.TryParse(val2, out var num2))
        {
            return num1.CompareTo(num2);
        }
        return string.Compare(val1, val2, StringComparison.Ordinal);
    }
}