using Microsoft.Extensions.DependencyInjection;
using WorkflowEngine.Nodes.Expressions;
using WorkflowEngine.Nodes.Interfaces;
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

        services.AddSingleton<IExpressionEvaluator, ExpressionEvaluator>();
        services.AddScoped<ICredentialService, FakeCredentialService>();
        services.AddScoped<ICredentialEncryptionService, FakeCredentialEncryptionService>();

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

public class FakeCredentialService : ICredentialService
{
    public Task<Dictionary<string, string>> GetCredentialDataAsync(int credentialId, Guid userId)
    {
        return Task.FromResult(new Dictionary<string, string>
        {
            ["username"] = "test",
            ["password"] = "test",
            ["token"] = "test-token",
            ["api_key"] = "test-key"
        });
    }

    public Task<T> GetCredentialDataAsync<T>(int credentialId, Guid userId) where T : class
    {
        var dict = new Dictionary<string, string>
        {
            ["username"] = "test",
            ["password"] = "test"
        };
        return Task.FromResult((T)(object)dict);
    }

    public Task<bool> ValidateCredentialAsync(int credentialId, Guid userId)
    {
        return Task.FromResult(true);
    }
}

public class FakeCredentialEncryptionService : ICredentialEncryptionService
{
    public Task<string> EncryptAsync(string data)
    {
        return Task.FromResult(Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(data)));
    }

    public Task<string> DecryptAsync(string encryptedData)
    {
        return Task.FromResult(System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(encryptedData)));
    }
}