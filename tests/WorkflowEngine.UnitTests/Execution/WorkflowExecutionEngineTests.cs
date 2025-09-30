using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using WorkflowEngine.Application.DTOs.Workflow;
using WorkflowEngine.Core.Entities;
using WorkflowEngine.Core.Enums;
using WorkflowEngine.Execution.Engine;
using WorkflowEngine.Execution.Exceptions;
using WorkflowEngine.Execution.Interfaces;
using WorkflowEngine.Execution.Models;
using WorkflowEngine.Infrastructure.Data;
using WorkflowEngine.Nodes.Models;

namespace WorkflowEngine.UnitTests.Execution;

public class WorkflowExecutionEngineTests : IDisposable
{
    private readonly WorkflowEngineDbContext _dbContext;
    private readonly Mock<IWorkflowGraphBuilder> _graphBuilderMock;
    private readonly Mock<ITopologicalSorter> _topologicalSorterMock;
    private readonly Mock<INodeExecutor> _nodeExecutorMock;
    private readonly Mock<IExecutionLogger> _executionLoggerMock;
    private readonly Mock<IExecutionContextFactory> _contextFactoryMock;
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly Mock<ILogger<WorkflowExecutionEngine>> _loggerMock;
    private readonly WorkflowExecutionEngine _engine;

    private void SetupBasicGraphMocks(
        WorkflowGraph graph,
        List<string> executionOrder,
        bool enableParallel = false)
    {
        _graphBuilderMock.Setup(g => g.BuildGraph(
                It.IsAny<List<WorkflowNodeDto>>(),
                It.IsAny<List<NodeConnectionDto>>()))
            .Returns(graph);

        _graphBuilderMock.Setup(g => g.ValidateGraphAsync(
                It.IsAny<WorkflowGraph>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _topologicalSorterMock.Setup(t => t.Sort(It.IsAny<WorkflowGraph>()))
            .Returns(executionOrder);

        List<List<string>> parallelGroups;
        if (enableParallel)
        {
            parallelGroups = new List<List<string>>();
            foreach (var nodeId in executionOrder)
            {
                parallelGroups.Add(new List<string> { nodeId });
            }
        }
        else
        {
            parallelGroups = executionOrder.Select(nodeId => new List<string> { nodeId }).ToList();
        }

        _topologicalSorterMock.Setup(t => t.GetParallelGroups(It.IsAny<WorkflowGraph>()))
            .Returns(parallelGroups);
    }

    private void SetupContextFactoryMocks()
    {
        _contextFactoryMock.Setup(c => c.CreateWorkflowContext(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>()))
            .Returns(new Dictionary<string, object>());

        _contextFactoryMock.Setup(c => c.GetInputDataForNode(
                It.IsAny<WorkflowNode>(),
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<List<NodeConnectionDto>>()))
            .Returns(new Dictionary<string, object>());
    }


    public WorkflowExecutionEngineTests()
    {
        var options = new DbContextOptionsBuilder<WorkflowEngineDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new WorkflowEngineDbContext(options);
        _graphBuilderMock = new Mock<IWorkflowGraphBuilder>();
        _topologicalSorterMock = new Mock<ITopologicalSorter>();
        _nodeExecutorMock = new Mock<INodeExecutor>();
        _executionLoggerMock = new Mock<IExecutionLogger>();
        _contextFactoryMock = new Mock<IExecutionContextFactory>();
        _serviceProviderMock = new Mock<IServiceProvider>();
        _loggerMock = new Mock<ILogger<WorkflowExecutionEngine>>();

        _engine = new WorkflowExecutionEngine(
            _dbContext,
            _graphBuilderMock.Object,
            _topologicalSorterMock.Object,
            _nodeExecutorMock.Object,
            _executionLoggerMock.Object,
            _contextFactoryMock.Object,
            _serviceProviderMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_SimpleLinearWorkflow_ExecutesSuccessfully()
    {
        var workflow = CreateTestWorkflow(new List<WorkflowNodeDto>
        {
            new() { Id = "node1", Type = "manual_trigger", Name = "Start", Parameters = new() },
            new() { Id = "node2", Type = "http_request", Name = "API", Parameters = new() }
        }, new List<NodeConnectionDto>
        {
            new() { SourceNodeId = "node1", TargetNodeId = "node2" }
        });

        var graph = new WorkflowGraph();
        graph.AddNode(new WorkflowNode { Id = "node1", Type = "manual_trigger", Name = "Start" });
        graph.AddNode(new WorkflowNode { Id = "node2", Type = "http_request", Name = "API" });

        _graphBuilderMock.Setup(g => g.BuildGraph(It.IsAny<List<WorkflowNodeDto>>(), It.IsAny<List<NodeConnectionDto>>()))
            .Returns(graph);

        _graphBuilderMock.Setup(g => g.ValidateGraphAsync(It.IsAny<WorkflowGraph>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _topologicalSorterMock.Setup(t => t.Sort(It.IsAny<WorkflowGraph>()))
            .Returns(new List<string> { "node1", "node2" });

        _contextFactoryMock.Setup(c => c.CreateWorkflowContext(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
            .Returns(new Dictionary<string, object>());

        _contextFactoryMock.Setup(c => c.GetInputDataForNode(
                It.IsAny<WorkflowNode>(), It.IsAny<Dictionary<string, object>>(), It.IsAny<List<NodeConnectionDto>>()))
            .Returns(new Dictionary<string, object>());

        _nodeExecutorMock.Setup(n => n.ExecuteNodeAsync(
                It.IsAny<WorkflowNode>(), It.IsAny<NodeExecutionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NodeExecutionResult { Success = true, OutputData = new() });

        var result = await _engine.ExecuteAsync(workflow);

        Assert.True(result.IsSuccess);
        Assert.Equal(ExecutionStatus.Completed, result.Status);
        Assert.Equal(2, result.NodeExecutions.Count);

        var execution = await _dbContext.WorkflowExecutions.FirstAsync();
        Assert.Equal(ExecutionStatus.Completed, execution.Status);
        Assert.NotNull(execution.CompletedAt);
    }

    //[Fact]
    //public async Task ExecuteAsync_EmptyWorkflow_ThrowsException()
    //{
    //    var workflow = CreateTestWorkflow(new List<WorkflowNodeDto>(), new List<NodeConnectionDto>());

    //    await Assert.ThrowsAsync<WorkflowExecutionException>(() => _engine.ExecuteAsync(workflow));
    //}

    [Fact]
    public async Task ExecuteAsync_NodeFails_StopsExecution()
    {
        var workflow = CreateTestWorkflow(new List<WorkflowNodeDto>
        {
            new() { Id = "node1", Type = "manual_trigger", Name = "Start", Parameters = new() },
            new() { Id = "node2", Type = "http_request", Name = "API", Parameters = new() },
            new() { Id = "node3", Type = "set_variable", Name = "Set", Parameters = new() }
        }, new List<NodeConnectionDto>
        {
            new() { SourceNodeId = "node1", TargetNodeId = "node2" },
            new() { SourceNodeId = "node2", TargetNodeId = "node3" }
        });

        var graph = new WorkflowGraph();
        graph.AddNode(new WorkflowNode { Id = "node1", Type = "manual_trigger", Name = "Start" });
        graph.AddNode(new WorkflowNode { Id = "node2", Type = "http_request", Name = "API" });
        graph.AddNode(new WorkflowNode { Id = "node3", Type = "set_variable", Name = "Set" });

        _graphBuilderMock.Setup(g => g.BuildGraph(It.IsAny<List<WorkflowNodeDto>>(), It.IsAny<List<NodeConnectionDto>>()))
            .Returns(graph);

        _graphBuilderMock.Setup(g => g.ValidateGraphAsync(It.IsAny<WorkflowGraph>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _topologicalSorterMock.Setup(t => t.Sort(It.IsAny<WorkflowGraph>()))
            .Returns(new List<string> { "node1", "node2", "node3" });

        _contextFactoryMock.Setup(c => c.CreateWorkflowContext(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
            .Returns(new Dictionary<string, object>());

        _contextFactoryMock.Setup(c => c.GetInputDataForNode(
                It.IsAny<WorkflowNode>(), It.IsAny<Dictionary<string, object>>(), It.IsAny<List<NodeConnectionDto>>()))
            .Returns(new Dictionary<string, object>());

        var callCount = 0;
        _nodeExecutorMock.Setup(n => n.ExecuteNodeAsync(
                It.IsAny<WorkflowNode>(), It.IsAny<NodeExecutionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                if (callCount == 2)
                {
                    return new NodeExecutionResult
                    {
                        Success = false,
                        ErrorMessage = "API call failed"
                    };
                }
                return new NodeExecutionResult { Success = true, OutputData = new() };
            });

        var result = await _engine.ExecuteAsync(workflow, null, new ExecutionOptions { ContinueOnError = false });

        Assert.False(result.IsSuccess);
        Assert.Equal(ExecutionStatus.Failed, result.Status);
        Assert.Contains("failed", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(2, result.NodeExecutions.Count);

        var execution = await _dbContext.WorkflowExecutions.FirstAsync();
        Assert.Equal(ExecutionStatus.Failed, execution.Status);
    }

    [Fact]
    public async Task ExecuteAsync_ContinueOnError_CompletesWorkflow()
    {
        var workflow = CreateTestWorkflow(new List<WorkflowNodeDto>
        {
            new() { Id = "node1", Type = "manual_trigger", Name = "Start", Parameters = new() },
            new() { Id = "node2", Type = "http_request", Name = "API", Parameters = new() },
            new() { Id = "node3", Type = "set_variable", Name = "Set", Parameters = new() }
        }, new List<NodeConnectionDto>
        {
            new() { SourceNodeId = "node1", TargetNodeId = "node2" },
            new() { SourceNodeId = "node2", TargetNodeId = "node3" }
        });

        var graph = new WorkflowGraph();
        graph.AddNode(new WorkflowNode { Id = "node1", Type = "manual_trigger", Name = "Start" });
        graph.AddNode(new WorkflowNode { Id = "node2", Type = "http_request", Name = "API" });
        graph.AddNode(new WorkflowNode { Id = "node3", Type = "set_variable", Name = "Set" });

        _graphBuilderMock.Setup(g => g.BuildGraph(It.IsAny<List<WorkflowNodeDto>>(), It.IsAny<List<NodeConnectionDto>>()))
            .Returns(graph);

        _graphBuilderMock.Setup(g => g.ValidateGraphAsync(It.IsAny<WorkflowGraph>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _topologicalSorterMock.Setup(t => t.Sort(It.IsAny<WorkflowGraph>()))
            .Returns(new List<string> { "node1", "node2", "node3" });

        _contextFactoryMock.Setup(c => c.CreateWorkflowContext(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
            .Returns(new Dictionary<string, object>());

        _contextFactoryMock.Setup(c => c.GetInputDataForNode(
                It.IsAny<WorkflowNode>(), It.IsAny<Dictionary<string, object>>(), It.IsAny<List<NodeConnectionDto>>()))
            .Returns(new Dictionary<string, object>());

        var callCount = 0;
        _nodeExecutorMock.Setup(n => n.ExecuteNodeAsync(
                It.IsAny<WorkflowNode>(), It.IsAny<NodeExecutionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                if (callCount == 2)
                {
                    return new NodeExecutionResult
                    {
                        Success = false,
                        ErrorMessage = "API call failed"
                    };
                }
                return new NodeExecutionResult { Success = true, OutputData = new() };
            });

        var result = await _engine.ExecuteAsync(workflow, null, new ExecutionOptions { ContinueOnError = true });

        Assert.True(result.IsSuccess);
        Assert.Equal(ExecutionStatus.Completed, result.Status);
        Assert.Equal(3, result.NodeExecutions.Count);
        Assert.Single(result.NodeExecutions.Where(n => !n.Success));
    }

    [Fact]
    public async Task ExecuteAsync_WithTimeout_ReturnsTimeoutStatus()
    {
        var workflow = CreateTestWorkflow(new List<WorkflowNodeDto>
        {
            new() { Id = "node1", Type = "manual_trigger", Name = "Start", Parameters = new() }
        }, new List<NodeConnectionDto>());

        var graph = new WorkflowGraph();
        graph.AddNode(new WorkflowNode { Id = "node1", Type = "manual_trigger", Name = "Start" });

        _graphBuilderMock.Setup(g => g.BuildGraph(It.IsAny<List<WorkflowNodeDto>>(), It.IsAny<List<NodeConnectionDto>>()))
            .Returns(graph);

        _graphBuilderMock.Setup(g => g.ValidateGraphAsync(It.IsAny<WorkflowGraph>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _topologicalSorterMock.Setup(t => t.Sort(It.IsAny<WorkflowGraph>()))
            .Returns(new List<string> { "node1" });

        _contextFactoryMock.Setup(c => c.CreateWorkflowContext(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
            .Returns(new Dictionary<string, object>());

        _contextFactoryMock.Setup(c => c.GetInputDataForNode(
                It.IsAny<WorkflowNode>(), It.IsAny<Dictionary<string, object>>(), It.IsAny<List<NodeConnectionDto>>()))
            .Returns(new Dictionary<string, object>());

        _nodeExecutorMock.Setup(n => n.ExecuteNodeAsync(
                It.IsAny<WorkflowNode>(), It.IsAny<NodeExecutionContext>(), It.IsAny<CancellationToken>()))
            .Returns(async (WorkflowNode node, NodeExecutionContext ctx, CancellationToken ct) =>
            {
                await Task.Delay(10000, ct);
                return new NodeExecutionResult { Success = true };
            });

        var result = await _engine.ExecuteAsync(workflow, null, new ExecutionOptions { Timeout = TimeSpan.FromMilliseconds(100) });

        Assert.False(result.IsSuccess);
        Assert.Equal(ExecutionStatus.Timeout, result.Status);

        var execution = await _dbContext.WorkflowExecutions.FirstAsync();
        Assert.Equal(ExecutionStatus.Timeout, execution.Status);
    }

    [Fact]
    public async Task ExecuteAsync_WithCancellation_ReturnsCancelledStatus()
    {
        var workflow = CreateTestWorkflow(new List<WorkflowNodeDto>
        {
            new() { Id = "node1", Type = "manual_trigger", Name = "Start", Parameters = new() }
        }, new List<NodeConnectionDto>());

        var graph = new WorkflowGraph();
        graph.AddNode(new WorkflowNode { Id = "node1", Type = "manual_trigger", Name = "Start" });

        _graphBuilderMock.Setup(g => g.BuildGraph(It.IsAny<List<WorkflowNodeDto>>(), It.IsAny<List<NodeConnectionDto>>()))
            .Returns(graph);

        _graphBuilderMock.Setup(g => g.ValidateGraphAsync(It.IsAny<WorkflowGraph>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _topologicalSorterMock.Setup(t => t.Sort(It.IsAny<WorkflowGraph>()))
            .Returns(new List<string> { "node1" });

        _contextFactoryMock.Setup(c => c.CreateWorkflowContext(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
            .Returns(new Dictionary<string, object>());

        _contextFactoryMock.Setup(c => c.GetInputDataForNode(
                It.IsAny<WorkflowNode>(), It.IsAny<Dictionary<string, object>>(), It.IsAny<List<NodeConnectionDto>>()))
            .Returns(new Dictionary<string, object>());

        var cts = new CancellationTokenSource();

        _nodeExecutorMock.Setup(n => n.ExecuteNodeAsync(
                It.IsAny<WorkflowNode>(), It.IsAny<NodeExecutionContext>(), It.IsAny<CancellationToken>()))
            .Returns(async (WorkflowNode node, NodeExecutionContext ctx, CancellationToken ct) =>
            {
                await Task.Delay(10000, ct);
                return new NodeExecutionResult { Success = true };
            });

        cts.Cancel();

        var result = await _engine.ExecuteAsync(workflow, null, null, cts.Token);

        Assert.False(result.IsSuccess);
        Assert.Equal(ExecutionStatus.Cancelled, result.Status);

        var execution = await _dbContext.WorkflowExecutions.FirstAsync();
        Assert.Equal(ExecutionStatus.Cancelled, execution.Status);
    }

    [Fact]
    public async Task ExecuteAsync_ParallelExecution_ExecutesNodesInParallel()
    {
        var workflow = CreateTestWorkflow(new List<WorkflowNodeDto>
        {
            new() { Id = "node1", Type = "manual_trigger", Name = "Start", Parameters = new() },
            new() { Id = "node2", Type = "http_request", Name = "API1", Parameters = new() },
            new() { Id = "node3", Type = "http_request", Name = "API2", Parameters = new() }
        }, new List<NodeConnectionDto>
        {
            new() { SourceNodeId = "node1", TargetNodeId = "node2" },
            new() { SourceNodeId = "node1", TargetNodeId = "node3" }
        });

        var graph = new WorkflowGraph();
        graph.AddNode(new WorkflowNode { Id = "node1", Type = "manual_trigger", Name = "Start" });
        graph.AddNode(new WorkflowNode { Id = "node2", Type = "http_request", Name = "API1" });
        graph.AddNode(new WorkflowNode { Id = "node3", Type = "http_request", Name = "API2" });
        graph.AddEdge(new GraphEdge { SourceNodeId = "node1", TargetNodeId = "node2" });
        graph.AddEdge(new GraphEdge { SourceNodeId = "node1", TargetNodeId = "node3" });

        _graphBuilderMock.Setup(g => g.BuildGraph(It.IsAny<List<WorkflowNodeDto>>(), It.IsAny<List<NodeConnectionDto>>()))
            .Returns(graph);

        _graphBuilderMock.Setup(g => g.ValidateGraphAsync(It.IsAny<WorkflowGraph>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _topologicalSorterMock.Setup(t => t.GetParallelGroups(It.IsAny<WorkflowGraph>()))
            .Returns(new List<List<string>>
            {
                new() { "node1" },
                new() { "node2", "node3" }
            });

        _contextFactoryMock.Setup(c => c.CreateWorkflowContext(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
            .Returns(new Dictionary<string, object>());

        _contextFactoryMock.Setup(c => c.GetInputDataForNode(
                It.IsAny<WorkflowNode>(), It.IsAny<Dictionary<string, object>>(), It.IsAny<List<NodeConnectionDto>>()))
            .Returns(new Dictionary<string, object>());

        _nodeExecutorMock.Setup(n => n.ExecuteNodeAsync(
                It.IsAny<WorkflowNode>(), It.IsAny<NodeExecutionContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NodeExecutionResult { Success = true, OutputData = new() });

        var result = await _engine.ExecuteAsync(workflow, null, new ExecutionOptions { EnableParallelExecution = true });

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.NodeExecutions.Count);
    }

    [Fact]
    public async Task GetExecutionStatusAsync_ExistingExecution_ReturnsStatus()
    {
        var execution = new WorkflowExecution
        {
            Id = Guid.NewGuid(),
            WorkflowId = Guid.NewGuid(),
            Status = ExecutionStatus.Running,
            StartedAt = DateTime.UtcNow
        };

        await _dbContext.WorkflowExecutions.AddAsync(execution);
        await _dbContext.SaveChangesAsync();

        var status = await _engine.GetExecutionStatusAsync(execution.Id);

        Assert.Equal(ExecutionStatus.Running, status);
    }

    [Fact]
    public async Task GetExecutionStatusAsync_NonExistentExecution_ThrowsException()
    {
        await Assert.ThrowsAsync<WorkflowExecutionException>(
            () => _engine.GetExecutionStatusAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task CancelExecutionAsync_RunningExecution_CancelsSuccessfully()
    {
        var execution = new WorkflowExecution
        {
            Id = Guid.NewGuid(),
            WorkflowId = Guid.NewGuid(),
            Status = ExecutionStatus.Running,
            StartedAt = DateTime.UtcNow
        };

        await _dbContext.WorkflowExecutions.AddAsync(execution);
        await _dbContext.SaveChangesAsync();

        var result = await _engine.CancelExecutionAsync(execution.Id);

        Assert.Equal(ExecutionStatus.Cancelled, result.Status);

        var updatedExecution = await _dbContext.WorkflowExecutions.FindAsync(execution.Id);
        Assert.Equal(ExecutionStatus.Cancelled, updatedExecution!.Status);
        Assert.NotNull(updatedExecution.CompletedAt);
    }

    [Fact]
    public async Task CancelExecutionAsync_CompletedExecution_ThrowsException()
    {
        var execution = new WorkflowExecution
        {
            Id = Guid.NewGuid(),
            WorkflowId = Guid.NewGuid(),
            Status = ExecutionStatus.Completed,
            StartedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow
        };

        await _dbContext.WorkflowExecutions.AddAsync(execution);
        await _dbContext.SaveChangesAsync();

        await Assert.ThrowsAsync<WorkflowExecutionException>(
            () => _engine.CancelExecutionAsync(execution.Id));
    }

    private Workflow CreateTestWorkflow(List<WorkflowNodeDto> nodes, List<NodeConnectionDto> connections)
    {
        return new Workflow
        {
            Id = Guid.NewGuid(),
            Name = "Test Workflow",
            Description = "Test workflow for unit tests",
            NodesJson = JsonSerializer.Serialize(nodes),
            ConnectionsJson = JsonSerializer.Serialize(connections),
            Status = WorkflowStatus.Active,
            OrganizationId = Guid.NewGuid(),
            CreatedBy = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            LastModified = DateTime.UtcNow
        };
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }
}