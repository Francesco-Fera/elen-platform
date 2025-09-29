using WorkflowEngine.Nodes.Expressions;

namespace WorkflowEngine.UnitTests.Nodes;

public class ExpressionEvaluatorTests
{
    private readonly ExpressionEvaluator _evaluator;

    public ExpressionEvaluatorTests()
    {
        _evaluator = new ExpressionEvaluator();
    }

    [Fact]
    public void IsExpression_WithValidExpression_ReturnsTrue()
    {
        Assert.True(_evaluator.IsExpression("{{$node.Start.data}}"));
        Assert.True(_evaluator.IsExpression("Hello {{name}}"));
    }

    [Fact]
    public void IsExpression_WithoutExpression_ReturnsFalse()
    {
        Assert.False(_evaluator.IsExpression("regular string"));
        Assert.False(_evaluator.IsExpression(""));
        Assert.False(_evaluator.IsExpression(null!));
    }

    [Fact]
    public async Task EvaluateAsync_SimpleExpression_ReturnsValue()
    {
        var context = new Dictionary<string, object>
        {
            ["name"] = "John"
        };

        var result = await _evaluator.EvaluateAsync("{{name}}", context);

        Assert.Equal("John", result);
    }

    [Fact]
    public async Task EvaluateAsync_NestedPath_ReturnsValue()
    {
        var context = new Dictionary<string, object>
        {
            ["user"] = new Dictionary<string, object>
            {
                ["name"] = "John",
                ["email"] = "john@example.com"
            }
        };

        var result = await _evaluator.EvaluateAsync("{{user.email}}", context);

        Assert.Equal("john@example.com", result);
    }

    [Fact]
    public async Task EvaluateAsync_MultipleExpressions_ReplacesAll()
    {
        var context = new Dictionary<string, object>
        {
            ["firstName"] = "John",
            ["lastName"] = "Doe"
        };

        var result = await _evaluator.EvaluateAsync("Hello {{firstName}} {{lastName}}", context);

        Assert.Equal("Hello John Doe", result);
    }

    [Fact]
    public async Task EvaluateAsync_MissingValue_ReturnsEmptyString()
    {
        var context = new Dictionary<string, object>();

        var result = await _evaluator.EvaluateAsync("{{missing}}", context);

        Assert.Equal("", result);
    }

    [Fact]
    public async Task EvaluateParametersAsync_EvaluatesStringParameters()
    {
        var parameters = new Dictionary<string, object>
        {
            ["url"] = "https://api.example.com/{{userId}}",
            ["method"] = "GET"
        };

        var context = new Dictionary<string, object>
        {
            ["userId"] = "123"
        };

        var result = await _evaluator.EvaluateParametersAsync(parameters, context);

        Assert.Equal("https://api.example.com/123", result["url"]);
        Assert.Equal("GET", result["method"]);
    }

    [Fact]
    public async Task EvaluateParametersAsync_HandlesNestedDictionaries()
    {
        var parameters = new Dictionary<string, object>
        {
            ["headers"] = new Dictionary<string, object>
            {
                ["Authorization"] = "Bearer {{token}}"
            }
        };

        var context = new Dictionary<string, object>
        {
            ["token"] = "abc123"
        };

        var result = await _evaluator.EvaluateParametersAsync(parameters, context);

        var headers = (Dictionary<string, object>)result["headers"];
        Assert.Equal("Bearer abc123", headers["Authorization"]);
    }
}