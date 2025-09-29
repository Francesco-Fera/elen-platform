using Microsoft.Extensions.Logging;
using Moq;
using WorkflowEngine.Nodes.Core;
using WorkflowEngine.Nodes.Expressions;
using WorkflowEngine.Nodes.Models;

namespace WorkflowEngine.UnitTests.Nodes;

public class SetVariableNodeTests
{
    private readonly SetVariableNode _node;
    private readonly Mock<IServiceProvider> _serviceProviderMock;

    public SetVariableNodeTests()
    {
        var loggerMock = new Mock<ILogger<SetVariableNode>>();
        var expressionEvaluator = new ExpressionEvaluator();
        _serviceProviderMock = new Mock<IServiceProvider>();

        _node = new SetVariableNode(loggerMock.Object, expressionEvaluator);
    }

    [Fact]
    public void GetDefinition_ReturnsCorrectMetadata()
    {
        var definition = _node.GetDefinition();

        Assert.Equal("set_variable", definition.Type);
        Assert.Equal("Set Variable", definition.Name);
        Assert.Equal("Processing", definition.Category);
    }

    [Fact]
    public async Task ExecuteAsync_SetsVariables_Success()
    {
        var context = new NodeExecutionContext
        {
            ExecutionId = Guid.NewGuid(),
            NodeId = "test-node",
            UserId = Guid.NewGuid(),
            Services = _serviceProviderMock.Object,
            Parameters = new Dictionary<string, object>
            {
                ["variables"] = new List<Dictionary<string, object>>
                {
                    new() { ["name"] = "var1", ["value"] = "value1" },
                    new() { ["name"] = "var2", ["value"] = 123 }
                }
            }
        };

        var result = await _node.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal("value1", result.OutputData["var1"]);
        Assert.Equal(123, result.OutputData["var2"]);
    }

    [Fact]
    public async Task ExecuteAsync_EmptyVariables_ReturnsEmptyOutput()
    {
        var context = new NodeExecutionContext
        {
            ExecutionId = Guid.NewGuid(),
            NodeId = "test-node",
            UserId = Guid.NewGuid(),
            Services = _serviceProviderMock.Object,
            Parameters = new Dictionary<string, object>
            {
                ["variables"] = new List<Dictionary<string, object>>()
            }
        };

        var result = await _node.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Empty(result.OutputData);
    }
}