using System.ComponentModel.DataAnnotations;
using WorkflowEngine.Core.Enums;

namespace WorkflowEngine.Application.DTOs.Workflow;

public class UpdateWorkflowRequest
{
    [MaxLength(200)]
    public string? Name { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    public WorkflowVisibility? Visibility { get; set; }
    public bool? IsTemplate { get; set; }

    // Workflow structure updates
    public List<WorkflowNodeDto>? Nodes { get; set; }
    public List<NodeConnectionDto>? Connections { get; set; }
    public Dictionary<string, object>? Settings { get; set; }
}