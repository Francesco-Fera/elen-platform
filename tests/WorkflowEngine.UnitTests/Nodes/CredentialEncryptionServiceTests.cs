using Microsoft.Extensions.Configuration;
using WorkflowEngine.Nodes.Credentials;

namespace WorkflowEngine.UnitTests.Nodes;

public class CredentialEncryptionServiceTests
{
    private readonly CredentialEncryptionService _service;

    public CredentialEncryptionServiceTests()
    {
        var key = new byte[32];
        new Random().NextBytes(key);
        var base64Key = Convert.ToBase64String(key);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Credentials:EncryptionKey"] = base64Key
            })
            .Build();

        _service = new CredentialEncryptionService(configuration);
    }

    [Fact]
    public async Task EncryptAsync_DecryptAsync_RoundTrip_Success()
    {
        var original = "sensitive data";

        var encrypted = await _service.EncryptAsync(original);
        var decrypted = await _service.DecryptAsync(encrypted);

        Assert.Equal(original, decrypted);
    }

    [Fact]
    public async Task EncryptAsync_DifferentResults_ForSameInput()
    {
        var data = "test data";

        var encrypted1 = await _service.EncryptAsync(data);
        var encrypted2 = await _service.EncryptAsync(data);

        Assert.NotEqual(encrypted1, encrypted2);
    }

    [Fact]
    public async Task EncryptAsync_EmptyString_Success()
    {
        var encrypted = await _service.EncryptAsync("");
        var decrypted = await _service.DecryptAsync(encrypted);

        Assert.Equal("", decrypted);
    }

    [Fact]
    public async Task DecryptAsync_InvalidData_ThrowsException()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _service.DecryptAsync("invalid-base64"));
    }
}