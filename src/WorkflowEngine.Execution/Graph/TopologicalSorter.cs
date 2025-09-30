using Microsoft.Extensions.Logging;
using WorkflowEngine.Execution.Exceptions;
using WorkflowEngine.Execution.Interfaces;
using WorkflowEngine.Execution.Models;

namespace WorkflowEngine.Execution.Graph;

public class TopologicalSorter : ITopologicalSorter
{
    private readonly ILogger<TopologicalSorter> _logger;

    public TopologicalSorter(ILogger<TopologicalSorter> logger)
    {
        _logger = logger;
    }

    public List<string> Sort(WorkflowGraph graph)
    {
        if (graph.Nodes.Count == 0)
            return new List<string>();

        if (graph.HasCycle())
            throw new GraphValidationException("Cannot sort graph with cycles");

        var inDegree = new Dictionary<string, int>();
        foreach (var nodeId in graph.Nodes.Keys)
        {
            inDegree[nodeId] = graph.GetIncomingEdges(nodeId).Count;
        }

        var queue = new Queue<string>();
        foreach (var nodeId in graph.Nodes.Keys)
        {
            if (inDegree[nodeId] == 0)
                queue.Enqueue(nodeId);
        }

        var sorted = new List<string>();

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            sorted.Add(current);

            foreach (var edge in graph.GetOutgoingEdges(current))
            {
                inDegree[edge.TargetNodeId]--;
                if (inDegree[edge.TargetNodeId] == 0)
                    queue.Enqueue(edge.TargetNodeId);
            }
        }

        if (sorted.Count != graph.Nodes.Count)
            throw new GraphValidationException("Topological sort failed - possible cycle detected");

        _logger.LogInformation("Topological sort completed: {Order}", string.Join(" -> ", sorted));
        return sorted;
    }

    public List<List<string>> GetParallelGroups(WorkflowGraph graph)
    {
        if (graph.Nodes.Count == 0)
            return new List<List<string>>();

        if (graph.HasCycle())
            throw new GraphValidationException("Cannot determine parallel groups for graph with cycles");

        var inDegree = new Dictionary<string, int>();
        var levels = new Dictionary<string, int>();

        foreach (var nodeId in graph.Nodes.Keys)
        {
            inDegree[nodeId] = graph.GetIncomingEdges(nodeId).Count;
            levels[nodeId] = 0;
        }

        var queue = new Queue<string>();
        foreach (var nodeId in graph.Nodes.Keys)
        {
            if (inDegree[nodeId] == 0)
            {
                queue.Enqueue(nodeId);
                levels[nodeId] = 0;
            }
        }

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            var currentLevel = levels[current];

            foreach (var edge in graph.GetOutgoingEdges(current))
            {
                var targetId = edge.TargetNodeId;
                levels[targetId] = Math.Max(levels[targetId], currentLevel + 1);

                inDegree[targetId]--;
                if (inDegree[targetId] == 0)
                    queue.Enqueue(targetId);
            }
        }

        var maxLevel = levels.Values.Max();
        var groups = new List<List<string>>();

        for (int level = 0; level <= maxLevel; level++)
        {
            var nodesAtLevel = levels
                .Where(kvp => kvp.Value == level)
                .Select(kvp => kvp.Key)
                .ToList();

            if (nodesAtLevel.Count > 0)
                groups.Add(nodesAtLevel);
        }

        _logger.LogInformation("Identified {GroupCount} parallel groups with max parallelism of {MaxParallel}",
            groups.Count, groups.Max(g => g.Count));

        return groups;
    }

    public List<string> GetExecutableNodes(
        WorkflowGraph graph,
        HashSet<string> completedNodes,
        Dictionary<string, object> workflowContext)
    {
        var executable = new List<string>();

        foreach (var nodeId in graph.Nodes.Keys)
        {
            if (completedNodes.Contains(nodeId))
                continue;

            if (CanExecuteNode(nodeId, graph, completedNodes, workflowContext))
                executable.Add(nodeId);
        }

        return executable;
    }

    private bool CanExecuteNode(
        string nodeId,
        WorkflowGraph graph,
        HashSet<string> completedNodes,
        Dictionary<string, object> workflowContext)
    {
        var incomingEdges = graph.GetIncomingEdges(nodeId);

        if (incomingEdges.Count == 0)
            return true;

        var requiredPredecessors = new HashSet<string>();

        foreach (var edge in incomingEdges)
        {
            var sourceNodeId = edge.SourceNodeId;

            if (!completedNodes.Contains(sourceNodeId))
                return false;

            if (edge.SourceOutput != "default")
            {
                var contextKey = $"$node.{sourceNodeId}";
                if (workflowContext.TryGetValue(contextKey, out var nodeData))
                {
                    if (nodeData is Dictionary<string, object> dataDict)
                    {
                        if (dataDict.TryGetValue("conditionalOutput", out var output))
                        {
                            if (output?.ToString() != edge.SourceOutput)
                                return false;
                        }
                    }
                }
            }

            requiredPredecessors.Add(sourceNodeId);
        }

        return requiredPredecessors.All(p => completedNodes.Contains(p));
    }

    public Dictionary<string, int> CalculateNodeLevels(WorkflowGraph graph)
    {
        var levels = new Dictionary<string, int>();
        var inDegree = new Dictionary<string, int>();

        foreach (var nodeId in graph.Nodes.Keys)
        {
            inDegree[nodeId] = graph.GetIncomingEdges(nodeId).Count;
            levels[nodeId] = 0;
        }

        var queue = new Queue<string>();
        foreach (var nodeId in graph.Nodes.Keys)
        {
            if (inDegree[nodeId] == 0)
            {
                queue.Enqueue(nodeId);
                levels[nodeId] = 0;
            }
        }

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            var currentLevel = levels[current];

            foreach (var edge in graph.GetOutgoingEdges(current))
            {
                var targetId = edge.TargetNodeId;
                levels[targetId] = Math.Max(levels[targetId], currentLevel + 1);

                inDegree[targetId]--;
                if (inDegree[targetId] == 0)
                    queue.Enqueue(targetId);
            }
        }

        return levels;
    }
}