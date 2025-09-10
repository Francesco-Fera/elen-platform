using WorkflowEngine.Core.Entities;
using WorkflowEngine.Core.Enums;

namespace WorkflowEngine.Application.Interfaces.Auth;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    Guid? OrganizationId { get; }
    string? Email { get; }
    string? OrganizationSlug { get; }
    OrganizationRole? OrganizationRole { get; }
    bool IsEmailVerified { get; }
    Task<User?> GetCurrentUserAsync();
    Task<Organization?> GetCurrentOrganizationAsync();
    Task<bool> HasPermissionAsync(string permission);
    Task<bool> CanAccessWorkflowAsync(Guid workflowId);
    Task<bool> SwitchOrganizationAsync(Guid organizationId);
}
