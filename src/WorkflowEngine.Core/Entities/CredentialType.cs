namespace WorkflowEngine.Core.Entities;

public class CredentialType
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string AuthType { get; set; } = string.Empty;
    public string? IconUrl { get; set; }
    public string? Description { get; set; }
    public string? DocumentationUrl { get; set; }
    public string FieldsJson { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<UserCredential> UserCredentials { get; set; } = new List<UserCredential>();
}