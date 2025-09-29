using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Reflection;
using WorkflowEngine.Nodes.Attributes;
using WorkflowEngine.Nodes.Interfaces;
using WorkflowEngine.Nodes.Models;

namespace WorkflowEngine.Nodes.Registry;

public class NodeRegistry : INodeRegistry
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ConcurrentDictionary<string, Type> _nodeTypes = new();
    private readonly ConcurrentDictionary<string, NodeDefinition> _nodeDefinitions = new();

    public NodeRegistry(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        RegisterBuiltInNodes();
    }

    private void RegisterBuiltInNodes()
    {
        var nodeTypes = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => typeof(INode).IsAssignableFrom(t) &&
                       !t.IsAbstract &&
                       !t.IsInterface &&
                       t.GetCustomAttribute<NodeTypeAttribute>() != null);

        foreach (var nodeType in nodeTypes)
        {
            RegisterNodeType(nodeType);
        }
    }

    private void RegisterNodeType(Type nodeType)
    {
        var nodeInstance = (INode)ActivatorUtilities.CreateInstance(_serviceProvider, nodeType);
        _nodeTypes[nodeInstance.Type] = nodeType;
        _nodeDefinitions[nodeInstance.Type] = nodeInstance.GetDefinition();
    }

    public INode CreateNode(string nodeType)
    {
        if (!_nodeTypes.TryGetValue(nodeType, out var type))
            throw new ArgumentException($"Node type '{nodeType}' not registered");

        return (INode)ActivatorUtilities.CreateInstance(_serviceProvider, type);
    }

    public NodeDefinition GetNodeDefinition(string nodeType)
    {
        if (!_nodeDefinitions.TryGetValue(nodeType, out var definition))
            throw new ArgumentException($"Node type '{nodeType}' not registered");

        return definition;
    }

    public IEnumerable<NodeDefinition> GetAllNodeDefinitions()
    {
        return _nodeDefinitions.Values
            .OrderBy(d => d.Category)
            .ThenBy(d => d.Name);
    }

    public void RegisterNode<T>() where T : class, INode
    {
        RegisterNodeType(typeof(T));
    }

    public bool IsRegistered(string nodeType)
    {
        return _nodeTypes.ContainsKey(nodeType);
    }
}