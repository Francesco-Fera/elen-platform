using WorkflowEngine.Application.DTOs.Execution;
using WorkflowEngine.Application.DTOs.Workflow;
using WorkflowEngine.Core.Enums;

namespace WorkflowEngine.Application.Interfaces.Workflow;

public interface IWorkflowService
{
    // Core CRUD operations
    Task<WorkflowListResponse> GetWorkflowsAsync(GetWorkflowsRequest request);
    Task<WorkflowResponse?> GetWorkflowByIdAsync(Guid id);
    Task<WorkflowResponse?> CreateWorkflowAsync(CreateWorkflowRequest request);
    Task<WorkflowResponse?> UpdateWorkflowAsync(Guid id, UpdateWorkflowRequest request);
    Task<bool> DeleteWorkflowAsync(Guid id);

    // Workflow operations
    Task<WorkflowResponse?> DuplicateWorkflowAsync(Guid sourceId, DuplicateWorkflowRequest request);
    Task<WorkflowResponse?> UpdateWorkflowStatusAsync(Guid id, WorkflowStatus status);

    // Validation and testing
    Task<WorkflowValidationResult> ValidateWorkflowAsync(Guid id);
    Task<WorkflowValidationResult> ValidateWorkflowDataAsync(List<WorkflowNodeDto> nodes, List<NodeConnectionDto> connections);

    // Statistics and analytics
    Task<WorkflowStatistics?> GetWorkflowStatisticsAsync(Guid id);
    Task<ExecutionListResponse> GetWorkflowExecutionsAsync(Guid id, GetExecutionsRequest request);

    // Permissions and sharing
    Task<bool> CanUserAccessWorkflowAsync(Guid workflowId, Guid userId);
    Task<bool> CanUserEditWorkflowAsync(Guid workflowId, Guid userId);
    Task<bool> CanUserDeleteWorkflowAsync(Guid workflowId, Guid userId);

    // Templates and sharing
    Task<WorkflowListResponse> GetWorkflowTemplatesAsync(GetWorkflowsRequest request);
    Task<WorkflowResponse?> CreateFromTemplateAsync(Guid templateId, CreateWorkflowRequest request);

    // Versioning (future implementation)
    Task<List<WorkflowVersionDto>> GetWorkflowVersionsAsync(Guid id);
    Task<WorkflowResponse?> CreateWorkflowVersionAsync(Guid id, string? versionNote = null);
    Task<WorkflowResponse?> RestoreWorkflowVersionAsync(Guid id, int version);
}