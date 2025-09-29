using Microsoft.Extensions.Logging;
using Moq;
using WorkflowEngine.Nodes.Core;
using WorkflowEngine.Nodes.Expressions;
using WorkflowEngine.Nodes.Models;

namespace WorkflowEngine.UnitTests.Nodes;

public class ManualTriggerNodeTests
{
    private readonly ManualTriggerNode _node;
    private readonly Mock<IServiceProvider> _serviceProviderMock;

    public ManualTriggerNodeTests()
    {
        var loggerMock = new Mock<ILogger<ManualTriggerNode>>();
        var expressionEvaluator = new ExpressionEvaluator();
        _serviceProviderMock = new Mock<IServiceProvider>();

        _node = new ManualTriggerNode(loggerMock.Object, expressionEvaluator);
    }

    [Fact]
    public void GetDefinition_ReturnsCorrectMetadata()
    {
        var definition = _node.GetDefinition();

        Assert.Equal("manual_trigger", definition.Type);
        Assert.Equal("Manual Trigger", definition.Name);
        Assert.Equal("Triggers", definition.Category);
        Assert.NotEmpty(definition.Operations);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsSuccessResult()
    {
        var context = new NodeExecutionContext
        {
            ExecutionId = Guid.NewGuid(),
            NodeId = "test-node",
            UserId = Guid.NewGuid(),
            Services = _serviceProviderMock.Object,
            InputData = new Dictionary<string, object> { ["test"] = "value" }
        };

        var result = await _node.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Contains("triggered", result.OutputData);
        Assert.True((bool)result.OutputData["triggered"]);
    }

    [Fact]
    public async Task ExecuteAsync_IncludesTimestamp()
    {
        var context = new NodeExecutionContext
        {
            ExecutionId = Guid.NewGuid(),
            NodeId = "test-node",
            UserId = Guid.NewGuid(),
            Services = _serviceProviderMock.Object
        };

        var result = await _node.ExecuteAsync(context);

        Assert.Contains("timestamp", result.OutputData);
        Assert.IsType<DateTime>(result.OutputData["timestamp"]);
    }
}