using WorkflowEngine.Core.Entities;
using WorkflowEngine.Core.Enums;
using WorkflowEngine.Execution.Models;

namespace WorkflowEngine.Execution.Interfaces;

public interface IWorkflowExecutionEngine
{
    Task<ExecutionResult> ExecuteAsync(
        Workflow workflow,
        Dictionary<string, object>? inputData = null,
        ExecutionOptions? options = null,
        CancellationToken cancellationToken = default);

    Task<ExecutionResult> CancelExecutionAsync(Guid executionId, CancellationToken cancellationToken = default);

    Task<ExecutionStatus> GetExecutionStatusAsync(Guid executionId, CancellationToken cancellationToken = default);
}