using System.ComponentModel.DataAnnotations;

namespace WorkflowEngine.Core.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [EmailAddress]
    [MaxLength(320)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? FirstName { get; set; }

    [MaxLength(100)]
    public string? LastName { get; set; }

    [MaxLength(50)]
    public string? TimeZone { get; set; } = "UTC";

    public bool IsActive { get; set; } = true;
    public bool IsEmailVerified { get; set; } = false;
    public string? EmailVerificationToken { get; set; }

    // Password reset
    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }

    // Multi-tenant properties
    public Guid? CurrentOrganizationId { get; set; }

    // Navigation properties
    public virtual Organization? CurrentOrganization { get; set; }
    public virtual ICollection<OrganizationMember> Organizations { get; set; } = new List<OrganizationMember>();
    public virtual ICollection<Workflow> CreatedWorkflows { get; set; } = new List<Workflow>();
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public virtual ICollection<OrganizationInvite> SentInvites { get; set; } = new List<OrganizationInvite>();
    public List<EmailVerificationToken> EmailVerificationTokens { get; set; } = new();
    public List<PasswordResetToken> PasswordResetTokens { get; set; } = new();
}