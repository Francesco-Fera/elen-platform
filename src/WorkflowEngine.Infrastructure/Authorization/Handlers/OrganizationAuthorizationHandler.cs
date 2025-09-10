using Microsoft.AspNetCore.Authorization;
using WorkflowEngine.Application.Services.Auth;
using WorkflowEngine.Core.Enums;
using WorkflowEngine.Infrastructure.Authorization.Requirements;

namespace WorkflowEngine.Infrastructure.Authorization.Handlers;

public class OrganizationMemberHandler : AuthorizationHandler<OrganizationMemberRequirement>
{
    private readonly ICurrentUserService _currentUserService;

    public OrganizationMemberHandler(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OrganizationMemberRequirement requirement)
    {
        if (_currentUserService.UserId == null || _currentUserService.OrganizationId == null)
        {
            context.Fail();
            return Task.CompletedTask;
        }

        var userRole = _currentUserService.OrganizationRole;
        if (userRole == null)
        {
            context.Fail();
            return Task.CompletedTask;
        }

        // Check if user's role meets the minimum requirement
        if (IsRoleSufficient(userRole.Value, requirement.MinimumRole))
        {
            context.Succeed(requirement);
        }
        else
        {
            context.Fail();
        }

        return Task.CompletedTask;
    }

    private static bool IsRoleSufficient(OrganizationRole userRole, OrganizationRole requiredRole)
    {
        // Role hierarchy: Owner > Admin > Member > Viewer
        var roleHierarchy = new Dictionary<OrganizationRole, int>
        {
            { OrganizationRole.Owner, 4 },
            { OrganizationRole.Admin, 3 },
            { OrganizationRole.Member, 2 },
            { OrganizationRole.Viewer, 1 }
        };

        return roleHierarchy[userRole] >= roleHierarchy[requiredRole];
    }
}

public class OrganizationOwnerHandler : AuthorizationHandler<OrganizationOwnerRequirement>
{
    private readonly ICurrentUserService _currentUserService;

    public OrganizationOwnerHandler(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OrganizationOwnerRequirement requirement)
    {
        if (_currentUserService.OrganizationRole == OrganizationRole.Owner)
        {
            context.Succeed(requirement);
        }
        else
        {
            context.Fail();
        }

        return Task.CompletedTask;
    }
}

public class OrganizationAdminHandler : AuthorizationHandler<OrganizationAdminRequirement>
{
    private readonly ICurrentUserService _currentUserService;

    public OrganizationAdminHandler(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OrganizationAdminRequirement requirement)
    {
        var role = _currentUserService.OrganizationRole;
        if (role == OrganizationRole.Owner || role == OrganizationRole.Admin)
        {
            context.Succeed(requirement);
        }
        else
        {
            context.Fail();
        }

        return Task.CompletedTask;
    }
}