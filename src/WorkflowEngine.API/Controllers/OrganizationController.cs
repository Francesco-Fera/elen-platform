using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkflowEngine.Application.Constants;
using WorkflowEngine.Application.DTOs.Organization;
using WorkflowEngine.Application.Interfaces.Auth;


namespace WorkflowEngine.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrganizationController : ControllerBase
{
    private readonly IOrganizationService _organizationService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<OrganizationController> _logger;

    public OrganizationController(
        IOrganizationService organizationService,
        ICurrentUserService currentUserService,
        ILogger<OrganizationController> logger)
    {
        _organizationService = organizationService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// Get all organizations for the current user
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetUserOrganizations()
    {
        try
        {
            var organizations = await _organizationService.GetUserOrganizationsAsync();

            return Ok(new
            {
                success = true,
                data = organizations
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting organizations for user {UserId}", _currentUserService.UserId);
            return StatusCode(500, new { message = "An error occurred while retrieving organizations" });
        }
    }

    /// <summary>
    /// Create a new organization
    /// </summary>
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.EmailVerified)]
    public async Task<IActionResult> CreateOrganization([FromBody] CreateOrganizationRequest request)
    {
        try
        {
            var organization = await _organizationService.CreateOrganizationAsync(request);

            if (organization == null)
            {
                return BadRequest(new { message = "Failed to create organization" });
            }

            _logger.LogInformation("User {UserId} created organization {OrganizationId}",
                _currentUserService.UserId, organization.Id);

            return CreatedAtAction(
                nameof(GetUserOrganizations),
                new { id = organization.Id },
                new { success = true, data = organization });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating organization for user {UserId}", _currentUserService.UserId);
            return StatusCode(500, new { message = "An error occurred while creating the organization" });
        }
    }

    /// <summary>
    /// Get current organization information
    /// </summary>
    [HttpGet("current")]
    [Authorize(Policy = AuthorizationPolicies.OrganizationMember)]
    public async Task<IActionResult> GetCurrentOrganization()
    {
        try
        {
            var organization = await _currentUserService.GetCurrentOrganizationAsync();

            if (organization == null)
            {
                return NotFound(new { message = "Current organization not found" });
            }

            return Ok(new
            {
                success = true,
                data = new
                {
                    id = organization.Id,
                    name = organization.Name,
                    slug = organization.Slug,
                    description = organization.Description,
                    plan = organization.Plan.ToString(),
                    maxUsers = organization.MaxUsers,
                    maxWorkflows = organization.MaxWorkflows,
                    isActive = organization.IsActive,
                    createdAt = organization.CreatedAt,
                    userRole = _currentUserService.OrganizationRole?.ToString()
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current organization for user {UserId}", _currentUserService.UserId);
            return StatusCode(500, new { message = "An error occurred while retrieving organization information" });
        }
    }

    /// <summary>
    /// Switch to a different organization
    /// </summary>
    [HttpPost("switch/{organizationId}")]
    public async Task<IActionResult> SwitchOrganization(Guid organizationId)
    {
        try
        {
            var success = await _currentUserService.SwitchOrganizationAsync(organizationId);

            if (!success)
            {
                return BadRequest(new { message = "Unable to switch to the specified organization" });
            }

            _logger.LogInformation("User {UserId} switched to organization {OrganizationId}",
                _currentUserService.UserId, organizationId);

            return Ok(new { success = true, message = "Organization switched successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error switching organization for user {UserId}", _currentUserService.UserId);
            return StatusCode(500, new { message = "An error occurred while switching organizations" });
        }
    }

    /// <summary>
    /// Get organization members
    /// </summary>
    [HttpGet("members")]
    [Authorize(Policy = AuthorizationPolicies.OrganizationMember)]
    public async Task<IActionResult> GetOrganizationMembers()
    {
        try
        {
            var members = await _organizationService.GetOrganizationMembersAsync();

            return Ok(new
            {
                success = true,
                data = members
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting organization members for org {OrganizationId}",
                _currentUserService.OrganizationId);
            return StatusCode(500, new { message = "An error occurred while retrieving organization members" });
        }
    }

    /// <summary>
    /// Invite a new member to the organization
    /// </summary>
    [HttpPost("invite")]
    [Authorize(Policy = AuthorizationPolicies.OrganizationAdmin)]
    public async Task<IActionResult> InviteMember([FromBody] InviteMemberRequest request)
    {
        try
        {
            var success = await _organizationService.InviteMemberAsync(request);

            if (!success)
            {
                return BadRequest(new { message = "Failed to send invitation" });
            }

            _logger.LogInformation("User {UserId} invited {Email} to organization {OrganizationId}",
                _currentUserService.UserId, request.Email, _currentUserService.OrganizationId);

            return Ok(new { success = true, message = "Invitation sent successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inviting member {Email} to organization {OrganizationId}",
                request.Email, _currentUserService.OrganizationId);
            return StatusCode(500, new { message = "An error occurred while sending the invitation" });
        }
    }

    /// <summary>
    /// Get pending invitations
    /// </summary>
    [HttpGet("invites")]
    [Authorize(Policy = AuthorizationPolicies.OrganizationAdmin)]
    public async Task<IActionResult> GetPendingInvites()
    {
        try
        {
            var invites = await _organizationService.GetPendingInvitesAsync();

            return Ok(new
            {
                success = true,
                data = invites
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending invites for organization {OrganizationId}",
                _currentUserService.OrganizationId);
            return StatusCode(500, new { message = "An error occurred while retrieving pending invitations" });
        }
    }

    /// <summary>
    /// Accept an organization invitation
    /// </summary>
    [HttpPost("invite/{inviteToken}/accept")]
    public async Task<IActionResult> AcceptInvite(string inviteToken)
    {
        try
        {
            var success = await _organizationService.AcceptInviteAsync(inviteToken);

            if (!success)
            {
                return BadRequest(new { message = "Invalid or expired invitation token" });
            }

            _logger.LogInformation("User {UserId} accepted invitation {InviteToken}",
                _currentUserService.UserId, inviteToken);

            return Ok(new { success = true, message = "Invitation accepted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error accepting invitation {InviteToken}", inviteToken);
            return StatusCode(500, new { message = "An error occurred while accepting the invitation" });
        }
    }

    /// <summary>
    /// Decline an organization invitation
    /// </summary>
    [HttpPost("invite/{inviteToken}/decline")]
    public async Task<IActionResult> DeclineInvite(string inviteToken)
    {
        try
        {
            var success = await _organizationService.DeclineInviteAsync(inviteToken);

            if (!success)
            {
                return BadRequest(new { message = "Invalid or expired invitation token" });
            }

            _logger.LogInformation("User {UserId} declined invitation {InviteToken}",
                _currentUserService.UserId, inviteToken);

            return Ok(new { success = true, message = "Invitation declined successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error declining invitation {InviteToken}", inviteToken);
            return StatusCode(500, new { message = "An error occurred while declining the invitation" });
        }
    }

    /// <summary>
    /// Remove a member from the organization
    /// </summary>
    [HttpDelete("members/{userId}")]
    [Authorize(Policy = AuthorizationPolicies.OrganizationAdmin)]
    public async Task<IActionResult> RemoveMember(Guid userId)
    {
        try
        {
            var success = await _organizationService.RemoveMemberAsync(userId);

            if (!success)
            {
                return BadRequest(new { message = "Failed to remove member" });
            }

            _logger.LogInformation("User {UserId} removed member {MemberId} from organization {OrganizationId}",
                _currentUserService.UserId, userId, _currentUserService.OrganizationId);

            return Ok(new { success = true, message = "Member removed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing member {MemberId} from organization {OrganizationId}",
                userId, _currentUserService.OrganizationId);
            return StatusCode(500, new { message = "An error occurred while removing the member" });
        }
    }

    /// <summary>
    /// Update member role in the organization
    /// </summary>
    [HttpPut("members/{userId}/role")]
    [Authorize(Policy = AuthorizationPolicies.OrganizationAdmin)]
    public async Task<IActionResult> UpdateMemberRole(Guid userId, [FromBody] UpdateMemberRoleRequest request)
    {
        try
        {
            var success = await _organizationService.UpdateMemberRoleAsync(userId, request.Role);

            if (!success)
            {
                return BadRequest(new { message = "Failed to update member role" });
            }

            _logger.LogInformation("User {UserId} updated member {MemberId} role to {Role} in organization {OrganizationId}",
                _currentUserService.UserId, userId, request.Role, _currentUserService.OrganizationId);

            return Ok(new { success = true, message = "Member role updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating member {MemberId} role in organization {OrganizationId}",
                userId, _currentUserService.OrganizationId);
            return StatusCode(500, new { message = "An error occurred while updating the member role" });
        }
    }
}