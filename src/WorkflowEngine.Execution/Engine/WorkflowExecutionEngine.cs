using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;
using WorkflowEngine.Application.DTOs.Workflow;
using WorkflowEngine.Core.Entities;
using WorkflowEngine.Core.Enums;
using WorkflowEngine.Execution.Exceptions;
using WorkflowEngine.Execution.Interfaces;
using WorkflowEngine.Execution.Models;
using WorkflowEngine.Infrastructure.Data;
using WorkflowEngine.Nodes.Models;

namespace WorkflowEngine.Execution.Engine;

public class WorkflowExecutionEngine : IWorkflowExecutionEngine
{
    private readonly WorkflowEngineDbContext _dbContext;
    private readonly IWorkflowGraphBuilder _graphBuilder;
    private readonly ITopologicalSorter _topologicalSorter;
    private readonly INodeExecutor _nodeExecutor;
    private readonly IExecutionLogger _executionLogger;
    private readonly IExecutionContextFactory _contextFactory;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WorkflowExecutionEngine> _logger;

    public WorkflowExecutionEngine(
        WorkflowEngineDbContext dbContext,
        IWorkflowGraphBuilder graphBuilder,
        ITopologicalSorter topologicalSorter,
        INodeExecutor nodeExecutor,
        IExecutionLogger executionLogger,
        IExecutionContextFactory contextFactory,
        IServiceProvider serviceProvider,
        ILogger<WorkflowExecutionEngine> logger)
    {
        _dbContext = dbContext;
        _graphBuilder = graphBuilder;
        _topologicalSorter = topologicalSorter;
        _nodeExecutor = nodeExecutor;
        _executionLogger = executionLogger;
        _contextFactory = contextFactory;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<Models.ExecutionResult> ExecuteAsync(
    Workflow workflow,
    Dictionary<string, object>? inputData = null,
    ExecutionOptions? options = null,
    CancellationToken cancellationToken = default)
    {
        options ??= new ExecutionOptions();
        var executionId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation(
            "Starting execution {ExecutionId} for workflow {WorkflowId} ({WorkflowName})",
            executionId, workflow.Id, workflow.Name);

        var execution = new WorkflowExecution
        {
            Id = executionId,
            WorkflowId = workflow.Id,
            UserId = null,
            Status = ExecutionStatus.Running,
            TriggerType = ExecutionTrigger.Manual,
            InputDataJson = inputData != null ? JsonSerializer.Serialize(inputData) : null,
            StartedAt = startTime
        };

        try
        {
            await _dbContext.WorkflowExecutions.AddAsync(execution, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            await _executionLogger.LogExecutionStartAsync(executionId, workflow.Id, cancellationToken);

            var nodes = string.IsNullOrEmpty(workflow.NodesJson)
                ? new List<WorkflowNodeDto>()
                : JsonSerializer.Deserialize<List<WorkflowNodeDto>>(workflow.NodesJson) ?? new List<WorkflowNodeDto>();

            var connections = string.IsNullOrEmpty(workflow.ConnectionsJson)
                ? new List<NodeConnectionDto>()
                : JsonSerializer.Deserialize<List<NodeConnectionDto>>(workflow.ConnectionsJson) ?? new List<NodeConnectionDto>();

            if (nodes.Count == 0)
            {
                throw new WorkflowExecutionException("Workflow contains no nodes", executionId);
            }

            var graph = _graphBuilder.BuildGraph(nodes, connections);
            await _graphBuilder.ValidateGraphAsync(graph, cancellationToken);

            var workflowContext = _contextFactory.CreateWorkflowContext(
                executionId,
                workflow.Id,
                workflow.Name,
                inputData);

            var nodeExecutions = new List<NodeExecutionSummary>();

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(options.Timeout);

            Models.ExecutionResult? executionResult;

            var executionStrategy = DetermineExecutionStrategy(graph, options);

            if (executionStrategy == ExecutionStrategy.Parallel)
            {
                executionResult = await ExecuteParallelAsync(
                    graph,
                    connections,
                    workflowContext,
                    executionId,
                    options,
                    nodeExecutions,
                    timeoutCts.Token);
            }
            else
            {
                executionResult = await ExecuteSequentialAsync(
                    graph,
                    connections,
                    workflowContext,
                    executionId,
                    options,
                    nodeExecutions,
                    timeoutCts.Token);
            }

            stopwatch.Stop();
            var duration = stopwatch.Elapsed;

            execution.Status = executionResult.Status;
            execution.CompletedAt = DateTime.UtcNow;
            execution.Duration = duration;
            execution.OutputDataJson = JsonSerializer.Serialize(workflowContext);

            if (!executionResult.IsSuccess)
            {
                execution.ErrorDataJson = JsonSerializer.Serialize(new
                {
                    message = executionResult.ErrorMessage,
                    failedNodes = nodeExecutions.Where(n => !n.Success).Select(n => n.NodeId).ToList()
                });
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            await _executionLogger.LogExecutionCompleteAsync(
                executionId,
                execution.Status,
                duration,
                cancellationToken);

            _logger.LogInformation(
                "Execution {ExecutionId} completed with status {Status} in {Duration}ms",
                executionId, execution.Status, duration.TotalMilliseconds);

            return executionResult with
            {
                Duration = duration,
                NodeExecutions = nodeExecutions
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return await HandleCancellationAsync(execution, stopwatch.Elapsed, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return await HandleTimeoutAsync(execution, stopwatch.Elapsed, options.Timeout, cancellationToken);
        }
        catch (Exception ex)
        {
            return await HandleExecutionErrorAsync(execution, ex, stopwatch.Elapsed, cancellationToken);
        }
    }


    private async Task<Models.ExecutionResult> ExecuteSequentialAsync(
    WorkflowGraph graph,
    List<NodeConnectionDto> connections,
    Dictionary<string, object> workflowContext,
    Guid executionId,
    ExecutionOptions options,
    List<NodeExecutionSummary> nodeExecutions,
    CancellationToken cancellationToken)
    {
        var executionOrder = _topologicalSorter.Sort(graph);
        var completedNodes = new HashSet<string>();

        foreach (var nodeId in executionOrder)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var node = graph.GetNode(nodeId);
            var nodeStopwatch = Stopwatch.StartNew();

            var shouldExecute = ShouldExecuteNode(nodeId, graph, completedNodes, workflowContext);
            if (!shouldExecute)
            {
                _logger.LogDebug("Skipping node {NodeId} - conditional branch not taken", nodeId);
                continue;
            }

            var inputData = _contextFactory.GetInputDataForNode(node, workflowContext, connections);

            var nodeContext = new NodeExecutionContext
            {
                ExecutionId = executionId,
                NodeId = nodeId,
                UserId = Guid.Empty,
                Parameters = node.Parameters,
                InputData = inputData,
                WorkflowContext = workflowContext,
                Services = _serviceProvider,
                CancellationToken = cancellationToken
            };

            var result = await _nodeExecutor.ExecuteNodeAsync(node, nodeContext, cancellationToken);
            nodeStopwatch.Stop();

            var summary = new NodeExecutionSummary
            {
                NodeId = nodeId,
                NodeType = node.Type,
                NodeName = node.Name,
                Success = result.Success,
                Duration = nodeStopwatch.Elapsed,
                StartedAt = DateTime.UtcNow - nodeStopwatch.Elapsed,
                CompletedAt = DateTime.UtcNow,
                ErrorMessage = result.ErrorMessage
            };

            nodeExecutions.Add(summary);

            await _executionLogger.LogNodeExecutionAsync(
                executionId,
                nodeId,
                node.Type,
                result,
                nodeStopwatch.Elapsed,
                cancellationToken);

            if (result.Success)
            {
                _contextFactory.UpdateContextWithNodeOutput(workflowContext, nodeId, result);
                completedNodes.Add(nodeId);
            }
            else
            {
                // MODIFICATO: even if it fails, update the context context and mark as completed if ContinueOnError
                if (options.ContinueOnError)
                {
                    // Add partial output to context (even if fail)
                    _contextFactory.UpdateContextWithNodeOutput(workflowContext, nodeId, result);
                    completedNodes.Add(nodeId);

                    await _executionLogger.LogErrorAsync(
                        executionId,
                        nodeId,
                        $"Node execution failed but workflow continues: {result.ErrorMessage}",
                        result.Exception,
                        cancellationToken);
                }
                else
                {
                    return Models.ExecutionResult.Failed(
                        executionId,
                        TimeSpan.Zero,
                        $"Node {nodeId} failed: {result.ErrorMessage}",
                        nodeExecutions);
                }
            }
        }

        return Models.ExecutionResult.Success(
            executionId,
            TimeSpan.Zero,
            workflowContext,
            nodeExecutions);
    }
    private async Task<Models.ExecutionResult> ExecuteParallelAsync(
    WorkflowGraph graph,
    List<NodeConnectionDto> connections,
    Dictionary<string, object> workflowContext,
    Guid executionId,
    ExecutionOptions options,
    List<NodeExecutionSummary> nodeExecutions,
    CancellationToken cancellationToken)
    {
        var parallelGroups = _topologicalSorter.GetParallelGroups(graph);
        var completedNodes = new HashSet<string>();

        foreach (var group in parallelGroups)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var executableNodes = group
                .Where(nodeId => ShouldExecuteNode(nodeId, graph, completedNodes, workflowContext))
                .ToList();

            if (executableNodes.Count == 0)
                continue;

            var tasks = executableNodes.Select(async nodeId =>
            {
                var node = graph.GetNode(nodeId);
                var nodeStopwatch = Stopwatch.StartNew();

                var inputData = _contextFactory.GetInputDataForNode(node, workflowContext, connections);

                var nodeContext = new NodeExecutionContext
                {
                    ExecutionId = executionId,
                    NodeId = nodeId,
                    UserId = Guid.Empty,
                    Parameters = node.Parameters,
                    InputData = inputData,
                    WorkflowContext = workflowContext,
                    Services = _serviceProvider,
                    CancellationToken = cancellationToken
                };

                var result = await _nodeExecutor.ExecuteNodeAsync(node, nodeContext, cancellationToken);
                nodeStopwatch.Stop();

                var summary = new NodeExecutionSummary
                {
                    NodeId = nodeId,
                    NodeType = node.Type,
                    NodeName = node.Name,
                    Success = result.Success,
                    Duration = nodeStopwatch.Elapsed,
                    StartedAt = DateTime.UtcNow - nodeStopwatch.Elapsed,
                    CompletedAt = DateTime.UtcNow,
                    ErrorMessage = result.ErrorMessage
                };

                await _executionLogger.LogNodeExecutionAsync(
                    executionId,
                    nodeId,
                    node.Type,
                    result,
                    nodeStopwatch.Elapsed,
                    cancellationToken);

                return (nodeId, result, summary);
            }).ToList();

            var results = await Task.WhenAll(tasks);

            foreach (var (nodeId, result, summary) in results)
            {
                nodeExecutions.Add(summary);

                if (result.Success)
                {
                    lock (workflowContext)
                    {
                        _contextFactory.UpdateContextWithNodeOutput(workflowContext, nodeId, result);
                    }
                    lock (completedNodes)
                    {
                        completedNodes.Add(nodeId);
                    }
                }
                else
                {
                    // EDIT: Same logic as Sequential
                    if (options.ContinueOnError)
                    {
                        lock (workflowContext)
                        {
                            _contextFactory.UpdateContextWithNodeOutput(workflowContext, nodeId, result);
                        }
                        lock (completedNodes)
                        {
                            completedNodes.Add(nodeId);
                        }

                        await _executionLogger.LogErrorAsync(
                            executionId,
                            nodeId,
                            $"Node execution failed but workflow continues: {result.ErrorMessage}",
                            result.Exception,
                            cancellationToken);
                    }
                    else
                    {
                        return Models.ExecutionResult.Failed(
                            executionId,
                            TimeSpan.Zero,
                            $"Node {nodeId} failed: {result.ErrorMessage}",
                            nodeExecutions);
                    }
                }
            }
        }

        return Models.ExecutionResult.Success(
            executionId,
            TimeSpan.Zero,
            workflowContext,
            nodeExecutions);
    }

    private bool ShouldExecuteNode(
        string nodeId,
        WorkflowGraph graph,
        HashSet<string> completedNodes,
        Dictionary<string, object> workflowContext)
    {
        var incomingEdges = graph.GetIncomingEdges(nodeId);

        if (incomingEdges.Count == 0)
            return true;

        foreach (var edge in incomingEdges)
        {
            if (!completedNodes.Contains(edge.SourceNodeId))
                return false;

            if (edge.SourceOutput != "default")
            {
                var contextKey = $"$node.{edge.SourceNodeId}";
                if (workflowContext.TryGetValue(contextKey, out var nodeData))
                {
                    if (nodeData is Dictionary<string, object> dataDict)
                    {
                        if (dataDict.TryGetValue("conditionalOutput", out var output))
                        {
                            if (output?.ToString() != edge.SourceOutput)
                                return false;
                        }
                    }
                }
            }
        }

        return true;
    }

    private async Task<Models.ExecutionResult> HandleCancellationAsync(
        WorkflowExecution execution,
        TimeSpan duration,
        CancellationToken cancellationToken)
    {
        _logger.LogWarning("Execution {ExecutionId} was cancelled", execution.Id);

        execution.Status = ExecutionStatus.Cancelled;
        execution.CompletedAt = DateTime.UtcNow;
        execution.Duration = duration;

        await _dbContext.SaveChangesAsync(CancellationToken.None);

        await _executionLogger.LogExecutionCompleteAsync(
            execution.Id,
            ExecutionStatus.Cancelled,
            duration,
            CancellationToken.None);

        return Models.ExecutionResult.Cancelled(execution.Id, duration, new List<NodeExecutionSummary>());
    }

    private async Task<Models.ExecutionResult> HandleTimeoutAsync(
        WorkflowExecution execution,
        TimeSpan duration,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        _logger.LogError("Execution {ExecutionId} timed out after {Timeout}s", execution.Id, timeout.TotalSeconds);

        execution.Status = ExecutionStatus.Timeout;
        execution.CompletedAt = DateTime.UtcNow;
        execution.Duration = duration;
        execution.ErrorDataJson = JsonSerializer.Serialize(new
        {
            message = $"Execution timed out after {timeout.TotalSeconds}s"
        });

        await _dbContext.SaveChangesAsync(CancellationToken.None);

        await _executionLogger.LogErrorAsync(
            execution.Id,
            null,
            $"Workflow execution timed out after {timeout.TotalSeconds}s",
            null,
            CancellationToken.None);

        return Models.ExecutionResult.Timeout(execution.Id, duration, new List<NodeExecutionSummary>());
    }

    private async Task<Models.ExecutionResult> HandleExecutionErrorAsync(
        WorkflowExecution execution,
        Exception exception,
        TimeSpan duration,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Execution {ExecutionId} failed with exception", execution.Id);

        execution.Status = ExecutionStatus.Failed;
        execution.CompletedAt = DateTime.UtcNow;
        execution.Duration = duration;
        execution.ErrorDataJson = JsonSerializer.Serialize(new
        {
            message = exception.Message,
            type = exception.GetType().Name,
            stackTrace = exception.StackTrace
        });

        await _dbContext.SaveChangesAsync(CancellationToken.None);

        await _executionLogger.LogErrorAsync(
            execution.Id,
            null,
            $"Workflow execution failed: {exception.Message}",
            exception,
            CancellationToken.None);

        return Models.ExecutionResult.Failed(execution.Id, duration, exception.Message, new List<NodeExecutionSummary>());
    }

    public async Task<Models.ExecutionResult> CancelExecutionAsync(
        Guid executionId,
        CancellationToken cancellationToken = default)
    {
        var execution = await _dbContext.WorkflowExecutions
            .FirstOrDefaultAsync(e => e.Id == executionId, cancellationToken);

        if (execution == null)
        {
            throw new WorkflowExecutionException($"Execution {executionId} not found");
        }

        if (execution.Status != ExecutionStatus.Running && execution.Status != ExecutionStatus.Pending)
        {
            throw new WorkflowExecutionException(
                $"Cannot cancel execution {executionId} with status {execution.Status}");
        }

        execution.Status = ExecutionStatus.Cancelled;
        execution.CompletedAt = DateTime.UtcNow;
        execution.Duration = execution.CompletedAt - execution.StartedAt;

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _executionLogger.LogInfoAsync(
            executionId,
            null,
            "Execution cancelled by user request",
            null,
            cancellationToken);

        _logger.LogInformation("Execution {ExecutionId} cancelled", executionId);

        return Models.ExecutionResult.Cancelled(executionId, execution.Duration ?? TimeSpan.Zero, new List<NodeExecutionSummary>());
    }

    public async Task<ExecutionStatus> GetExecutionStatusAsync(
        Guid executionId,
        CancellationToken cancellationToken = default)
    {
        var execution = await _dbContext.WorkflowExecutions
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == executionId, cancellationToken);

        if (execution == null)
        {
            throw new WorkflowExecutionException($"Execution {executionId} not found");
        }

        return execution.Status;
    }

    private ExecutionStrategy DetermineExecutionStrategy(WorkflowGraph graph, ExecutionOptions options)
    {
        if (!options.EnableParallelExecution)
        {
            _logger.LogDebug("Sequential execution explicitly requested");
            return ExecutionStrategy.Sequential;
        }

        try
        {
            var parallelGroups = _topologicalSorter.GetParallelGroups(graph);

            if (parallelGroups == null || parallelGroups.Count == 0)
            {
                _logger.LogWarning("GetParallelGroups returned null/empty, falling back to sequential execution");
                return ExecutionStrategy.Sequential;
            }

            // Se tutti i gruppi hanno 1 solo nodo, non c'è beneficio nel parallel
            var hasParallelism = parallelGroups.Any(g => g.Count > 1);
            if (!hasParallelism)
            {
                _logger.LogDebug("No parallelism detected in workflow, using sequential execution");
                return ExecutionStrategy.Sequential;
            }

            _logger.LogInformation("Using parallel execution with {GroupCount} groups", parallelGroups.Count);
            return ExecutionStrategy.Parallel;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error determining parallel groups, falling back to sequential execution");
            return ExecutionStrategy.Sequential;
        }
    }

    private enum ExecutionStrategy
    {
        Sequential,
        Parallel
    }
}