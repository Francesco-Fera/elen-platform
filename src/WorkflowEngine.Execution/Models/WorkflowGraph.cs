namespace WorkflowEngine.Execution.Models;

public class WorkflowGraph
{
    private readonly Dictionary<string, WorkflowNode> _nodes = new();
    private readonly Dictionary<string, List<GraphEdge>> _adjacencyList = new();
    private readonly Dictionary<string, List<GraphEdge>> _reverseAdjacencyList = new();

    public IReadOnlyDictionary<string, WorkflowNode> Nodes => _nodes;
    public IReadOnlyDictionary<string, List<GraphEdge>> AdjacencyList => _adjacencyList;
    public IReadOnlyDictionary<string, List<GraphEdge>> ReverseAdjacencyList => _reverseAdjacencyList;

    public void AddNode(WorkflowNode node)
    {
        _nodes[node.Id] = node;
        if (!_adjacencyList.ContainsKey(node.Id))
            _adjacencyList[node.Id] = new();
        if (!_reverseAdjacencyList.ContainsKey(node.Id))
            _reverseAdjacencyList[node.Id] = new();
    }

    public void AddEdge(GraphEdge edge)
    {
        _adjacencyList[edge.SourceNodeId].Add(edge);
        _reverseAdjacencyList[edge.TargetNodeId].Add(edge);
    }

    public WorkflowNode GetNode(string nodeId)
    {
        if (!_nodes.TryGetValue(nodeId, out var node))
            throw new InvalidOperationException($"Node {nodeId} not found in graph");
        return node;
    }

    public List<GraphEdge> GetOutgoingEdges(string nodeId)
    {
        return _adjacencyList.TryGetValue(nodeId, out var edges) ? edges : new();
    }

    public List<GraphEdge> GetIncomingEdges(string nodeId)
    {
        return _reverseAdjacencyList.TryGetValue(nodeId, out var edges) ? edges : new();
    }

    public List<string> GetStartNodes()
    {
        return _nodes.Keys.Where(nodeId => GetIncomingEdges(nodeId).Count == 0).ToList();
    }

    public bool HasCycle()
    {
        var visited = new HashSet<string>();
        var recursionStack = new HashSet<string>();

        foreach (var nodeId in _nodes.Keys)
        {
            if (HasCycleDfs(nodeId, visited, recursionStack))
                return true;
        }

        return false;
    }

    private bool HasCycleDfs(string nodeId, HashSet<string> visited, HashSet<string> recursionStack)
    {
        if (recursionStack.Contains(nodeId))
            return true;

        if (visited.Contains(nodeId))
            return false;

        visited.Add(nodeId);
        recursionStack.Add(nodeId);

        foreach (var edge in GetOutgoingEdges(nodeId))
        {
            if (HasCycleDfs(edge.TargetNodeId, visited, recursionStack))
                return true;
        }

        recursionStack.Remove(nodeId);
        return false;
    }
}