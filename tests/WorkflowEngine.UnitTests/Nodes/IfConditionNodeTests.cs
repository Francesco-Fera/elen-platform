using Microsoft.Extensions.Logging;
using Moq;
using WorkflowEngine.Nodes.Core;
using WorkflowEngine.Nodes.Expressions;
using WorkflowEngine.Nodes.Models;

namespace WorkflowEngine.UnitTests.Nodes;

public class IfConditionNodeTests
{
    private readonly IfConditionNode _node;
    private readonly Mock<IServiceProvider> _serviceProviderMock;

    public IfConditionNodeTests()
    {
        var loggerMock = new Mock<ILogger<IfConditionNode>>();
        var expressionEvaluator = new ExpressionEvaluator();
        _serviceProviderMock = new Mock<IServiceProvider>();

        _node = new IfConditionNode(loggerMock.Object, expressionEvaluator);
    }

    [Theory]
    [InlineData("equals", "test", "test", true)]
    [InlineData("equals", "test", "other", false)]
    [InlineData("notEquals", "test", "other", true)]
    [InlineData("notEquals", "test", "test", false)]
    public async Task ExecuteAsync_Comparison_ReturnsExpectedResult(string op, string val1, string val2, bool expected)
    {
        var context = new NodeExecutionContext
        {
            ExecutionId = Guid.NewGuid(),
            NodeId = "test-node",
            UserId = Guid.NewGuid(),
            Services = _serviceProviderMock.Object,
            Parameters = new Dictionary<string, object>
            {
                ["value1"] = val1,
                ["operator"] = op,
                ["value2"] = val2
            }
        };

        var result = await _node.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal(expected, result.OutputData["result"]);
    }

    [Theory]
    [InlineData("10", "5", true)]
    [InlineData("5", "10", false)]
    public async Task ExecuteAsync_NumericComparison_GreaterThan(string val1, string val2, bool expected)
    {
        var context = new NodeExecutionContext
        {
            ExecutionId = Guid.NewGuid(),
            NodeId = "test-node",
            UserId = Guid.NewGuid(),
            Services = _serviceProviderMock.Object,
            Parameters = new Dictionary<string, object>
            {
                ["value1"] = val1,
                ["operator"] = "greaterThan",
                ["value2"] = val2
            }
        };

        var result = await _node.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal(expected, result.OutputData["result"]);
    }

    [Fact]
    public async Task ExecuteAsync_Contains_Success()
    {
        var context = new NodeExecutionContext
        {
            ExecutionId = Guid.NewGuid(),
            NodeId = "test-node",
            UserId = Guid.NewGuid(),
            Services = _serviceProviderMock.Object,
            Parameters = new Dictionary<string, object>
            {
                ["value1"] = "hello world",
                ["operator"] = "contains",
                ["value2"] = "world"
            }
        };

        var result = await _node.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.True((bool)result.OutputData["result"]);
    }
}