using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using WorkflowEngine.Application.Interfaces.Auth;
using WorkflowEngine.Core.Entities;
using WorkflowEngine.Core.Enums;
using WorkflowEngine.Infrastructure.Data;

namespace WorkflowEngine.Infrastructure.Services.Auth;
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly WorkflowEngineDbContext _context;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor, WorkflowEngineDbContext context)
    {
        _httpContextAccessor = httpContextAccessor;
        _context = context;
    }

    public Guid? UserId
    {
        get
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("user_id")?.Value;
            return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
        }
    }

    public Guid? OrganizationId
    {
        get
        {
            var orgIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("org_id")?.Value;
            return Guid.TryParse(orgIdClaim, out var orgId) ? orgId : null;
        }
    }

    public string? Email => _httpContextAccessor.HttpContext?.User?.FindFirst("email")?.Value;

    public string? OrganizationSlug => _httpContextAccessor.HttpContext?.User?.FindFirst("org_slug")?.Value;

    public OrganizationRole? OrganizationRole
    {
        get
        {
            var roleClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("org_role")?.Value;
            return Enum.TryParse<OrganizationRole>(roleClaim, out var role) ? role : null;
        }
    }

    public bool IsEmailVerified
    {
        get
        {
            var emailVerifiedClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("email_verified")?.Value;
            return bool.TryParse(emailVerifiedClaim, out var isVerified) && isVerified;
        }
    }

    public async Task<User?> GetCurrentUserAsync()
    {
        if (UserId == null) return null;

        return await _context.Users
            .Include(u => u.CurrentOrganization)
            .FirstOrDefaultAsync(u => u.Id == UserId);
    }

    public async Task<Organization?> GetCurrentOrganizationAsync()
    {
        if (OrganizationId == null) return null;

        return await _context.Organizations
            .FirstOrDefaultAsync(o => o.Id == OrganizationId);
    }

    public async Task<bool> HasPermissionAsync(string permission)
    {
        if (UserId == null || OrganizationId == null) return false;

        // Organization owners and admins have all permissions
        if (OrganizationRole == Core.Enums.OrganizationRole.Owner ||
            OrganizationRole == Core.Enums.OrganizationRole.Admin)
            return true;

        // Add specific permission logic here based on roles
        return OrganizationRole switch
        {
            Core.Enums.OrganizationRole.Member => permission != "delete_organization" && permission != "manage_billing",
            Core.Enums.OrganizationRole.Viewer => permission.StartsWith("view_") || permission.StartsWith("read_"),
            _ => false
        };
    }

    public async Task<bool> CanAccessWorkflowAsync(Guid workflowId)
    {
        if (UserId == null) return false;

        var workflow = await _context.Workflows
            .FirstOrDefaultAsync(w => w.Id == workflowId);

        if (workflow == null) return false;

        // Check if workflow belongs to user's current organization
        if (workflow.OrganizationId == OrganizationId) return true;

        // Check specific workflow permissions
        var permission = await _context.WorkflowPermissions
            .FirstOrDefaultAsync(p => p.WorkflowId == workflowId && p.UserId == UserId);

        return permission != null;
    }

    public async Task<bool> SwitchOrganizationAsync(Guid organizationId)
    {
        if (UserId == null) return false;

        // Check if user is member of the organization
        var membership = await _context.OrganizationMembers
            .FirstOrDefaultAsync(m => m.UserId == UserId &&
                                     m.OrganizationId == organizationId &&
                                     m.IsActive);

        if (membership == null) return false;

        // Update user's current organization
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == UserId);
        if (user != null)
        {
            user.CurrentOrganizationId = organizationId;
            await _context.SaveChangesAsync();
            return true;
        }

        return false;
    }
}