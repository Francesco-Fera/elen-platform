using WorkflowEngine.Application.DTOs.Organization;

namespace WorkflowEngine.Application.Interfaces.Auth;

public interface IOrganizationService
{
    Task<OrganizationResponse?> CreateOrganizationAsync(CreateOrganizationRequest request);
    Task<bool> InviteMemberAsync(InviteMemberRequest request);
    Task<List<OrganizationResponse>> GetUserOrganizationsAsync();
    Task<bool> AcceptInviteAsync(string inviteToken);
    Task<bool> DeclineInviteAsync(string inviteToken);
    Task<bool> RemoveMemberAsync(Guid userId);
    Task<bool> UpdateMemberRoleAsync(Guid userId, Core.Enums.OrganizationRole newRole);
    Task<List<OrganizationMemberResponse>> GetOrganizationMembersAsync();
    Task<List<PendingInviteResponse>> GetPendingInvitesAsync();
}