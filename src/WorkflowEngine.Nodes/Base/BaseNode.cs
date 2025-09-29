using Microsoft.Extensions.Logging;
using WorkflowEngine.Nodes.Interfaces;
using WorkflowEngine.Nodes.Models;

namespace WorkflowEngine.Nodes.Base;

public abstract class BaseNode : INode
{
    protected readonly ILogger _logger;
    protected readonly IExpressionEvaluator _expressionEvaluator;

    protected BaseNode(ILogger logger, IExpressionEvaluator expressionEvaluator)
    {
        _logger = logger;
        _expressionEvaluator = expressionEvaluator;
    }

    public abstract string Type { get; }
    public abstract string Name { get; }
    public abstract string Category { get; }

    public abstract NodeDefinition GetDefinition();

    public virtual async Task<NodeExecutionResult> ExecuteAsync(NodeExecutionContext context)
    {
        try
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            _logger.LogInformation("Executing node {NodeType} ({NodeId})", Type, context.NodeId);

            await ValidateParametersAsync(context);

            var evaluatedParams = await _expressionEvaluator.EvaluateParametersAsync(
                context.Parameters,
                context.WorkflowContext);

            var evaluatedContext = context with { Parameters = evaluatedParams };

            var result = await ExecuteInternalAsync(evaluatedContext);

            _logger.LogInformation("Node {NodeType} ({NodeId}) executed successfully", Type, context.NodeId);

            return result;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Node {NodeType} ({NodeId}) execution cancelled", Type, context.NodeId);
            return NodeExecutionResult.Error("Execution cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing node {NodeType} ({NodeId})", Type, context.NodeId);
            return NodeExecutionResult.Error(ex.Message, ex);
        }
    }

    protected abstract Task<NodeExecutionResult> ExecuteInternalAsync(NodeExecutionContext context);

    protected virtual Task ValidateParametersAsync(NodeExecutionContext context)
    {
        var definition = GetDefinition();
        var errors = new List<string>();

        foreach (var operation in definition.Operations)
        {
            foreach (var parameter in operation.Parameters.Where(p => p.Required))
            {
                if (!context.Parameters.ContainsKey(parameter.Name) ||
                    context.Parameters[parameter.Name] == null)
                {
                    errors.Add($"Required parameter '{parameter.Name}' is missing");
                }
            }
        }

        if (errors.Any())
            throw new ArgumentException(string.Join(", ", errors));

        return Task.CompletedTask;
    }

    protected T GetRequiredParameter<T>(NodeExecutionContext context, string name)
    {
        var value = context.GetParameter<T>(name);
        if (value == null)
            throw new ArgumentException($"Required parameter '{name}' is missing or invalid");
        return value;
    }

    protected T? GetOptionalParameter<T>(NodeExecutionContext context, string name, T? defaultValue = default)
    {
        return context.GetParameter<T>(name) ?? defaultValue;
    }
}