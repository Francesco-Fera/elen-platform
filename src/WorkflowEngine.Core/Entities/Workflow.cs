using System.ComponentModel.DataAnnotations;

namespace WorkflowEngine.Core.Entities;

public class Workflow
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public WorkflowStatus Status { get; set; } = WorkflowStatus.Draft;

    // JSON columns for PostgreSQL
    public string? NodesJson { get; set; }
    public string? ConnectionsJson { get; set; }
    public string? SettingsJson { get; set; } = "{}";

    public int Version { get; set; } = 1;

    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastModified { get; set; }

    // Navigation properties
    public virtual User Creator { get; set; } = null!;
    public virtual ICollection<WorkflowExecution> Executions { get; set; } = new List<WorkflowExecution>();
}
