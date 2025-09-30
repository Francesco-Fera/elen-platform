using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkflowEngine.Core.Entities;
using WorkflowEngine.Core.Enums;
using WorkflowEngine.Execution.Interfaces;
using WorkflowEngine.Infrastructure.Data;
using WorkflowEngine.Nodes.Models;
using LogLevel = WorkflowEngine.Core.Enums.LogLevel;

namespace WorkflowEngine.Execution.Logging;

public class ExecutionLogger : IExecutionLogger
{
    private readonly WorkflowEngineDbContext _dbContext;
    private readonly ILogger<ExecutionLogger> _logger;

    public ExecutionLogger(
        WorkflowEngineDbContext dbContext,
        ILogger<ExecutionLogger> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task LogExecutionStartAsync(
        Guid executionId,
        Guid workflowId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var log = new ExecutionLog
            {
                Id = Guid.NewGuid(),
                ExecutionId = executionId,
                NodeId = null,
                Level = LogLevel.Info,
                Message = $"Workflow execution started",
                Timestamp = DateTime.UtcNow
                //DetailsJson = JsonSerializer.Serialize(new { workflowId }),
            };

            await _dbContext.ExecutionLogs.AddAsync(log, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Execution {ExecutionId} started for workflow {WorkflowId}",
                executionId, workflowId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log execution start for {ExecutionId}", executionId);
        }
    }

    public async Task LogExecutionCompleteAsync(
        Guid executionId,
        ExecutionStatus status,
        TimeSpan duration,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var level = status == ExecutionStatus.Completed
                ? LogLevel.Info
                : LogLevel.Error;

            var log = new ExecutionLog
            {
                Id = Guid.NewGuid(),
                ExecutionId = executionId,
                NodeId = null,
                Level = level,
                Message = $"Workflow execution {status.ToString().ToLower()}",
                Timestamp = DateTime.UtcNow
                //DetailsJson = JsonSerializer.Serialize(new
                //{
                //    status = status.ToString(),
                //    durationMs = duration.TotalMilliseconds
                //}),
            };

            await _dbContext.ExecutionLogs.AddAsync(log, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            //_logger.Log(
            //    level,
            //    "Execution {ExecutionId} completed with status {Status} in {Duration}ms",
            //    executionId, status, duration.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log execution completion for {ExecutionId}", executionId);
        }
    }

    public async Task LogNodeExecutionAsync(
        Guid executionId,
        string nodeId,
        string nodeType,
        NodeExecutionResult result,
        TimeSpan duration,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var level = result.Success ? LogLevel.Info : LogLevel.Error;
            var message = result.Success
                ? $"Node '{nodeId}' ({nodeType}) executed successfully"
                : $"Node '{nodeId}' ({nodeType}) execution failed: {result.ErrorMessage}";

            var details = new Dictionary<string, object>
            {
                ["nodeId"] = nodeId,
                ["nodeType"] = nodeType,
                ["success"] = result.Success,
                ["durationMs"] = duration.TotalMilliseconds
            };

            if (result.OutputData.Count > 0)
                details["outputKeys"] = result.OutputData.Keys.ToList();

            if (!result.Success && result.ErrorMessage != null)
                details["error"] = result.ErrorMessage;

            if (result.Metadata.Count > 0)
                details["metadata"] = result.Metadata;

            var log = new ExecutionLog
            {
                Id = Guid.NewGuid(),
                ExecutionId = executionId,
                NodeId = nodeId,
                Level = level,
                Message = message,
                Timestamp = DateTime.UtcNow
                //DetailsJson = JsonSerializer.Serialize(details),
            };

            await _dbContext.ExecutionLogs.AddAsync(log, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            //_logger.LogInformation(
            //    level,
            //    "Node {NodeId} ({NodeType}) execution {Status} in {Duration}ms for execution {ExecutionId}",
            //    nodeId, nodeType, result.Success ? "succeeded" : "failed", duration.TotalMilliseconds, executionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to log node execution for {NodeId} in execution {ExecutionId}",
                nodeId, executionId);
        }
    }

    public async Task LogErrorAsync(
        Guid executionId,
        string? nodeId,
        string message,
        Exception? exception,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var details = new Dictionary<string, object>
            {
                ["message"] = message
            };

            if (nodeId != null)
                details["nodeId"] = nodeId;

            if (exception != null)
            {
                details["exceptionType"] = exception.GetType().Name;
                details["exceptionMessage"] = exception.Message;
                details["stackTrace"] = exception.StackTrace ?? string.Empty;
            }

            var log = new ExecutionLog
            {
                Id = Guid.NewGuid(),
                ExecutionId = executionId,
                NodeId = nodeId,
                Level = LogLevel.Error,
                Message = message,
                Timestamp = DateTime.UtcNow
                //DetailsJson = JsonSerializer.Serialize(details),
            };

            await _dbContext.ExecutionLogs.AddAsync(log, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogError(exception,
                "Execution error in {ExecutionId}{NodeInfo}: {Message}",
                executionId,
                nodeId != null ? $" at node {nodeId}" : string.Empty,
                message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to log error for execution {ExecutionId}",
                executionId);
        }
    }

    public async Task LogInfoAsync(
        Guid executionId,
        string? nodeId,
        string message,
        Dictionary<string, object>? details = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var detailsDict = details ?? new Dictionary<string, object>();
            if (nodeId != null)
                detailsDict["nodeId"] = nodeId;

            var log = new ExecutionLog
            {
                Id = Guid.NewGuid(),
                ExecutionId = executionId,
                NodeId = nodeId,
                Level = LogLevel.Info,
                Message = message,
                Timestamp = DateTime.UtcNow
                //DetailsJson = detailsDict.Count > 0 ? JsonSerializer.Serialize(detailsDict) : null,
            };

            await _dbContext.ExecutionLogs.AddAsync(log, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Execution {ExecutionId}{NodeInfo}: {Message}",
                executionId,
                nodeId != null ? $" at node {nodeId}" : string.Empty,
                message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to log info for execution {ExecutionId}",
                executionId);
        }
    }

    public async Task<List<ExecutionLog>> GetExecutionLogsAsync(
        Guid executionId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.ExecutionLogs
            .Where(l => l.ExecutionId == executionId)
            .OrderBy(l => l.Timestamp)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ExecutionLog>> GetNodeLogsAsync(
        Guid executionId,
        string nodeId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.ExecutionLogs
            .Where(l => l.ExecutionId == executionId && l.NodeId == nodeId)
            .OrderBy(l => l.Timestamp)
            .ToListAsync(cancellationToken);
    }

    public async Task DeleteOldLogsAsync(
        TimeSpan retentionPeriod,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow - retentionPeriod;

            var oldLogs = await _dbContext.ExecutionLogs
                .Where(l => l.Timestamp < cutoffDate)
                .ToListAsync(cancellationToken);

            if (oldLogs.Count > 0)
            {
                _dbContext.ExecutionLogs.RemoveRange(oldLogs);
                await _dbContext.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Deleted {Count} execution logs older than {CutoffDate}",
                    oldLogs.Count, cutoffDate);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete old execution logs");
        }
    }
}