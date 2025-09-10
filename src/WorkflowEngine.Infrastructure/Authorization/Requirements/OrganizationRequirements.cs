using Microsoft.AspNetCore.Authorization;
using WorkflowEngine.Core.Enums;

namespace WorkflowEngine.Infrastructure.Authorization.Requirements;

public class OrganizationMemberRequirement : IAuthorizationRequirement
{
    public OrganizationRole MinimumRole { get; }

    public OrganizationMemberRequirement(OrganizationRole minimumRole = OrganizationRole.Member)
    {
        MinimumRole = minimumRole;
    }
}

public class OrganizationOwnerRequirement : IAuthorizationRequirement { }

public class OrganizationAdminRequirement : IAuthorizationRequirement { }
