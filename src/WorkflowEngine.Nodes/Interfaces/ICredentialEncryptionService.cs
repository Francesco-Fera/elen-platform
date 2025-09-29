namespace WorkflowEngine.Nodes.Interfaces;

public interface ICredentialEncryptionService
{
    Task<string> EncryptAsync(string data);
    Task<string> DecryptAsync(string encryptedData);
}