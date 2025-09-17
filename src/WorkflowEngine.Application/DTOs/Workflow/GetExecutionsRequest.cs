using System.ComponentModel.DataAnnotations;
using WorkflowEngine.Core.Enums;

namespace WorkflowEngine.Application.DTOs.Workflow;

public class GetExecutionsRequest
{
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int Limit { get; set; } = 20;

    public ExecutionStatus? Status { get; set; }
    public ExecutionTrigger? TriggerType { get; set; }

    public DateTime? StartedAfter { get; set; }
    public DateTime? StartedBefore { get; set; }
    public DateTime? CompletedAfter { get; set; }
    public DateTime? CompletedBefore { get; set; }

    public string SortBy { get; set; } = "StartedAt";
    public string SortOrder { get; set; } = "desc";
}

