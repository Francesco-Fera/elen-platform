using System.ComponentModel.DataAnnotations;
using WorkflowEngine.Core.Enums;

namespace WorkflowEngine.Application.DTOs.Workflow;

public class DuplicateWorkflowRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public WorkflowVisibility Visibility { get; set; } = WorkflowVisibility.Private;
    public bool IsTemplate { get; set; } = false;
}
