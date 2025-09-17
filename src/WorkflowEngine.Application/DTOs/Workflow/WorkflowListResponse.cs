using WorkflowEngine.Application.DTOs.Common;
using WorkflowEngine.Core.Enums;

namespace WorkflowEngine.Application.DTOs.Workflow;

public class WorkflowListResponse
{
    public List<WorkflowSummaryDto> Workflows { get; set; } = new();
    public PaginationDto Pagination { get; set; } = new();
}

public class WorkflowSummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public WorkflowStatus Status { get; set; }
    public WorkflowVisibility Visibility { get; set; }
    public bool IsTemplate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastModified { get; set; }
    public string? CreatorName { get; set; }

    // Quick stats
    public int NodeCount { get; set; }
    public int ExecutionCount { get; set; }
    public DateTime? LastExecutedAt { get; set; }
    public ExecutionStatus? LastExecutionStatus { get; set; }
}
