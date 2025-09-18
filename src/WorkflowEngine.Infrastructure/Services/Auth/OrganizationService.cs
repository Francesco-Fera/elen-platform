using Microsoft.EntityFrameworkCore;
using WorkflowEngine.Application.DTOs.Organization;
using WorkflowEngine.Application.Interfaces.Auth;
using WorkflowEngine.Core.Entities;
using WorkflowEngine.Core.Enums;
using WorkflowEngine.Infrastructure.Data;

namespace WorkflowEngine.Infrastructure.Services.Auth;

public class OrganizationService : IOrganizationService
{
    private readonly WorkflowEngineDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public OrganizationService(WorkflowEngineDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<OrganizationResponse?> CreateOrganizationAsync(CreateOrganizationRequest request)
    {
        if (_currentUserService.UserId == null)
            return null;

        var organization = new Organization
        {
            Name = request.Name,
            Slug = GenerateSlug(request.Name),
            Description = request.Description,
            IsActive = true,
            Plan = SubscriptionPlan.Free,
            MaxUsers = 5,
            MaxWorkflows = 10
        };

        _context.Organizations.Add(organization);
        await _context.SaveChangesAsync();

        // Add current user as owner
        var membership = new OrganizationMember
        {
            OrganizationId = organization.Id,
            UserId = _currentUserService.UserId.Value,
            Role = OrganizationRole.Owner,
            IsActive = true
        };

        _context.OrganizationMembers.Add(membership);
        await _context.SaveChangesAsync();

        return new OrganizationResponse
        {
            Id = organization.Id,
            Name = organization.Name,
            Slug = organization.Slug,
            Description = organization.Description,
            Plan = organization.Plan.ToString(),
            IsActive = organization.IsActive,
            CreatedAt = organization.CreatedAt
        };
    }

    public async Task<bool> InviteMemberAsync(InviteMemberRequest request)
    {
        if (_currentUserService.UserId == null || _currentUserService.OrganizationId == null)
            return false;

        // Check if user has permission to invite (Admin or Owner)
        var currentRole = _currentUserService.OrganizationRole;
        if (currentRole != OrganizationRole.Owner && currentRole != OrganizationRole.Admin)
            return false;

        // Check if user is already a member
        var existingMember = await _context.OrganizationMembers
            .Include(m => m.User)
            .FirstOrDefaultAsync(m => m.User.Email == request.Email &&
                                     m.OrganizationId == _currentUserService.OrganizationId);

        if (existingMember != null)
            return false;

        // Check for existing pending invite
        var existingInvite = await _context.OrganizationInvites
            .FirstOrDefaultAsync(i => i.Email == request.Email &&
                                     i.OrganizationId == _currentUserService.OrganizationId &&
                                     i.Status == InviteStatus.Pending);

        if (existingInvite != null)
            return false;

        // Create invite
        var invite = new OrganizationInvite
        {
            OrganizationId = _currentUserService.OrganizationId.Value,
            Email = request.Email,
            Role = request.Role,
            InviteToken = Guid.NewGuid().ToString(),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            InvitedBy = _currentUserService.UserId.Value,
            Status = InviteStatus.Pending
        };

        _context.OrganizationInvites.Add(invite);
        await _context.SaveChangesAsync();

        // TODO: Send email invitation
        //await _emailService.SendInvitationEmailAsync(invite);
        return true;
    }

    public async Task<List<OrganizationResponse>> GetUserOrganizationsAsync()
    {
        if (_currentUserService.UserId == null)
            return new List<OrganizationResponse>();

        var organizations = await _context.OrganizationMembers
            .Where(m => m.UserId == _currentUserService.UserId && m.IsActive)
            .Include(m => m.Organization)
            .Select(m => new OrganizationResponse
            {
                Id = m.Organization.Id,
                Name = m.Organization.Name,
                Slug = m.Organization.Slug,
                Description = m.Organization.Description,
                Plan = m.Organization.Plan.ToString(),
                IsActive = m.Organization.IsActive,
                CreatedAt = m.Organization.CreatedAt,
                UserRole = m.Role.ToString()
            })
            .ToListAsync();

        return organizations;
    }

    public async Task<bool> AcceptInviteAsync(string inviteToken)
    {
        if (_currentUserService.UserId == null)
            return false;

        var invite = await _context.OrganizationInvites
            .Include(i => i.Organization)
            .FirstOrDefaultAsync(i => i.InviteToken == inviteToken &&
                                     i.Status == InviteStatus.Pending &&
                                     i.ExpiresAt > DateTime.UtcNow);

        if (invite == null)
            return false;

        var userEmail = _currentUserService.Email;
        if (userEmail != invite.Email)
            return false;

        // Check if user is already a member
        var existingMember = await _context.OrganizationMembers
            .FirstOrDefaultAsync(m => m.UserId == _currentUserService.UserId &&
                                     m.OrganizationId == invite.OrganizationId &&
                                     m.IsActive);

        if (existingMember != null)
        {
            // User is already a member, just mark invite as accepted
            invite.Status = InviteStatus.Accepted;
            invite.AcceptedBy = _currentUserService.UserId;
            invite.AcceptedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        // Check organization member limits
        var currentMemberCount = await _context.OrganizationMembers
            .CountAsync(m => m.OrganizationId == invite.OrganizationId && m.IsActive);

        if (currentMemberCount >= invite.Organization.MaxUsers)
            return false;

        // Create membership
        var membership = new OrganizationMember
        {
            OrganizationId = invite.OrganizationId,
            UserId = _currentUserService.UserId.Value,
            Role = invite.Role,
            IsActive = true,
            InvitedBy = invite.InvitedBy
        };

        _context.OrganizationMembers.Add(membership);

        // Update invite status
        invite.Status = InviteStatus.Accepted;
        invite.AcceptedBy = _currentUserService.UserId;
        invite.AcceptedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeclineInviteAsync(string inviteToken)
    {
        if (_currentUserService.UserId == null)
            return false;

        var invite = await _context.OrganizationInvites
            .FirstOrDefaultAsync(i => i.InviteToken == inviteToken &&
                                     i.Status == InviteStatus.Pending &&
                                     i.ExpiresAt > DateTime.UtcNow);

        if (invite == null)
            return false;

        // Check if user email matches invite email
        var userEmail = _currentUserService.Email;
        if (userEmail != invite.Email)
            return false;

        // Update invite status
        invite.Status = InviteStatus.Declined;
        invite.AcceptedBy = _currentUserService.UserId;
        invite.AcceptedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RemoveMemberAsync(Guid userId)
    {
        if (_currentUserService.UserId == null || _currentUserService.OrganizationId == null)
            return false;

        // Check if current user has permission
        var currentRole = _currentUserService.OrganizationRole;
        if (currentRole != OrganizationRole.Owner && currentRole != OrganizationRole.Admin)
            return false;

        // Cannot remove yourself
        if (userId == _currentUserService.UserId)
            return false;

        var membership = await _context.OrganizationMembers
            .FirstOrDefaultAsync(m => m.UserId == userId &&
                                     m.OrganizationId == _currentUserService.OrganizationId &&
                                     m.IsActive);

        if (membership == null)
            return false;

        // Owners can remove anyone, Admins cannot remove other Admins or Owners
        if (currentRole == OrganizationRole.Admin &&
            (membership.Role == OrganizationRole.Owner || membership.Role == OrganizationRole.Admin))
            return false;

        // Check if this is the last owner
        if (membership.Role == OrganizationRole.Owner)
        {
            var ownerCount = await _context.OrganizationMembers
                .CountAsync(m => m.OrganizationId == _currentUserService.OrganizationId &&
                               m.Role == OrganizationRole.Owner &&
                               m.IsActive);

            if (ownerCount <= 1)
                return false;
        }

        // Deactivate membership
        membership.IsActive = false;
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> UpdateMemberRoleAsync(Guid userId, OrganizationRole newRole)
    {
        if (_currentUserService.UserId == null || _currentUserService.OrganizationId == null)
            return false;

        // Check if current user has permission (Owner only)
        var currentRole = _currentUserService.OrganizationRole;
        if (currentRole != OrganizationRole.Owner)
            return false;

        // Cannot change your own role
        if (userId == _currentUserService.UserId)
            return false;

        var membership = await _context.OrganizationMembers
            .FirstOrDefaultAsync(m => m.UserId == userId &&
                                     m.OrganizationId == _currentUserService.OrganizationId &&
                                     m.IsActive);

        if (membership == null)
            return false;

        // If demoting from Owner, ensure there's at least one other Owner
        if (membership.Role == OrganizationRole.Owner && newRole != OrganizationRole.Owner)
        {
            var ownerCount = await _context.OrganizationMembers
                .CountAsync(m => m.OrganizationId == _currentUserService.OrganizationId &&
                               m.Role == OrganizationRole.Owner &&
                               m.IsActive);

            if (ownerCount <= 1)
                return false;
        }

        membership.Role = newRole;
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<List<OrganizationMemberResponse>> GetOrganizationMembersAsync()
    {
        if (_currentUserService.OrganizationId == null)
            return new List<OrganizationMemberResponse>();

        var members = await _context.OrganizationMembers
            .Where(m => m.OrganizationId == _currentUserService.OrganizationId && m.IsActive)
            .Include(m => m.User)
            .Include(m => m.Inviter)
            .Select(m => new OrganizationMemberResponse
            {
                Id = m.Id,
                UserId = m.UserId,
                Email = m.User.Email,
                FirstName = m.User.FirstName,
                LastName = m.User.LastName,
                Role = m.Role.ToString(),
                IsActive = m.IsActive,
                JoinedAt = m.JoinedAt,
                InvitedByEmail = m.Inviter != null ? m.Inviter.Email : null
            })
            .ToListAsync();

        return members;
    }

    public async Task<List<PendingInviteResponse>> GetPendingInvitesAsync()
    {
        if (_currentUserService.OrganizationId == null)
            return new List<PendingInviteResponse>();

        var invites = await _context.OrganizationInvites
            .Where(i => i.OrganizationId == _currentUserService.OrganizationId &&
                       i.Status == InviteStatus.Pending)
            .Include(i => i.Inviter)
            .Select(i => new PendingInviteResponse
            {
                Id = i.Id,
                Email = i.Email,
                Role = i.Role.ToString(),
                Status = i.Status.ToString(),
                CreatedAt = i.CreatedAt,
                ExpiresAt = i.ExpiresAt,
                InvitedByEmail = i.Inviter.Email
            })
            .ToListAsync();

        return invites;
    }

    private string GenerateSlug(string name)
    {
        return name.ToLower()
            .Replace(" ", "-")
            .Replace("'", "")
            + "-" + Guid.NewGuid().ToString()[..8];
    }
}