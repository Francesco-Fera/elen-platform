using Microsoft.Extensions.Logging;
using Moq;
using WorkflowEngine.Execution.Engine;
using WorkflowEngine.Execution.Exceptions;
using WorkflowEngine.Execution.Models;
using WorkflowEngine.Nodes.Interfaces;
using WorkflowEngine.Nodes.Models;

namespace WorkflowEngine.UnitTests.Execution;

public class NodeExecutorTests
{
    private readonly Mock<INodeRegistry> _nodeRegistryMock;
    private readonly Mock<IExpressionEvaluator> _expressionEvaluatorMock;
    private readonly Mock<ILogger<NodeExecutor>> _loggerMock;
    private readonly NodeExecutor _executor;

    public NodeExecutorTests()
    {
        _nodeRegistryMock = new Mock<INodeRegistry>();
        _expressionEvaluatorMock = new Mock<IExpressionEvaluator>();
        _loggerMock = new Mock<ILogger<NodeExecutor>>();
        _executor = new NodeExecutor(
            _nodeRegistryMock.Object,
            _expressionEvaluatorMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task ExecuteNodeAsync_SuccessfulExecution_ReturnsSuccess()
    {
        var node = new WorkflowNode
        {
            Id = "node1",
            Type = "test_node",
            Name = "Test Node",
            Parameters = new Dictionary<string, object> { ["key"] = "value" }
        };

        var context = new NodeExecutionContext
        {
            ExecutionId = Guid.NewGuid(),
            NodeId = node.Id,
            UserId = Guid.NewGuid(),
            Parameters = node.Parameters,
            InputData = new(),
            WorkflowContext = new(),
            Services = Mock.Of<IServiceProvider>()
        };

        var expectedResult = new NodeExecutionResult
        {
            Success = true,
            OutputData = new Dictionary<string, object> { ["result"] = "success" }
        };

        var nodeMock = new Mock<INode>();
        nodeMock.Setup(n => n.ExecuteAsync(It.IsAny<NodeExecutionContext>()))
            .ReturnsAsync(expectedResult);

        _nodeRegistryMock.Setup(r => r.CreateNode(node.Type))
            .Returns(nodeMock.Object);

        _expressionEvaluatorMock.Setup(e => e.EvaluateParametersAsync(
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<Dictionary<string, object>>()))
            .ReturnsAsync((Dictionary<string, object> p, Dictionary<string, object> c) => p);

        var result = await _executor.ExecuteNodeAsync(node, context);

        Assert.True(result.Success);
        Assert.Equal("success", result.OutputData["result"]);
        nodeMock.Verify(n => n.ExecuteAsync(It.IsAny<NodeExecutionContext>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteNodeAsync_NodeNotRegistered_ReturnsError()
    {
        var node = new WorkflowNode
        {
            Id = "node1",
            Type = "nonexistent_node",
            Name = "Test Node"
        };

        var context = new NodeExecutionContext
        {
            ExecutionId = Guid.NewGuid(),
            NodeId = node.Id,
            UserId = Guid.NewGuid(),
            Parameters = new(),
            InputData = new(),
            WorkflowContext = new(),
            Services = Mock.Of<IServiceProvider>()
        };

        _nodeRegistryMock.Setup(r => r.CreateNode(node.Type))
            .Returns((INode)null!);

        var result = await _executor.ExecuteNodeAsync(node, context);

        Assert.False(result.Success);
        Assert.Contains("not registered", result.ErrorMessage);
    }

    [Fact]
    public async Task ExecuteNodeAsync_NodeThrowsException_ReturnsError()
    {
        var node = new WorkflowNode
        {
            Id = "node1",
            Type = "test_node",
            Name = "Test Node"
        };

        var context = new NodeExecutionContext
        {
            ExecutionId = Guid.NewGuid(),
            NodeId = node.Id,
            UserId = Guid.NewGuid(),
            Parameters = new(),
            InputData = new(),
            WorkflowContext = new(),
            Services = Mock.Of<IServiceProvider>()
        };

        var nodeMock = new Mock<INode>();
        nodeMock.Setup(n => n.ExecuteAsync(It.IsAny<NodeExecutionContext>()))
            .ThrowsAsync(new InvalidOperationException("Test exception"));

        _nodeRegistryMock.Setup(r => r.CreateNode(node.Type))
            .Returns(nodeMock.Object);

        _expressionEvaluatorMock.Setup(e => e.EvaluateParametersAsync(
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<Dictionary<string, object>>()))
            .ReturnsAsync((Dictionary<string, object> p, Dictionary<string, object> c) => p);

        var result = await _executor.ExecuteNodeAsync(node, context);

        Assert.False(result.Success);
        Assert.Contains("Test exception", result.ErrorMessage);
    }

    [Fact]
    public async Task ExecuteNodeAsync_WithRetry_RetriesOnFailure()
    {
        var node = new WorkflowNode
        {
            Id = "node1",
            Type = "test_node",
            Name = "Test Node",
            Configuration = new Dictionary<string, object>
            {
                ["maxRetries"] = 2,
                ["retryDelay"] = 10
            }
        };

        var context = new NodeExecutionContext
        {
            ExecutionId = Guid.NewGuid(),
            NodeId = node.Id,
            UserId = Guid.NewGuid(),
            Parameters = new(),
            InputData = new(),
            WorkflowContext = new(),
            Services = Mock.Of<IServiceProvider>()
        };

        var callCount = 0;
        var nodeMock = new Mock<INode>();
        nodeMock.Setup(n => n.ExecuteAsync(It.IsAny<NodeExecutionContext>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                if (callCount < 2)
                {
                    return new NodeExecutionResult
                    {
                        Success = false,
                        ErrorMessage = "Temporary failure"
                    };
                }
                return new NodeExecutionResult { Success = true };
            });

        _nodeRegistryMock.Setup(r => r.CreateNode(node.Type))
            .Returns(nodeMock.Object);

        _expressionEvaluatorMock.Setup(e => e.EvaluateParametersAsync(
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<Dictionary<string, object>>()))
            .ReturnsAsync((Dictionary<string, object> p, Dictionary<string, object> c) => p);

        var result = await _executor.ExecuteNodeAsync(node, context);

        Assert.True(result.Success);
        Assert.Equal(2, callCount);
    }

    [Fact]
    public async Task ExecuteNodeAsync_ExhaustsRetries_ReturnsLastError()
    {
        var node = new WorkflowNode
        {
            Id = "node1",
            Type = "test_node",
            Name = "Test Node",
            Configuration = new Dictionary<string, object>
            {
                ["maxRetries"] = 2,
                ["retryDelay"] = 10
            }
        };

        var context = new NodeExecutionContext
        {
            ExecutionId = Guid.NewGuid(),
            NodeId = node.Id,
            UserId = Guid.NewGuid(),
            Parameters = new(),
            InputData = new(),
            WorkflowContext = new(),
            Services = Mock.Of<IServiceProvider>()
        };

        var nodeMock = new Mock<INode>();
        nodeMock.Setup(n => n.ExecuteAsync(It.IsAny<NodeExecutionContext>()))
            .ReturnsAsync(new NodeExecutionResult
            {
                Success = false,
                ErrorMessage = "Persistent failure"
            });

        _nodeRegistryMock.Setup(r => r.CreateNode(node.Type))
            .Returns(nodeMock.Object);

        _expressionEvaluatorMock.Setup(e => e.EvaluateParametersAsync(
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<Dictionary<string, object>>()))
            .ReturnsAsync((Dictionary<string, object> p, Dictionary<string, object> c) => p);

        var result = await _executor.ExecuteNodeAsync(node, context);

        Assert.False(result.Success);
        Assert.Contains("Persistent failure", result.ErrorMessage);
        nodeMock.Verify(n => n.ExecuteAsync(It.IsAny<NodeExecutionContext>()), Times.Exactly(3));
    }

    [Fact]
    public async Task ExecuteNodeAsync_Timeout_ReturnsTimeoutError()
    {
        var node = new WorkflowNode
        {
            Id = "node1",
            Type = "test_node",
            Name = "Test Node",
            Configuration = new Dictionary<string, object>
            {
                ["timeout"] = 100
            }
        };

        var context = new NodeExecutionContext
        {
            ExecutionId = Guid.NewGuid(),
            NodeId = node.Id,
            UserId = Guid.NewGuid(),
            Parameters = new(),
            InputData = new(),
            WorkflowContext = new(),
            Services = Mock.Of<IServiceProvider>()
        };

        var nodeMock = new Mock<INode>();
        nodeMock.Setup(n => n.ExecuteAsync(It.IsAny<NodeExecutionContext>()))
            .Returns(async (NodeExecutionContext ctx) =>
            {
                await Task.Delay(5000, ctx.CancellationToken);
                return new NodeExecutionResult { Success = true };
            });

        _nodeRegistryMock.Setup(r => r.CreateNode(node.Type))
            .Returns(nodeMock.Object);

        _expressionEvaluatorMock.Setup(e => e.EvaluateParametersAsync(
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<Dictionary<string, object>>()))
            .ReturnsAsync((Dictionary<string, object> p, Dictionary<string, object> c) => p);

        var result = await _executor.ExecuteNodeAsync(node, context);

        Assert.False(result.Success);
        Assert.Contains("timed out", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteNodeAsync_CancellationRequested_ThrowsOperationCanceledException()
    {
        var node = new WorkflowNode
        {
            Id = "node1",
            Type = "test_node",
            Name = "Test Node"
        };

        var cts = new CancellationTokenSource();
        var context = new NodeExecutionContext
        {
            ExecutionId = Guid.NewGuid(),
            NodeId = node.Id,
            UserId = Guid.NewGuid(),
            Parameters = new(),
            InputData = new(),
            WorkflowContext = new(),
            Services = Mock.Of<IServiceProvider>(),
            CancellationToken = cts.Token
        };

        var nodeMock = new Mock<INode>();
        nodeMock.Setup(n => n.ExecuteAsync(It.IsAny<NodeExecutionContext>()))
            .Returns(async (NodeExecutionContext ctx) =>
            {
                await Task.Delay(1000, ctx.CancellationToken);
                return new NodeExecutionResult { Success = true };
            });

        _nodeRegistryMock.Setup(r => r.CreateNode(node.Type))
            .Returns(nodeMock.Object);

        _expressionEvaluatorMock.Setup(e => e.EvaluateParametersAsync(
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<Dictionary<string, object>>()))
            .ReturnsAsync((Dictionary<string, object> p, Dictionary<string, object> c) => p);

        cts.Cancel();

        await Assert.ThrowsAsync<TaskCanceledException>(
            () => _executor.ExecuteNodeAsync(node, context, cts.Token));
    }

    [Fact]
    public async Task ExecuteNodeAsync_EvaluatesExpressions_BeforeExecution()
    {
        var node = new WorkflowNode
        {
            Id = "node1",
            Type = "test_node",
            Name = "Test Node",
            Parameters = new Dictionary<string, object>
            {
                ["url"] = "{{$node.previous.data.apiUrl}}"
            }
        };

        var context = new NodeExecutionContext
        {
            ExecutionId = Guid.NewGuid(),
            NodeId = node.Id,
            UserId = Guid.NewGuid(),
            Parameters = node.Parameters,
            InputData = new(),
            WorkflowContext = new Dictionary<string, object>
            {
                ["$node.previous"] = new Dictionary<string, object>
                {
                    ["data"] = new Dictionary<string, object>
                    {
                        ["apiUrl"] = "https://api.example.com"
                    }
                }
            },
            Services = Mock.Of<IServiceProvider>()
        };

        var evaluatedParams = new Dictionary<string, object>
        {
            ["url"] = "https://api.example.com"
        };

        var nodeMock = new Mock<INode>();
        nodeMock.Setup(n => n.ExecuteAsync(It.IsAny<NodeExecutionContext>()))
            .ReturnsAsync(new NodeExecutionResult { Success = true });

        _nodeRegistryMock.Setup(r => r.CreateNode(node.Type))
            .Returns(nodeMock.Object);

        _expressionEvaluatorMock.Setup(e => e.EvaluateParametersAsync(
                It.Is<Dictionary<string, object>>(p => p.ContainsKey("url")),
                It.IsAny<Dictionary<string, object>>()))
            .ReturnsAsync(evaluatedParams);

        _expressionEvaluatorMock.Setup(e => e.EvaluateParametersAsync(
                It.Is<Dictionary<string, object>>(p => !p.ContainsKey("url")),
                It.IsAny<Dictionary<string, object>>()))
            .ReturnsAsync((Dictionary<string, object> p, Dictionary<string, object> c) => p);

        var result = await _executor.ExecuteNodeAsync(node, context);

        Assert.True(result.Success);
        _expressionEvaluatorMock.Verify(e => e.EvaluateParametersAsync(
            It.IsAny<Dictionary<string, object>>(),
            It.IsAny<Dictionary<string, object>>()), Times.Exactly(2));
    }

    [Fact]
    public async Task ExecuteNodeAsync_ExpressionEvaluationFails_ThrowsException()
    {
        var node = new WorkflowNode
        {
            Id = "node1",
            Type = "test_node",
            Name = "Test Node",
            Parameters = new Dictionary<string, object>
            {
                ["value"] = "{{$node.nonexistent.data}}"
            }
        };

        var context = new NodeExecutionContext
        {
            ExecutionId = Guid.NewGuid(),
            NodeId = node.Id,
            UserId = Guid.NewGuid(),
            Parameters = node.Parameters,
            InputData = new(),
            WorkflowContext = new(),
            Services = Mock.Of<IServiceProvider>()
        };

        _nodeRegistryMock.Setup(r => r.CreateNode(node.Type))
            .Returns(Mock.Of<INode>());

        _expressionEvaluatorMock.Setup(e => e.EvaluateParametersAsync(
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<Dictionary<string, object>>()))
            .ThrowsAsync(new InvalidOperationException("Expression evaluation failed"));

        await Assert.ThrowsAsync<WorkflowExecutionException>(
            () => _executor.ExecuteNodeAsync(node, context));
    }
}