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

    public UserController(
        ICurrentUserService currentUserService,
        ILogger<UserController> logger)
    {
        _currentUserService = currentUserService;
        _logger = logger;
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

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            return Ok(new
            {
                success = true,
                data = new
                {
                    id = user.Id,
                    email = user.Email,
                    firstName = user.FirstName,
                    lastName = user.LastName,
                    timeZone = user.TimeZone,
                    isEmailVerified = user.IsEmailVerified,
                    isActive = user.IsActive,
                    createdAt = user.CreatedAt,
                    lastLoginAt = user.LastLoginAt
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
            // TODO: Implement profile update logic
            // This would involve creating a user service to update profile information

            return Ok(new { success = true, message = "Profile updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating profile for user {UserId}", _currentUserService.UserId);
            return StatusCode(500, new { message = "An error occurred while updating profile" });
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
}