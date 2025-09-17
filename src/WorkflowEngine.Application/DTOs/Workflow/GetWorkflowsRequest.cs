using System.ComponentModel.DataAnnotations;
using WorkflowEngine.Core.Enums;

namespace WorkflowEngine.Application.DTOs.Workflow;

public class GetWorkflowsRequest
{
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int Limit { get; set; } = 20;

    public string? Search { get; set; }
    public WorkflowStatus? Status { get; set; }
    public WorkflowVisibility? Visibility { get; set; }
    public bool? IsTemplate { get; set; }

    // Sorting
    public string SortBy { get; set; } = "LastModified";
    public string SortOrder { get; set; } = "desc"; // asc or desc

    // Date filtering
    public DateTime? CreatedAfter { get; set; }
    public DateTime? CreatedBefore { get; set; }
    public DateTime? ModifiedAfter { get; set; }
    public DateTime? ModifiedBefore { get; set; }

    // Creator filtering
    public Guid? CreatedBy { get; set; }
}