using Microsoft.Extensions.Logging;
using WorkflowEngine.Execution.Exceptions;
using WorkflowEngine.Execution.Interfaces;
using WorkflowEngine.Execution.Models;
using WorkflowEngine.Nodes.Interfaces;
using WorkflowEngine.Nodes.Models;

namespace WorkflowEngine.Execution.Engine;

public class NodeExecutor : INodeExecutor
{
    private readonly INodeRegistry _nodeRegistry;
    private readonly IExpressionEvaluator _expressionEvaluator;
    private readonly ILogger<NodeExecutor> _logger;

    public NodeExecutor(
        INodeRegistry nodeRegistry,
        IExpressionEvaluator expressionEvaluator,
        ILogger<NodeExecutor> logger)
    {
        _nodeRegistry = nodeRegistry;
        _expressionEvaluator = expressionEvaluator;
        _logger = logger;
    }

    public async Task<NodeExecutionResult> ExecuteNodeAsync(
        WorkflowNode node,
        NodeExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var nodeInstance = _nodeRegistry.CreateNode(node.Type);
        if (nodeInstance == null)
        {
            var error = $"Node type '{node.Type}' not registered";
            _logger.LogError("Failed to create node instance: {Error}", error);
            return new NodeExecutionResult
            {
                Success = false,
                ErrorMessage = error
            };
        }

        _logger.LogDebug(
            "Executing node {NodeId} ({NodeType}) for execution {ExecutionId}",
            node.Id, node.Type, context.ExecutionId);

        var evaluatedContext = await EvaluateContextParametersAsync(context, cancellationToken);

        var maxRetries = GetMaxRetries(node);
        var retryDelay = GetRetryDelay(node);
        var nodeTimeout = GetNodeTimeout(node);

        for (int attempt = 0; attempt <= maxRetries; attempt++)
        {
            if (attempt > 0)
            {
                _logger.LogWarning(
                    "Retrying node {NodeId} (attempt {Attempt}/{MaxRetries})",
                    node.Id, attempt, maxRetries);

                await Task.Delay(retryDelay, cancellationToken);
            }

            try
            {
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(nodeTimeout);

                var result = await nodeInstance.ExecuteAsync(evaluatedContext with
                {
                    CancellationToken = timeoutCts.Token
                });

                if (result.Success)
                {
                    _logger.LogDebug(
                        "Node {NodeId} executed successfully{RetryInfo}",
                        node.Id,
                        attempt > 0 ? $" after {attempt} retries" : string.Empty);

                    return result;
                }

                if (attempt == maxRetries)
                {
                    _logger.LogError(
                        "Node {NodeId} failed after {Attempts} attempts: {Error}",
                        node.Id, attempt + 1, result.ErrorMessage);

                    return result;
                }

                _logger.LogWarning(
                    "Node {NodeId} failed (attempt {Attempt}/{MaxRetries}): {Error}",
                    node.Id, attempt + 1, maxRetries + 1, result.ErrorMessage);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Node {NodeId} execution cancelled", node.Id);
                throw;
            }
            catch (OperationCanceledException)
            {
                var timeoutError = $"Node execution timed out after {nodeTimeout.TotalSeconds}s";
                _logger.LogError("Node {NodeId} timed out", node.Id);

                if (attempt == maxRetries)
                {
                    return new NodeExecutionResult
                    {
                        Success = false,
                        ErrorMessage = timeoutError
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Node {NodeId} threw exception (attempt {Attempt}/{MaxRetries})",
                    node.Id, attempt + 1, maxRetries + 1);

                if (attempt == maxRetries)
                {
                    return new NodeExecutionResult
                    {
                        Success = false,
                        ErrorMessage = ex.Message,
                        Exception = ex
                    };
                }
            }
        }

        return new NodeExecutionResult
        {
            Success = false,
            ErrorMessage = "Node execution failed after all retry attempts"
        };
    }

    private async Task<NodeExecutionContext> EvaluateContextParametersAsync(
        NodeExecutionContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            var evaluatedParams = await _expressionEvaluator.EvaluateParametersAsync(
                context.Parameters,
                context.WorkflowContext);

            var evaluatedInputData = await _expressionEvaluator.EvaluateParametersAsync(
                context.InputData,
                context.WorkflowContext);

            return context with
            {
                Parameters = evaluatedParams,
                InputData = evaluatedInputData
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to evaluate expressions for node {NodeId}",
                context.NodeId);
            throw new WorkflowExecutionException(
                $"Expression evaluation failed: {ex.Message}",
                context.ExecutionId,
                context.NodeId);
        }
    }

    private int GetMaxRetries(WorkflowNode node)
    {
        if (node.Configuration.TryGetValue("maxRetries", out var retriesObj))
        {
            if (retriesObj is int retries)
                return Math.Max(0, retries);

            if (int.TryParse(retriesObj?.ToString(), out var parsed))
                return Math.Max(0, parsed);
        }

        return 0;
    }

    private TimeSpan GetRetryDelay(WorkflowNode node)
    {
        if (node.Configuration.TryGetValue("retryDelay", out var delayObj))
        {
            if (delayObj is int delayMs)
                return TimeSpan.FromMilliseconds(Math.Max(0, delayMs));

            if (int.TryParse(delayObj?.ToString(), out var parsed))
                return TimeSpan.FromMilliseconds(Math.Max(0, parsed));
        }

        return TimeSpan.FromSeconds(5);
    }

    private TimeSpan GetNodeTimeout(WorkflowNode node)
    {
        if (node.Configuration.TryGetValue("timeout", out var timeoutObj))
        {
            if (timeoutObj is int timeoutMs)
                return TimeSpan.FromMilliseconds(Math.Max(1000, timeoutMs));

            if (int.TryParse(timeoutObj?.ToString(), out var parsed))
                return TimeSpan.FromMilliseconds(Math.Max(1000, parsed));
        }

        return TimeSpan.FromMinutes(1);
    }
}