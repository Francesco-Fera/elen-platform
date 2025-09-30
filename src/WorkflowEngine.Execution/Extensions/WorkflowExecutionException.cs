namespace WorkflowEngine.Execution.Exceptions;

public class WorkflowExecutionException : Exception
{
    public Guid? ExecutionId { get; }
    public string? NodeId { get; }

    public WorkflowExecutionException(string message) : base(message) { }

    public WorkflowExecutionException(string message, Exception innerException) : base(message, innerException) { }

    public WorkflowExecutionException(string message, Guid executionId) : base(message)
    {
        ExecutionId = executionId;
    }

    public WorkflowExecutionException(string message, Guid executionId, string nodeId) : base(message)
    {
        ExecutionId = executionId;
        NodeId = nodeId;
    }
}