using WorkflowEngine.Application.DTOs.Common;

namespace WorkflowEngine.Application.DTOs.Execution;

public class ExecutionListResponse
{
    public List<ExecutionSummaryDto> Executions { get; set; } = new();
    public PaginationDto Pagination { get; set; } = new();
}