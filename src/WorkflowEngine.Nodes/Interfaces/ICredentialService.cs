namespace WorkflowEngine.Nodes.Interfaces;

public interface ICredentialService
{
    Task<T> GetCredentialDataAsync<T>(int credentialId, Guid userId) where T : class;
    Task<Dictionary<string, string>> GetCredentialDataAsync(int credentialId, Guid userId);
    Task<bool> ValidateCredentialAsync(int credentialId, Guid userId);
}