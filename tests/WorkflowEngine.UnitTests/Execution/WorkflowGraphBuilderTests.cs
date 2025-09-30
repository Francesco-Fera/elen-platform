using Microsoft.Extensions.Logging;
using Moq;
using WorkflowEngine.Application.DTOs.Workflow;
using WorkflowEngine.Execution.Exceptions;
using WorkflowEngine.Execution.Graph;

namespace WorkflowEngine.UnitTests.Execution;

public class WorkflowGraphBuilderTests
{
    private readonly WorkflowGraphBuilder _builder;
    private readonly Mock<ILogger<WorkflowGraphBuilder>> _loggerMock;

    public WorkflowGraphBuilderTests()
    {
        _loggerMock = new Mock<ILogger<WorkflowGraphBuilder>>();
        _builder = new WorkflowGraphBuilder(_loggerMock.Object);
    }

    [Fact]
    public void BuildGraph_SimpleLinearWorkflow_Success()
    {
        var nodes = new List<WorkflowNodeDto>
        {
            new() { Id = "node1", Type = "manual_trigger", Name = "Start", Parameters = new() },
            new() { Id = "node2", Type = "http_request", Name = "API Call", Parameters = new() },
            new() { Id = "node3", Type = "set_variable", Name = "Set Result", Parameters = new() }
        };

        var connections = new List<NodeConnectionDto>
        {
            new() { SourceNodeId = "node1", TargetNodeId = "node2" },
            new() { SourceNodeId = "node2", TargetNodeId = "node3" }
        };

        var graph = _builder.BuildGraph(nodes, connections);

        Assert.Equal(3, graph.Nodes.Count);
        Assert.Equal(2, graph.GetOutgoingEdges("node1").Count + graph.GetOutgoingEdges("node2").Count);
        Assert.Single(graph.GetStartNodes());
        Assert.Equal("node1", graph.GetStartNodes()[0]);
    }

    [Fact]
    public void BuildGraph_EmptyNodes_ThrowsException()
    {
        var nodes = new List<WorkflowNodeDto>();
        var connections = new List<NodeConnectionDto>();

        var exception = Assert.Throws<GraphValidationException>(() => _builder.BuildGraph(nodes, connections));
        Assert.Contains("at least one node", exception.Message);
    }

    [Fact]
    public void BuildGraph_NullNodes_ThrowsException()
    {
        var exception = Assert.Throws<GraphValidationException>(() => _builder.BuildGraph(null!, null!));
        Assert.Contains("at least one node", exception.Message);
    }

    [Fact]
    public void BuildGraph_ConnectionToNonExistentNode_ThrowsException()
    {
        var nodes = new List<WorkflowNodeDto>
        {
            new() { Id = "node1", Type = "manual_trigger", Name = "Start", Parameters = new() }
        };

        var connections = new List<NodeConnectionDto>
        {
            new() { SourceNodeId = "node1", TargetNodeId = "node_nonexistent" }
        };

        var exception = Assert.Throws<GraphValidationException>(() => _builder.BuildGraph(nodes, connections));
        Assert.Contains("non-existent target node", exception.Message);
    }

    [Fact]
    public void BuildGraph_ConditionalBranching_Success()
    {
        var nodes = new List<WorkflowNodeDto>
        {
            new() { Id = "node1", Type = "manual_trigger", Name = "Start", Parameters = new() },
            new() { Id = "node2", Type = "if_condition", Name = "Check", Parameters = new() },
            new() { Id = "node3", Type = "http_request", Name = "True Branch", Parameters = new() },
            new() { Id = "node4", Type = "set_variable", Name = "False Branch", Parameters = new() }
        };

        var connections = new List<NodeConnectionDto>
        {
            new() { SourceNodeId = "node1", TargetNodeId = "node2" },
            new() { SourceNodeId = "node2", TargetNodeId = "node3", SourceOutput = "true" },
            new() { SourceNodeId = "node2", TargetNodeId = "node4", SourceOutput = "false" }
        };

        var graph = _builder.BuildGraph(nodes, connections);

        Assert.Equal(4, graph.Nodes.Count);
        var node2Edges = graph.GetOutgoingEdges("node2");
        Assert.Equal(2, node2Edges.Count);
        Assert.Contains(node2Edges, e => e.SourceOutput == "true");
        Assert.Contains(node2Edges, e => e.SourceOutput == "false");
    }

    [Fact]
    public async Task ValidateGraphAsync_ValidLinearGraph_Success()
    {
        var nodes = new List<WorkflowNodeDto>
        {
            new() { Id = "node1", Type = "manual_trigger", Name = "Start", Parameters = new() },
            new() { Id = "node2", Type = "http_request", Name = "API", Parameters = new() }
        };

        var connections = new List<NodeConnectionDto>
        {
            new() { SourceNodeId = "node1", TargetNodeId = "node2" }
        };

        var graph = _builder.BuildGraph(nodes, connections);
        var result = await _builder.ValidateGraphAsync(graph);

        Assert.True(result);
    }

    [Fact]
    public async Task ValidateGraphAsync_CyclicGraph_ThrowsException()
    {
        var nodes = new List<WorkflowNodeDto>
        {
            new() { Id = "node1", Type = "manual_trigger", Name = "Start", Parameters = new() },
            new() { Id = "node2", Type = "http_request", Name = "API", Parameters = new() }
        };

        var connections = new List<NodeConnectionDto>
        {
            new() { SourceNodeId = "node1", TargetNodeId = "node2" },
            new() { SourceNodeId = "node2", TargetNodeId = "node1" }
        };

        var graph = _builder.BuildGraph(nodes, connections);

        var exception = await Assert.ThrowsAsync<GraphValidationException>(() => _builder.ValidateGraphAsync(graph));

        Assert.NotEmpty(exception.ValidationErrors);
        Assert.Contains(exception.ValidationErrors, e => e.Contains("cycle", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ValidateGraphAsync_NoStartNodes_ThrowsException()
    {
        var nodes = new List<WorkflowNodeDto>
        {
            new() { Id = "node1", Type = "http_request", Name = "API1", Parameters = new() },
            new() { Id = "node2", Type = "http_request", Name = "API2", Parameters = new() }
        };

        var connections = new List<NodeConnectionDto>
        {
            new() { SourceNodeId = "node1", TargetNodeId = "node2" },
            new() { SourceNodeId = "node2", TargetNodeId = "node1" }
        };

        var graph = _builder.BuildGraph(nodes, connections);

        var exception = await Assert.ThrowsAsync<GraphValidationException>(() => _builder.ValidateGraphAsync(graph));

        Assert.NotEmpty(exception.ValidationErrors);
        Assert.Contains(exception.ValidationErrors, e => e.Contains("no start nodes", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ValidateGraphAsync_UnreachableNodes_ThrowsException()
    {
        var nodes = new List<WorkflowNodeDto>
        {
            new() { Id = "node1", Type = "manual_trigger", Name = "Start", Parameters = new() },
            new() { Id = "node2", Type = "http_request", Name = "API", Parameters = new() },
            new() { Id = "node3", Type = "set_variable", Name = "Orphan", Parameters = new() }
        };

        var connections = new List<NodeConnectionDto>
        {
            new() { SourceNodeId = "node1", TargetNodeId = "node2" }
        };

        var graph = _builder.BuildGraph(nodes, connections);

        var exception = await Assert.ThrowsAsync<GraphValidationException>(() => _builder.ValidateGraphAsync(graph));

        Assert.NotEmpty(exception.ValidationErrors);
        var hasRelevantError = exception.ValidationErrors.Any(e =>
            e.Contains("multiple start", StringComparison.OrdinalIgnoreCase) ||
            e.Contains("unreachable", StringComparison.OrdinalIgnoreCase));
        Assert.True(hasRelevantError);
    }

    [Fact]
    public void BuildGraph_ParallelBranches_Success()
    {
        var nodes = new List<WorkflowNodeDto>
        {
            new() { Id = "node1", Type = "manual_trigger", Name = "Start", Parameters = new() },
            new() { Id = "node2", Type = "http_request", Name = "API1", Parameters = new() },
            new() { Id = "node3", Type = "http_request", Name = "API2", Parameters = new() },
            new() { Id = "node4", Type = "set_variable", Name = "Merge", Parameters = new() }
        };

        var connections = new List<NodeConnectionDto>
        {
            new() { SourceNodeId = "node1", TargetNodeId = "node2" },
            new() { SourceNodeId = "node1", TargetNodeId = "node3" },
            new() { SourceNodeId = "node2", TargetNodeId = "node4" },
            new() { SourceNodeId = "node3", TargetNodeId = "node4" }
        };

        var graph = _builder.BuildGraph(nodes, connections);

        Assert.Equal(4, graph.Nodes.Count);
        Assert.Equal(2, graph.GetOutgoingEdges("node1").Count);
        Assert.Equal(2, graph.GetIncomingEdges("node4").Count);
    }

    [Fact]
    public void BuildGraph_CustomOutputNames_Success()
    {
        var nodes = new List<WorkflowNodeDto>
        {
            new() { Id = "node1", Type = "manual_trigger", Name = "Start", Parameters = new() },
            new() { Id = "node2", Type = "if_condition", Name = "Check", Parameters = new() },
            new() { Id = "node3", Type = "http_request", Name = "API", Parameters = new() }
        };

        var connections = new List<NodeConnectionDto>
        {
            new() { SourceNodeId = "node1", TargetNodeId = "node2", SourceOutput = "output1", TargetInput = "input1" },
            new() { SourceNodeId = "node2", TargetNodeId = "node3", SourceOutput = "true", TargetInput = "default" }
        };

        var graph = _builder.BuildGraph(nodes, connections);

        var edge1 = graph.GetOutgoingEdges("node1").First();
        Assert.Equal("output1", edge1.SourceOutput);
        Assert.Equal("input1", edge1.TargetInput);

        var edge2 = graph.GetOutgoingEdges("node2").First();
        Assert.Equal("true", edge2.SourceOutput);
    }
}