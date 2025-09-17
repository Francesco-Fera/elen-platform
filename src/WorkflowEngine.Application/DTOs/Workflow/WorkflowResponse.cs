using WorkflowEngine.Core.Enums;

namespace WorkflowEngine.Application.DTOs.Workflow;

public class WorkflowResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public WorkflowStatus Status { get; set; }
    public WorkflowVisibility Visibility { get; set; }
    public bool IsTemplate { get; set; }
    public int Version { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastModified { get; set; }

    // Creator information
    public Guid CreatedBy { get; set; }
    public string? CreatorName { get; set; }
    public string? CreatorEmail { get; set; }

    // Organization information
    public Guid OrganizationId { get; set; }
    public string? OrganizationName { get; set; }

    // Workflow structure (optional - only included in detailed views)
    public List<WorkflowNodeDto>? Nodes { get; set; }
    public List<NodeConnectionDto>? Connections { get; set; }
    public Dictionary<string, object>? Settings { get; set; }

    // Statistics (optional)
    public int? ExecutionCount { get; set; }
    public DateTime? LastExecutedAt { get; set; }
    public double? AverageExecutionTime { get; set; }
    public double? SuccessRate { get; set; }
}
