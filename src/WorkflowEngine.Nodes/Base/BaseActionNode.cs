using Microsoft.Extensions.Logging;
using WorkflowEngine.Nodes.Interfaces;

namespace WorkflowEngine.Nodes.Base;

public abstract class BaseActionNode : BaseNode
{
    protected BaseActionNode(ILogger logger, IExpressionEvaluator expressionEvaluator)
        : base(logger, expressionEvaluator)
    {
    }

    public override string Category => "Actions";
}