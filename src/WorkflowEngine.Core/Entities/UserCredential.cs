namespace WorkflowEngine.Core.Entities;

public class UserCredential
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public int CredentialTypeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string EncryptedData { get; set; } = string.Empty;
    public string EncryptionKeyId { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastUsedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? LastValidatedAt { get; set; }
    public string ValidationStatus { get; set; } = "unknown";

    public User User { get; set; } = null!;
    public CredentialType CredentialType { get; set; } = null!;
}