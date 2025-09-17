using System.ComponentModel.DataAnnotations;
using WorkflowEngine.Core.Enums;

namespace WorkflowEngine.Application.DTOs.Workflow;

public class UpdateWorkflowStatusRequest
{
    [Required]
    public WorkflowStatus Status { get; set; }
}
