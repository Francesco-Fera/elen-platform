namespace WorkflowEngine.Nodes.Models;

public class NodeExecutionContext
{
    public required Guid ExecutionId { get; init; }
    public required string NodeId { get; init; }
    public required Guid UserId { get; init; }
    public Dictionary<string, object> InputData { get; init; } = new();
    public Dictionary<string, object> Parameters { get; init; } = new();
    public required IServiceProvider Services { get; init; }
    public CancellationToken CancellationToken { get; init; } = default;
    public Dictionary<string, object> WorkflowContext { get; init; } = new();

    // Helper methods
    public T GetService<T>() where T : notnull
        => Services.GetRequiredService<T>();

    public object? GetParameter(string name)
        => Parameters.GetValueOrDefault(name);

    public T? GetParameter<T>(string name)
    {
        var value = GetParameter(name);
        if (value == null) return default;

        if (value is T typedValue)
            return typedValue;

        try
        {
            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch
        {
            return default;
        }
    }

    public object? GetInput(string key)
        => InputData.GetValueOrDefault(key);

    public T? GetInput<T>(string key)
    {
        var value = GetInput(key);
        if (value == null) return default;

        if (value is T typedValue)
            return typedValue;

        try
        {
            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch
        {
            return default;
        }
    }
}