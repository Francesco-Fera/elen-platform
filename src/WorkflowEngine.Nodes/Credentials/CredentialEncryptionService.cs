using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using WorkflowEngine.Nodes.Interfaces;

namespace WorkflowEngine.Nodes.Credentials;

public class CredentialEncryptionService : ICredentialEncryptionService
{
    private readonly byte[] _key;

    public CredentialEncryptionService(IConfiguration configuration)
    {
        var keyString = configuration["Credentials:EncryptionKey"]
            ?? throw new InvalidOperationException("Encryption key not configured");
        _key = Convert.FromBase64String(keyString);
    }

    public async Task<string> EncryptAsync(string data)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        var dataBytes = Encoding.UTF8.GetBytes(data);
        var encryptedBytes = await Task.Run(() =>
            encryptor.TransformFinalBlock(dataBytes, 0, dataBytes.Length));

        var result = new EncryptedData
        {
            IV = aes.IV,
            Data = encryptedBytes
        };

        return Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(result));
    }

    public async Task<string> DecryptAsync(string encryptedData)
    {
        var data = JsonSerializer.Deserialize<EncryptedData>(
            Convert.FromBase64String(encryptedData))
            ?? throw new InvalidOperationException("Invalid encrypted data");

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = data.IV;

        using var decryptor = aes.CreateDecryptor();
        var decryptedBytes = await Task.Run(() =>
            decryptor.TransformFinalBlock(data.Data, 0, data.Data.Length));

        return Encoding.UTF8.GetString(decryptedBytes);
    }

    private class EncryptedData
    {
        public byte[] IV { get; set; } = Array.Empty<byte>();
        public byte[] Data { get; set; } = Array.Empty<byte>();
    }
}