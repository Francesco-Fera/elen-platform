using Microsoft.Extensions.Logging;
using Moq;
using WorkflowEngine.Application.DTOs.Workflow;
using WorkflowEngine.Execution.Exceptions;
using WorkflowEngine.Execution.Graph;

namespace WorkflowEngine.UnitTests.Execution;

public class TopologicalSorterTests
{
    private readonly TopologicalSorter _sorter;
    private readonly WorkflowGraphBuilder _builder;
    private readonly Mock<ILogger<TopologicalSorter>> _sorterLoggerMock;
    private readonly Mock<ILogger<WorkflowGraphBuilder>> _builderLoggerMock;

    public TopologicalSorterTests()
    {
        _sorterLoggerMock = new Mock<ILogger<TopologicalSorter>>();
        _builderLoggerMock = new Mock<ILogger<WorkflowGraphBuilder>>();
        _sorter = new TopologicalSorter(_sorterLoggerMock.Object);
        _builder = new WorkflowGraphBuilder(_builderLoggerMock.Object);
    }

    [Fact]
    public void Sort_LinearWorkflow_ReturnsCorrectOrder()
    {
        var nodes = new List<WorkflowNodeDto>
        {
            new() { Id = "node1", Type = "manual_trigger", Name = "Start", Parameters = new() },
            new() { Id = "node2", Type = "http_request", Name = "API", Parameters = new() },
            new() { Id = "node3", Type = "set_variable", Name = "Set", Parameters = new() }
        };

        var connections = new List<NodeConnectionDto>
        {
            new() { SourceNodeId = "node1", TargetNodeId = "node2" },
            new() { SourceNodeId = "node2", TargetNodeId = "node3" }
        };

        var graph = _builder.BuildGraph(nodes, connections);
        var sorted = _sorter.Sort(graph);

        Assert.Equal(3, sorted.Count);
        Assert.Equal("node1", sorted[0]);
        Assert.Equal("node2", sorted[1]);
        Assert.Equal("node3", sorted[2]);
    }

    [Fact]
    public void Sort_ParallelBranches_ReturnsValidOrder()
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
        var sorted = _sorter.Sort(graph);

        Assert.Equal(4, sorted.Count);
        Assert.Equal("node1", sorted[0]);
        Assert.Equal("node4", sorted[3]);
        Assert.Contains("node2", sorted);
        Assert.Contains("node3", sorted);
    }

    [Fact]
    public void Sort_EmptyGraph_ReturnsEmptyList()
    {
        var nodes = new List<WorkflowNodeDto>
        {
            new() { Id = "node1", Type = "manual_trigger", Name = "Start", Parameters = new() }
        };

        var graph = _builder.BuildGraph(nodes, new List<NodeConnectionDto>());
        var sorted = _sorter.Sort(graph);

        Assert.Single(sorted);
        Assert.Equal("node1", sorted[0]);
    }

    [Fact]
    public void Sort_CyclicGraph_ThrowsException()
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

        Assert.Throws<GraphValidationException>(() => _sorter.Sort(graph));
    }

    [Fact]
    public void GetParallelGroups_LinearWorkflow_ReturnsSequentialGroups()
    {
        var nodes = new List<WorkflowNodeDto>
        {
            new() { Id = "node1", Type = "manual_trigger", Name = "Start", Parameters = new() },
            new() { Id = "node2", Type = "http_request", Name = "API", Parameters = new() },
            new() { Id = "node3", Type = "set_variable", Name = "Set", Parameters = new() }
        };

        var connections = new List<NodeConnectionDto>
        {
            new() { SourceNodeId = "node1", TargetNodeId = "node2" },
            new() { SourceNodeId = "node2", TargetNodeId = "node3" }
        };

        var graph = _builder.BuildGraph(nodes, connections);
        var groups = _sorter.GetParallelGroups(graph);

        Assert.Equal(3, groups.Count);
        Assert.Single(groups[0]);
        Assert.Single(groups[1]);
        Assert.Single(groups[2]);
        Assert.Equal("node1", groups[0][0]);
        Assert.Equal("node2", groups[1][0]);
        Assert.Equal("node3", groups[2][0]);
    }

    [Fact]
    public void GetParallelGroups_ParallelBranches_ReturnsCorrectGroups()
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
        var groups = _sorter.GetParallelGroups(graph);

        Assert.Equal(3, groups.Count);
        Assert.Single(groups[0]);
        Assert.Equal(2, groups[1].Count);
        Assert.Single(groups[2]);
        Assert.Equal("node1", groups[0][0]);
        Assert.Contains("node2", groups[1]);
        Assert.Contains("node3", groups[1]);
        Assert.Equal("node4", groups[2][0]);
    }

    [Fact]
    public void GetParallelGroups_ComplexDAG_ReturnsCorrectLevels()
    {
        var nodes = new List<WorkflowNodeDto>
        {
            new() { Id = "node1", Type = "manual_trigger", Name = "Start", Parameters = new() },
            new() { Id = "node2", Type = "http_request", Name = "A", Parameters = new() },
            new() { Id = "node3", Type = "http_request", Name = "B", Parameters = new() },
            new() { Id = "node4", Type = "http_request", Name = "C", Parameters = new() },
            new() { Id = "node5", Type = "set_variable", Name = "D", Parameters = new() }
        };

        var connections = new List<NodeConnectionDto>
        {
            new() { SourceNodeId = "node1", TargetNodeId = "node2" },
            new() { SourceNodeId = "node1", TargetNodeId = "node3" },
            new() { SourceNodeId = "node2", TargetNodeId = "node4" },
            new() { SourceNodeId = "node3", TargetNodeId = "node4" },
            new() { SourceNodeId = "node4", TargetNodeId = "node5" }
        };

        var graph = _builder.BuildGraph(nodes, connections);
        var groups = _sorter.GetParallelGroups(graph);

        Assert.Equal(4, groups.Count);
        Assert.Single(groups[0]);
        Assert.Equal(2, groups[1].Count);
        Assert.Single(groups[2]);
        Assert.Single(groups[3]);
    }

    [Fact]
    public void GetExecutableNodes_AllDependenciesMet_ReturnsNode()
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
        var completed = new HashSet<string> { "node1" };
        var context = new Dictionary<string, object>();

        var executable = _sorter.GetExecutableNodes(graph, completed, context);

        Assert.Single(executable);
        Assert.Equal("node2", executable[0]);
    }

    [Fact]
    public void GetExecutableNodes_DependenciesNotMet_ReturnsEmpty()
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
        var completed = new HashSet<string>();
        var context = new Dictionary<string, object>();

        var executable = _sorter.GetExecutableNodes(graph, completed, context);

        Assert.Single(executable);
        Assert.Equal("node1", executable[0]);
    }

    [Fact]
    public void GetExecutableNodes_ConditionalBranch_RespectsCondition()
    {
        var nodes = new List<WorkflowNodeDto>
        {
            new() { Id = "node1", Type = "manual_trigger", Name = "Start", Parameters = new() },
            new() { Id = "node2", Type = "if_condition", Name = "Check", Parameters = new() },
            new() { Id = "node3", Type = "http_request", Name = "True", Parameters = new() },
            new() { Id = "node4", Type = "set_variable", Name = "False", Parameters = new() }
        };

        var connections = new List<NodeConnectionDto>
        {
            new() { SourceNodeId = "node1", TargetNodeId = "node2" },
            new() { SourceNodeId = "node2", TargetNodeId = "node3", SourceOutput = "true" },
            new() { SourceNodeId = "node2", TargetNodeId = "node4", SourceOutput = "false" }
        };

        var graph = _builder.BuildGraph(nodes, connections);
        var completed = new HashSet<string> { "node1", "node2" };
        var context = new Dictionary<string, object>
        {
            ["$node.node2"] = new Dictionary<string, object>
            {
                ["conditionalOutput"] = "true"
            }
        };

        var executable = _sorter.GetExecutableNodes(graph, completed, context);

        Assert.Single(executable);
        Assert.Equal("node3", executable[0]);
    }

    [Fact]
    public void CalculateNodeLevels_LinearWorkflow_ReturnsIncrementalLevels()
    {
        var nodes = new List<WorkflowNodeDto>
        {
            new() { Id = "node1", Type = "manual_trigger", Name = "Start", Parameters = new() },
            new() { Id = "node2", Type = "http_request", Name = "API", Parameters = new() },
            new() { Id = "node3", Type = "set_variable", Name = "Set", Parameters = new() }
        };

        var connections = new List<NodeConnectionDto>
        {
            new() { SourceNodeId = "node1", TargetNodeId = "node2" },
            new() { SourceNodeId = "node2", TargetNodeId = "node3" }
        };

        var graph = _builder.BuildGraph(nodes, connections);
        var levels = _sorter.CalculateNodeLevels(graph);

        Assert.Equal(0, levels["node1"]);
        Assert.Equal(1, levels["node2"]);
        Assert.Equal(2, levels["node3"]);
    }

    [Fact]
    public void CalculateNodeLevels_ParallelBranches_ReturnsSameLevelForParallel()
    {
        var nodes = new List<WorkflowNodeDto>
        {
            new() { Id = "node1", Type = "manual_trigger", Name = "Start", Parameters = new() },
            new() { Id = "node2", Type = "http_request", Name = "API1", Parameters = new() },
            new() { Id = "node3", Type = "http_request", Name = "API2", Parameters = new() }
        };

        var connections = new List<NodeConnectionDto>
        {
            new() { SourceNodeId = "node1", TargetNodeId = "node2" },
            new() { SourceNodeId = "node1", TargetNodeId = "node3" }
        };

        var graph = _builder.BuildGraph(nodes, connections);
        var levels = _sorter.CalculateNodeLevels(graph);

        Assert.Equal(0, levels["node1"]);
        Assert.Equal(1, levels["node2"]);
        Assert.Equal(1, levels["node3"]);
    }
}