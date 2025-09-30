using WorkflowEngine.Core.Enums;

namespace WorkflowEngine.Execution.Models;

public record ExecutionResult
{
    public Guid ExecutionId { get; init; }
    public ExecutionStatus Status { get; init; }
    public Dictionary<string, object> OutputData { get; init; } = new();
    public string? ErrorMessage { get; init; }
    public TimeSpan Duration { get; init; }
    public List<NodeExecutionSummary> NodeExecutions { get; init; } = new();
    public bool IsSuccess => Status == ExecutionStatus.Completed;
    public bool HasFailedNodes => NodeExecutions.Any(n => !n.Success);

    public static ExecutionResult Success(
        Guid executionId,
        TimeSpan duration,
        Dictionary<string, object> outputData,
        List<NodeExecutionSummary> nodeExecutions)
    {
        return new ExecutionResult
        {
            ExecutionId = executionId,
            Status = ExecutionStatus.Completed,
            Duration = duration,
            OutputData = outputData,
            NodeExecutions = nodeExecutions
        };
    }

    public static ExecutionResult Failed(
        Guid executionId,
        TimeSpan duration,
        string errorMessage,
        List<NodeExecutionSummary> nodeExecutions)
    {
        return new ExecutionResult
        {
            ExecutionId = executionId,
            Status = ExecutionStatus.Failed,
            Duration = duration,
            ErrorMessage = errorMessage,
            NodeExecutions = nodeExecutions
        };
    }

    public static ExecutionResult Cancelled(
        Guid executionId,
        TimeSpan duration,
        List<NodeExecutionSummary> nodeExecutions)
    {
        return new ExecutionResult
        {
            ExecutionId = executionId,
            Status = ExecutionStatus.Cancelled,
            Duration = duration,
            NodeExecutions = nodeExecutions
        };
    }

    public static ExecutionResult Timeout(
        Guid executionId,
        TimeSpan duration,
        List<NodeExecutionSummary> nodeExecutions)
    {
        return new ExecutionResult
        {
            ExecutionId = executionId,
            Status = ExecutionStatus.Timeout,
            Duration = duration,
            NodeExecutions = nodeExecutions
        };
    }
}