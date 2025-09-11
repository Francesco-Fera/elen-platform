using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkflowEngine.Application.DTOs.User;
using WorkflowEngine.Application.Interfaces.Auth;

namespace WorkflowEngine.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<UserController> _logger;
    private readonly IUserService _userService;

    public UserController(
        ICurrentUserService currentUserService,
        ILogger<UserController> logger,
        IUserService userService)
    {
        _currentUserService = currentUserService;
        _logger = logger;
        _userService = userService;
    }

    /// <summary>
    /// Get current user profile
    /// </summary>
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        try
        {
            var user = await _currentUserService.GetCurrentUserAsync();
            var organization = await _currentUserService.GetCurrentOrganizationAsync();

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            return Ok(new
            {
                success = true,
                data = new
                {
                    user = new
                    {
                        id = user.Id,
                        email = user.Email,
                        firstName = user.FirstName,
                        lastName = user.LastName,
                        currentOrganizationId = user.CurrentOrganizationId,
                        isEmailVerified = user.IsEmailVerified,
                        timeZone = user.TimeZone,
                        createdAt = user.CreatedAt,
                        updatedAt = user.CreatedAt // TODO: Fix updatedAt 
                    },
                    organization = organization != null ? new
                    {
                        id = organization.Id,
                        name = organization.Name,
                        slug = organization.Slug,
                        description = organization.Description,
                        domain = organization.Domain,
                        isActive = organization.IsActive,
                        isTrialAccount = organization.IsTrialAccount,
                        trialExpiresAt = organization.TrialExpiresAt,
                        subscriptionPlan = organization.Plan.ToString(),
                        maxUsers = organization.MaxUsers,
                        maxWorkflows = organization.MaxWorkflows,
                        createdAt = organization.CreatedAt,
                        updatedAt = organization.UpdatedAt
                    } : null
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting profile for user {UserId}", _currentUserService.UserId);
            return StatusCode(500, new { message = "An error occurred while retrieving user profile" });
        }
    }

    /// <summary>
    /// Update user profile information
    /// </summary>
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        try
        {
            var updatedUser = await _userService.UpdateProfileAsync(request);

            if (updatedUser == null)
            {
                return BadRequest(new { message = "Failed to update profile" });
            }

            _logger.LogInformation("User {UserId} updated profile", _currentUserService.UserId);

            return Ok(new
            {
                success = true,
                message = "Profile updated successfully",
                data = new
                {
                    id = updatedUser.Id,
                    email = updatedUser.Email,
                    firstName = updatedUser.FirstName,
                    lastName = updatedUser.LastName,
                    timeZone = updatedUser.TimeZone
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating profile for user {UserId}", _currentUserService.UserId);
            return StatusCode(500, new { message = "An error occurred while updating profile" });
        }
    }

    // <summary>
    /// Change user password
    /// </summary>
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        try
        {
            if (_currentUserService.UserId == null)
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            var success = await _userService.ChangePasswordAsync(
                _currentUserService.UserId.Value,
                request.CurrentPassword,
                request.NewPassword);

            if (!success)
            {
                return BadRequest(new { message = "Current password is incorrect" });
            }

            _logger.LogInformation("User {UserId} changed password", _currentUserService.UserId);

            return Ok(new { success = true, message = "Password changed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for user {UserId}", _currentUserService.UserId);
            return StatusCode(500, new { message = "An error occurred while changing password" });
        }
    }

    /// <summary>
    /// Check user permissions for a specific action
    /// </summary>
    [HttpGet("permissions/{permission}")]
    public async Task<IActionResult> CheckPermission(string permission)
    {
        try
        {
            var hasPermission = await _currentUserService.HasPermissionAsync(permission);

            return Ok(new
            {
                success = true,
                data = new
                {
                    permission = permission,
                    hasPermission = hasPermission
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking permission {Permission} for user {UserId}",
                permission, _currentUserService.UserId);
            return StatusCode(500, new { message = "An error occurred while checking permissions" });
        }
    }

    /// <summary>
    /// Get user's organization memberships
    /// </summary>
    [HttpGet("organizations")]
    public async Task<IActionResult> GetUserOrganizations()
    {
        try
        {
            // This would typically call the organization service
            // For now, we'll return basic current organization info

            var currentOrg = await _currentUserService.GetCurrentOrganizationAsync();

            return Ok(new
            {
                success = true,
                data = new
                {
                    currentOrganizationId = _currentUserService.OrganizationId,
                    currentOrganizationRole = _currentUserService.OrganizationRole?.ToString(),
                    currentOrganization = currentOrg != null ? new
                    {
                        id = currentOrg.Id,
                        name = currentOrg.Name,
                        slug = currentOrg.Slug
                    } : null
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting organizations for user {UserId}", _currentUserService.UserId);
            return StatusCode(500, new { message = "An error occurred while retrieving user organizations" });
        }
    }

    /// <summary>
    /// Search for users (for inviting to organizations)
    /// </summary>
    [HttpGet("search")]
    public async Task<IActionResult> SearchUsers([FromQuery] string q, [FromQuery] int limit = 10)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            {
                return BadRequest(new { message = "Search term must be at least 2 characters" });
            }

            var users = await _userService.SearchUsersAsync(q, Math.Min(limit, 20));

            return Ok(new
            {
                success = true,
                data = users.Select(u => new
                {
                    id = u.Id,
                    email = u.Email,
                    firstName = u.FirstName,
                    lastName = u.LastName,
                    fullName = $"{u.FirstName} {u.LastName}".Trim()
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching users with term {SearchTerm}", q);
            return StatusCode(500, new { message = "An error occurred while searching users" });
        }
    }

    /// <summary>
    /// Deactivate user account
    /// </summary>
    [HttpPost("deactivate")]
    public async Task<IActionResult> DeactivateAccount()
    {
        try
        {
            if (_currentUserService.UserId == null)
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            var success = await _userService.DeactivateUserAsync(_currentUserService.UserId.Value);

            if (!success)
            {
                return BadRequest(new { message = "Failed to deactivate account" });
            }

            _logger.LogInformation("User {UserId} deactivated account", _currentUserService.UserId);

            return Ok(new { success = true, message = "Account deactivated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating account for user {UserId}", _currentUserService.UserId);
            return StatusCode(500, new { message = "An error occurred while deactivating account" });
        }
    }
}