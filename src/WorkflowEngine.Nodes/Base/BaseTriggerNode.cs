using Microsoft.Extensions.Logging;
using WorkflowEngine.Nodes.Interfaces;
using WorkflowEngine.Nodes.Models;

namespace WorkflowEngine.Nodes.Base;

public abstract class BaseTriggerNode : BaseNode
{
    protected BaseTriggerNode(ILogger logger, IExpressionEvaluator expressionEvaluator)
        : base(logger, expressionEvaluator)
    {
    }

    public override string Category => "Triggers";

    protected override async Task<NodeExecutionResult> ExecuteInternalAsync(NodeExecutionContext context)
    {
        var triggerData = await GetTriggerDataAsync(context);

        return NodeExecutionResult.Ok(new Dictionary<string, object>
        {
            ["triggered"] = true,
            ["timestamp"] = DateTime.UtcNow,
            ["data"] = triggerData
        });
    }

    protected abstract Task<Dictionary<string, object>> GetTriggerDataAsync(NodeExecutionContext context);
}