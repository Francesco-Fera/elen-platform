namespace WorkflowEngine.Application.DTOs.Workflow;

public class WorkflowVersionDto
{
    public Guid Id { get; set; }
    public Guid WorkflowId { get; set; }
    public int VersionNumber { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? VersionNote { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public string? CreatorName { get; set; }

    // Workflow structure
    public List<WorkflowNodeDto> Nodes { get; set; } = new();
    public List<NodeConnectionDto> Connections { get; set; } = new();
    public Dictionary<string, object> Settings { get; set; } = new();
}
