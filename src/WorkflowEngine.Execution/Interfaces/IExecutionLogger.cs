using WorkflowEngine.Core.Entities;
using WorkflowEngine.Core.Enums;
using WorkflowEngine.Nodes.Models;

namespace WorkflowEngine.Execution.Interfaces;

public interface IExecutionLogger
{
    Task LogExecutionStartAsync(Guid executionId, Guid workflowId, CancellationToken cancellationToken = default);

    Task LogExecutionCompleteAsync(Guid executionId, ExecutionStatus status, TimeSpan duration, CancellationToken cancellationToken = default);

    Task LogNodeExecutionAsync(Guid executionId, string nodeId, string nodeType, NodeExecutionResult result, TimeSpan duration, CancellationToken cancellationToken = default);

    Task LogErrorAsync(Guid executionId, string? nodeId, string message, Exception? exception, CancellationToken cancellationToken = default);

    Task LogInfoAsync(Guid executionId, string? nodeId, string message, Dictionary<string, object>? details = null, CancellationToken cancellationToken = default);

    Task<List<ExecutionLog>> GetExecutionLogsAsync(Guid executionId, CancellationToken cancellationToken = default);

    Task<List<ExecutionLog>> GetNodeLogsAsync(Guid executionId, string nodeId, CancellationToken cancellationToken = default);

    Task DeleteOldLogsAsync(TimeSpan retentionPeriod, CancellationToken cancellationToken = default);
}