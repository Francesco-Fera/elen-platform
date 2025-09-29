namespace WorkflowEngine.Nodes.Models;

public class NodeExecutionResult
{
    public bool Success { get; init; }
    public Dictionary<string, object> OutputData { get; init; } = new();
    public string? ErrorMessage { get; init; }
    public Exception? Exception { get; init; }
    public Dictionary<string, object> Metadata { get; init; } = new();
    public static NodeExecutionResult Ok(Dictionary<string, object>? output = null)
        => new()
        {
            Success = true,
            OutputData = output ?? new()
        };
    public static NodeExecutionResult Error(string message, Exception? exception = null)
        => new()
        {
            Success = false,
            ErrorMessage = message,
            Exception = exception
        };
}
