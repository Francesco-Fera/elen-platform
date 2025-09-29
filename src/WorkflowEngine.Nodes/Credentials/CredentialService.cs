using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using WorkflowEngine.Infrastructure.Data;
using WorkflowEngine.Nodes.Interfaces;

namespace WorkflowEngine.Nodes.Credentials;

public class CredentialService : ICredentialService
{
    private readonly WorkflowEngineDbContext _context;
    private readonly ICredentialEncryptionService _encryptionService;

    public CredentialService(
        WorkflowEngineDbContext context,
        ICredentialEncryptionService encryptionService)
    {
        _context = context;
        _encryptionService = encryptionService;
    }

    public async Task<Dictionary<string, string>> GetCredentialDataAsync(int credentialId, Guid userId)
    {
        var credential = await _context.UserCredentials
            .FirstOrDefaultAsync(c => c.Id == credentialId && c.UserId == userId)
            ?? throw new UnauthorizedAccessException("Credential not found or access denied");

        var decrypted = await _encryptionService.DecryptAsync(credential.EncryptedData);
        return JsonSerializer.Deserialize<Dictionary<string, string>>(decrypted)
            ?? new Dictionary<string, string>();
    }

    public async Task<T> GetCredentialDataAsync<T>(int credentialId, Guid userId) where T : class
    {
        var data = await GetCredentialDataAsync(credentialId, userId);
        var json = JsonSerializer.Serialize(data);
        return JsonSerializer.Deserialize<T>(json)
            ?? throw new InvalidOperationException("Failed to deserialize credential data");
    }

    public async Task<bool> ValidateCredentialAsync(int credentialId, Guid userId)
    {
        return await _context.UserCredentials
            .AnyAsync(c => c.Id == credentialId && c.UserId == userId && c.IsActive);
    }
}