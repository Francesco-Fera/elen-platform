using System.ComponentModel.DataAnnotations;
using WorkflowEngine.Core.Enums;

namespace WorkflowEngine.Core.Entities;

public class Organization
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Slug { get; set; } // URL-friendly identifier

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(50)]
    public string? Domain { get; set; } // For domain-based tenant identification

    public bool IsActive { get; set; } = true;
    public bool IsTrialAccount { get; set; } = true;
    public DateTime? TrialExpiresAt { get; set; }

    // Subscription info
    public SubscriptionPlan Plan { get; set; } = SubscriptionPlan.Free;
    public int MaxUsers { get; set; } = 5;
    public int MaxWorkflows { get; set; } = 10;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public virtual ICollection<OrganizationMember> Members { get; set; } = new List<OrganizationMember>();
    public virtual ICollection<Workflow> Workflows { get; set; } = new List<Workflow>();
    public virtual ICollection<OrganizationInvite> Invites { get; set; } = new List<OrganizationInvite>();
}