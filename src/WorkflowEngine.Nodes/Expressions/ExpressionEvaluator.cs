using System.Text.Json;
using System.Text.RegularExpressions;
using WorkflowEngine.Nodes.Interfaces;

namespace WorkflowEngine.Nodes.Expressions;

public class ExpressionEvaluator : IExpressionEvaluator
{
    private static readonly Regex ExpressionPattern = new(@"\{\{([^}]+)\}\}", RegexOptions.Compiled);

    public bool IsExpression(string value)
    {
        return !string.IsNullOrEmpty(value) && ExpressionPattern.IsMatch(value);
    }

    public async Task<object?> EvaluateAsync(string expression, Dictionary<string, object> context)
    {
        if (!IsExpression(expression))
            return expression;

        var result = expression;
        var matches = ExpressionPattern.Matches(expression);

        foreach (Match match in matches)
        {
            var path = match.Groups[1].Value.Trim();
            var value = await EvaluatePathAsync(path, context);
            var valueStr = value?.ToString() ?? "";
            result = result.Replace(match.Value, valueStr);
        }

        return result;
    }

    public async Task<Dictionary<string, object>> EvaluateParametersAsync(
        Dictionary<string, object> parameters,
        Dictionary<string, object> context)
    {
        var result = new Dictionary<string, object>();

        foreach (var (key, value) in parameters)
        {
            result[key] = value switch
            {
                string str => await EvaluateAsync(str, context) ?? str,
                Dictionary<string, object> dict => await EvaluateParametersAsync(dict, context),
                _ => value
            };
        }

        return result;
    }

    private Task<object?> EvaluatePathAsync(string path, Dictionary<string, object> context)
    {
        var parts = path.Split('.');
        object? current = context;

        foreach (var part in parts)
        {
            if (current == null) return Task.FromResult<object?>(null);

            if (current is Dictionary<string, object> dict)
            {
                if (!dict.TryGetValue(part, out current))
                    return Task.FromResult<object?>(null);
            }
            else if (current is JsonElement jsonElement)
            {
                if (jsonElement.ValueKind == JsonValueKind.Object &&
                    jsonElement.TryGetProperty(part, out var prop))
                {
                    current = prop;
                }
                else
                {
                    return Task.FromResult<object?>(null);
                }
            }
            else
            {
                var property = current.GetType().GetProperty(part);
                if (property == null) return Task.FromResult<object?>(null);
                current = property.GetValue(current);
            }
        }

        return Task.FromResult(current);
    }
}