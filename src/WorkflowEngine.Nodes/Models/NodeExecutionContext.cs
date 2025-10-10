using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace WorkflowEngine.Nodes.Models;
public record NodeExecutionContext
{
    public required Guid ExecutionId { get; init; }
    public required string NodeId { get; init; }
    public required Guid UserId { get; init; }
    public Dictionary<string, object> InputData { get; init; } = new();
    public Dictionary<string, object> Parameters { get; init; } = new();
    public required IServiceProvider Services { get; init; }
    public CancellationToken CancellationToken { get; init; } = default;
    public Dictionary<string, object> WorkflowContext { get; init; } = new();

    public T GetService<T>() where T : notnull
        => Services.GetRequiredService<T>();

    public object? GetParameter(string name)
        => Parameters.GetValueOrDefault(name);

    public T? GetParameter<T>(string name)
    {
        var value = GetParameter(name);
        if (value == null) return default;

        // Direct type match
        if (value is T typedValue)
            return typedValue;

        // Handle JsonElement from System.Text.Json deserialization
        if (value is JsonElement jsonElement)
        {
            try
            {
                return JsonSerializer.Deserialize<T>(jsonElement.GetRawText());
            }
            catch (JsonException)
            {
                // If deserialization fails, try primitive conversion
                return TryConvertPrimitive<T>(jsonElement);
            }
        }

        // Handle primitive type conversion
        if (typeof(T).IsPrimitive || typeof(T) == typeof(string) || typeof(T) == typeof(decimal))
        {
            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return default;
            }
        }

        // Try JSON serialization round-trip for complex types
        try
        {
            var json = JsonSerializer.Serialize(value);
            return JsonSerializer.Deserialize<T>(json);
        }
        catch
        {
            return default;
        }
    }

    private T? TryConvertPrimitive<T>(JsonElement jsonElement)
    {
        try
        {
            var targetType = typeof(T);

            if (targetType == typeof(string))
                return (T)(object)jsonElement.GetString()!;

            if (targetType == typeof(int) || targetType == typeof(int?))
                return (T)(object)jsonElement.GetInt32();

            if (targetType == typeof(long) || targetType == typeof(long?))
                return (T)(object)jsonElement.GetInt64();

            if (targetType == typeof(bool) || targetType == typeof(bool?))
                return (T)(object)jsonElement.GetBoolean();

            if (targetType == typeof(double) || targetType == typeof(double?))
                return (T)(object)jsonElement.GetDouble();

            if (targetType == typeof(decimal) || targetType == typeof(decimal?))
                return (T)(object)jsonElement.GetDecimal();

            return default;
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

        // Handle JsonElement
        if (value is JsonElement jsonElement)
        {
            try
            {
                return JsonSerializer.Deserialize<T>(jsonElement.GetRawText());
            }
            catch
            {
                return TryConvertPrimitive<T>(jsonElement);
            }
        }

        // Primitive conversion
        if (typeof(T).IsPrimitive || typeof(T) == typeof(string) || typeof(T) == typeof(decimal))
        {
            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return default;
            }
        }

        // JSON round-trip
        try
        {
            var json = JsonSerializer.Serialize(value);
            return JsonSerializer.Deserialize<T>(json);
        }
        catch
        {
            return default;
        }
    }
}