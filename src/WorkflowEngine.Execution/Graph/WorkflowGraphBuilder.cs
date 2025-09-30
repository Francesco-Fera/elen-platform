using Microsoft.Extensions.Logging;
using WorkflowEngine.Application.DTOs.Workflow;
using WorkflowEngine.Execution.Exceptions;
using WorkflowEngine.Execution.Interfaces;
using WorkflowEngine.Execution.Models;

namespace WorkflowEngine.Execution.Graph;

public class WorkflowGraphBuilder : IWorkflowGraphBuilder
{
    private readonly ILogger<WorkflowGraphBuilder> _logger;

    public WorkflowGraphBuilder(ILogger<WorkflowGraphBuilder> logger)
    {
        _logger = logger;
    }

    public WorkflowGraph BuildGraph(List<WorkflowNodeDto> nodes, List<NodeConnectionDto> connections)
    {
        var graph = new WorkflowGraph();

        if (nodes == null || nodes.Count == 0)
            throw new GraphValidationException("Workflow must contain at least one node");

        foreach (var nodeDto in nodes)
        {
            var node = new WorkflowNode
            {
                Id = nodeDto.Id,
                Type = nodeDto.Type,
                Name = nodeDto.Name,
                Parameters = nodeDto.Parameters ?? new(),
                Configuration = nodeDto.Configuration ?? new()
            };

            graph.AddNode(node);
        }

        if (connections != null)
        {
            foreach (var connDto in connections)
            {
                if (!graph.Nodes.ContainsKey(connDto.SourceNodeId))
                    throw new GraphValidationException($"Connection references non-existent source node: {connDto.SourceNodeId}");

                if (!graph.Nodes.ContainsKey(connDto.TargetNodeId))
                    throw new GraphValidationException($"Connection references non-existent target node: {connDto.TargetNodeId}");

                var edge = new GraphEdge
                {
                    SourceNodeId = connDto.SourceNodeId,
                    TargetNodeId = connDto.TargetNodeId,
                    SourceOutput = connDto.SourceOutput ?? "default",
                    TargetInput = connDto.TargetInput ?? "default"
                };

                graph.AddEdge(edge);
            }
        }

        _logger.LogInformation("Built workflow graph with {NodeCount} nodes and {EdgeCount} edges",
            nodes.Count, connections?.Count ?? 0);

        return graph;
    }

    public async Task<bool> ValidateGraphAsync(WorkflowGraph graph, CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();

        if (graph.Nodes.Count == 0)
        {
            errors.Add("Graph contains no nodes");
        }

        if (graph.HasCycle())
        {
            errors.Add("Graph contains cycles - circular dependencies detected");
        }

        var startNodes = graph.GetStartNodes();
        if (startNodes.Count == 0)
        {
            errors.Add("Graph has no start nodes - all nodes have incoming connections");
        }
        else if (startNodes.Count > 1)
        {
            errors.Add($"Graph has multiple start nodes ({string.Join(", ", startNodes)}) - workflows must have exactly one entry point");
        }

        if (startNodes.Count > 0)
        {
            var reachableNodes = new HashSet<string>();
            await DfsAsync(startNodes[0], graph, reachableNodes, cancellationToken);

            var unreachableNodes = graph.Nodes.Keys.Except(reachableNodes).ToList();
            if (unreachableNodes.Count > 0)
            {
                errors.Add($"Graph contains unreachable nodes: {string.Join(", ", unreachableNodes)}");
            }
        }

        foreach (var nodeId in graph.Nodes.Keys)
        {
            var outgoingEdges = graph.GetOutgoingEdges(nodeId);
            var groupedByTarget = outgoingEdges.GroupBy(e => e.TargetNodeId);

            foreach (var group in groupedByTarget)
            {
                if (group.Count() > 1)
                {
                    var duplicateInputs = group.GroupBy(e => e.TargetInput).Where(g => g.Count() > 1);
                    if (duplicateInputs.Any())
                    {
                        errors.Add($"Node {nodeId} has multiple connections to the same input of node {group.Key}");
                    }
                }
            }
        }

        if (errors.Count > 0)
        {
            _logger.LogWarning("Graph validation failed with {ErrorCount} errors", errors.Count);
            throw new GraphValidationException("Graph validation failed", errors);
        }

        _logger.LogInformation("Graph validation successful");
        return true;
    }

    private async Task<HashSet<string>> GetReachableNodesAsync(WorkflowGraph graph, CancellationToken cancellationToken)
    {
        var reachable = new HashSet<string>();
        var startNodes = graph.GetStartNodes();

        foreach (var startNode in startNodes)
        {
            await DfsAsync(startNode, graph, reachable, cancellationToken);
        }

        return reachable;
    }

    private async Task DfsAsync(string nodeId, WorkflowGraph graph, HashSet<string> visited, CancellationToken cancellationToken)
    {
        if (visited.Contains(nodeId))
            return;

        visited.Add(nodeId);

        var outgoingEdges = graph.GetOutgoingEdges(nodeId);
        foreach (var edge in outgoingEdges)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await DfsAsync(edge.TargetNodeId, graph, visited, cancellationToken);
        }
    }
}