using Microsoft.Extensions.Logging;
using WorkflowEngine.Nodes.Interfaces;

namespace WorkflowEngine.Nodes.Base;

public abstract class BaseProcessingNode : BaseNode
{
    protected BaseProcessingNode(ILogger logger, IExpressionEvaluator expressionEvaluator)
        : base(logger, expressionEvaluator)
    {
    }

    public override string Category => "Processing";
}