using Microsoft.EntityFrameworkCore;
using WorkflowEngine.Application.DTOs.Organization;
using WorkflowEngine.Application.Interfaces.Auth;
using WorkflowEngine.Application.Services.Auth;
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
            return false; // Already a member

        // Check for existing pending invite
        var existingInvite = await _context.OrganizationInvites
            .FirstOrDefaultAsync(i => i.Email == request.Email &&
                                     i.OrganizationId == _currentUserService.OrganizationId &&
                                     i.Status == InviteStatus.Pending);

        if (existingInvite != null)
            return false; // Invite already pending

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
        // TODO: Implementare logica di accettazione invito
        return await Task.FromResult(true);
    }

    public async Task<bool> DeclineInviteAsync(string inviteToken)
    {
        // TODO: Implementare logica di rifiuto invito
        return await Task.FromResult(true);
    }

    public async Task<bool> RemoveMemberAsync(Guid userId)
    {
        // TODO: Implementare rimozione membro
        return await Task.FromResult(true);
    }

    public async Task<bool> UpdateMemberRoleAsync(Guid userId, OrganizationRole newRole)
    {
        // TODO: Implementare aggiornamento ruolo
        return await Task.FromResult(true);
    }

    public async Task<List<OrganizationMemberResponse>> GetOrganizationMembersAsync()
    {
        // TODO: Implementare lista membri
        return new List<OrganizationMemberResponse>();
    }

    public async Task<List<PendingInviteResponse>> GetPendingInvitesAsync()
    {
        // TODO: Implementare lista inviti pendenti
        return new List<PendingInviteResponse>();
    }

    private string GenerateSlug(string name)
    {
        return name.ToLower()
            .Replace(" ", "-")
            .Replace("'", "")
            + "-" + Guid.NewGuid().ToString()[..8];
    }
}