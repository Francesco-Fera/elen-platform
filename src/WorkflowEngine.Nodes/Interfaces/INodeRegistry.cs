using WorkflowEngine.Nodes.Models;

namespace WorkflowEngine.Nodes.Interfaces;

public interface INodeRegistry
{
    NodeDefinition GetNodeDefinition(string nodeType);
    IEnumerable<NodeDefinition> GetAllNodeDefinitions();
    INode CreateNode(string nodeType);
    void RegisterNode<T>() where T : class, INode;
    bool IsRegistered(string nodeType);
}