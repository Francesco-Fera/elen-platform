namespace WorkflowEngine.Nodes.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class NodeTypeAttribute : Attribute
{
    public string Type { get; }
    public NodeTypeAttribute(string type) => Type = type;
}