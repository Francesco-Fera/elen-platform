using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using WorkflowEngine.Application.Services.Auth;
using WorkflowEngine.Core.Entities;
using WorkflowEngine.Core.Enums;
using WorkflowEngine.Infrastructure.Authorization.Requirements;
using WorkflowEngine.Infrastructure.Data;

namespace WorkflowEngine.Infrastructure.Authorization.Handlers;
public class WorkflowAccessHandler : AuthorizationHandler<WorkflowAccessRequirement, Workflow>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly WorkflowEngineDbContext _context;

    public WorkflowAccessHandler(ICurrentUserService currentUserService, WorkflowEngineDbContext context)
    {
        _currentUserService = currentUserService;
        _context = context;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        WorkflowAccessRequirement requirement,
        Workflow workflow)
    {
        if (_currentUserService.UserId == null)
        {
            context.Fail();
            return;
        }

        var userId = _currentUserService.UserId.Value;

        // 1. Check if user is the workflow creator
        if (workflow.CreatedBy == userId)
        {
            context.Succeed(requirement);
            return;
        }

        // 2. Check if workflow belongs to user's current organization
        if (workflow.OrganizationId == _currentUserService.OrganizationId)
        {
            // Check organization-level permissions based on visibility
            if (workflow.Visibility == WorkflowVisibility.Organization)
            {
                var userRole = _currentUserService.OrganizationRole;
                if (userRole != null && IsRoleSufficientForPermission(userRole.Value, requirement.MinimumPermission))
                {
                    context.Succeed(requirement);
                    return;
                }
            }
        }

        // 3. Check specific workflow permissions
        var permission = await _context.WorkflowPermissions
            .FirstOrDefaultAsync(p => p.WorkflowId == workflow.Id && p.UserId == userId);

        if (permission != null && IsPermissionSufficient(permission.Permission, requirement.MinimumPermission))
        {
            context.Succeed(requirement);
            return;
        }

        // 4. Check if workflow is public template
        if (workflow.Visibility == WorkflowVisibility.Public &&
            workflow.IsTemplate &&
            requirement.MinimumPermission == WorkflowPermissionType.View)
        {
            context.Succeed(requirement);
            return;
        }

        context.Fail();
    }

    private static bool IsRoleSufficientForPermission(OrganizationRole role, WorkflowPermissionType requiredPermission)
    {
        return role switch
        {
            OrganizationRole.Owner => true,
            OrganizationRole.Admin => true,
            OrganizationRole.Member => requiredPermission != WorkflowPermissionType.Manage,
            OrganizationRole.Viewer => requiredPermission == WorkflowPermissionType.View,
            _ => false
        };
    }

    private static bool IsPermissionSufficient(WorkflowPermissionType userPermission, WorkflowPermissionType requiredPermission)
    {
        var permissionHierarchy = new Dictionary<WorkflowPermissionType, int>
        {
            { WorkflowPermissionType.Manage, 4 },
            { WorkflowPermissionType.Execute, 3 },
            { WorkflowPermissionType.Edit, 2 },
            { WorkflowPermissionType.View, 1 }
        };

        return permissionHierarchy[userPermission] >= permissionHierarchy[requiredPermission];
    }
}

public class WorkflowOwnerHandler : AuthorizationHandler<WorkflowOwnerRequirement, Workflow>
{
    private readonly ICurrentUserService _currentUserService;

    public WorkflowOwnerHandler(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        WorkflowOwnerRequirement requirement,
        Workflow workflow)
    {
        if (_currentUserService.UserId == workflow.CreatedBy)
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