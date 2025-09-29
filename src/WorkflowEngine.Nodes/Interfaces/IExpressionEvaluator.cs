namespace WorkflowEngine.Nodes.Interfaces;

public interface IExpressionEvaluator
{
    Task<object?> EvaluateAsync(string expression, Dictionary<string, object> context);
    bool IsExpression(string value);
    Task<Dictionary<string, object>> EvaluateParametersAsync(
        Dictionary<string, object> parameters,
        Dictionary<string, object> context);
}