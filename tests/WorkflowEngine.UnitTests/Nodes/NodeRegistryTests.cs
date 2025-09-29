using Microsoft.Extensions.DependencyInjection;
using WorkflowEngine.Nodes.Registry;

namespace WorkflowEngine.UnitTests.Nodes;

public class NodeRegistryTests
{
    private readonly NodeRegistry _registry;

    public NodeRegistryTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpClient();
        var serviceProvider = services.BuildServiceProvider();

        _registry = new NodeRegistry(serviceProvider);
    }

    [Fact]
    public void GetAllNodeDefinitions_ReturnsNodes()
    {
        var definitions = _registry.GetAllNodeDefinitions();

        Assert.NotEmpty(definitions);
        Assert.Contains(definitions, d => d.Type == "manual_trigger");
        Assert.Contains(definitions, d => d.Type == "http_request");
        Assert.Contains(definitions, d => d.Type == "set_variable");
        Assert.Contains(definitions, d => d.Type == "if_condition");
    }

    [Fact]
    public void GetNodeDefinition_ValidType_ReturnsDefinition()
    {
        var definition = _registry.GetNodeDefinition("manual_trigger");

        Assert.NotNull(definition);
        Assert.Equal("manual_trigger", definition.Type);
    }

    [Fact]
    public void GetNodeDefinition_InvalidType_ThrowsException()
    {
        Assert.Throws<ArgumentException>(() => _registry.GetNodeDefinition("invalid_type"));
    }

    [Fact]
    public void CreateNode_ValidType_ReturnsNode()
    {
        var node = _registry.CreateNode("manual_trigger");

        Assert.NotNull(node);
        Assert.Equal("manual_trigger", node.Type);
    }

    [Fact]
    public void IsRegistered_ValidType_ReturnsTrue()
    {
        Assert.True(_registry.IsRegistered("manual_trigger"));
    }

    [Fact]
    public void IsRegistered_InvalidType_ReturnsFalse()
    {
        Assert.False(_registry.IsRegistered("invalid_type"));
    }
}