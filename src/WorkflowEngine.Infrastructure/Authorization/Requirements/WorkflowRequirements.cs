using Microsoft.AspNetCore.Authorization;
using WorkflowEngine.Core.Enums;

namespace WorkflowEngine.Infrastructure.Authorization.Requirements;

public class WorkflowAccessRequirement : IAuthorizationRequirement
{
    public WorkflowPermissionType MinimumPermission { get; }

    public WorkflowAccessRequirement(WorkflowPermissionType minimumPermission = WorkflowPermissionType.View)
    {
        MinimumPermission = minimumPermission;
    }
}

public class WorkflowOwnerRequirement : IAuthorizationRequirement { }