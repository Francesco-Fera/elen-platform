using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkflowEngine.Application.DTOs.Auth;
using WorkflowEngine.Application.Interfaces.Auth;

namespace WorkflowEngine.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthService authService,
        ICurrentUserService currentUserService,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// Authenticate user with email and password
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            var result = await _authService.LoginAsync(request);

            if (!result.IsSuccess)
            {
                return BadRequest(new { message = result.ErrorMessage });
            }

            _logger.LogInformation("User {Email} logged in successfully", request.Email);

            return Ok(new
            {
                success = true,
                data = new
                {
                    accessToken = result.Data!.AccessToken,
                    refreshToken = result.Data.RefreshToken,
                    expiresIn = 15 * 60,
                    user = new
                    {
                        id = result.Data.User.Id,
                        email = result.Data.User.Email,
                        firstName = result.Data.User.FirstName,
                        lastName = result.Data.User.LastName,
                        currentOrganizationId = result.Data.User.CurrentOrganizationId,
                        isEmailVerified = result.Data.User.IsEmailVerified,
                        timeZone = result.Data.User.TimeZone,
                        createdAt = result.Data.User.CreatedAt,
                        updatedAt = result.Data.User.CreatedAt  // TODO: Fix updatedAt
                    },
                    organization = result.Data.CurrentOrganization != null ? new
                    {
                        id = result.Data.CurrentOrganization.Id,
                        name = result.Data.CurrentOrganization.Name,
                        slug = result.Data.CurrentOrganization.Slug,
                        description = result.Data.CurrentOrganization.Description,
                        domain = result.Data.CurrentOrganization.Domain,
                        isActive = result.Data.CurrentOrganization.IsActive,
                        isTrialAccount = result.Data.CurrentOrganization.IsTrialAccount,
                        trialExpiresAt = result.Data.CurrentOrganization.TrialExpiresAt,
                        subscriptionPlan = result.Data.CurrentOrganization.Plan.ToString(),
                        maxUsers = result.Data.CurrentOrganization.MaxUsers,
                        maxWorkflows = result.Data.CurrentOrganization.MaxWorkflows,
                        createdAt = result.Data.CurrentOrganization.CreatedAt,
                        updatedAt = result.Data.CurrentOrganization.UpdatedAt
                    } : null
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for user {Email}", request.Email);
            return StatusCode(500, new { message = "An error occurred during login" });
        }
    }

    /// <summary>
    /// Register a new user and create personal organization
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            var result = await _authService.RegisterAsync(request);

            if (!result.IsSuccess)
            {
                return BadRequest(new { message = result.ErrorMessage });
            }

            _logger.LogInformation("New user {Email} registered successfully", request.Email);

            return Ok(new
            {
                success = true,
                data = new
                {
                    accessToken = result.Data!.AccessToken,
                    refreshToken = result.Data.RefreshToken,
                    expiresIn = 15 * 60,
                    user = new
                    {
                        id = result.Data.User.Id,
                        email = result.Data.User.Email,
                        firstName = result.Data.User.FirstName,
                        lastName = result.Data.User.LastName,
                        currentOrganizationId = result.Data.User.CurrentOrganizationId,
                        isEmailVerified = result.Data.User.IsEmailVerified,
                        timeZone = result.Data.User.TimeZone,
                        createdAt = result.Data.User.CreatedAt,
                        updatedAt = result.Data.User.CreatedAt // TODO: Fix updatedAt
                    },
                    organization = new
                    {
                        id = result.Data.CurrentOrganization!.Id,
                        name = result.Data.CurrentOrganization.Name,
                        slug = result.Data.CurrentOrganization.Slug,
                        description = result.Data.CurrentOrganization.Description,
                        domain = result.Data.CurrentOrganization.Domain,
                        isActive = result.Data.CurrentOrganization.IsActive,
                        isTrialAccount = result.Data.CurrentOrganization.IsTrialAccount,
                        trialExpiresAt = result.Data.CurrentOrganization.TrialExpiresAt,
                        subscriptionPlan = result.Data.CurrentOrganization.Plan.ToString(),
                        maxUsers = result.Data.CurrentOrganization.MaxUsers,
                        maxWorkflows = result.Data.CurrentOrganization.MaxWorkflows,
                        createdAt = result.Data.CurrentOrganization.CreatedAt,
                        updatedAt = result.Data.CurrentOrganization.UpdatedAt
                    }
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration for user {Email}", request.Email);
            return StatusCode(500, new { message = "An error occurred during registration" });
        }
    }

    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
    {
        try
        {
            var result = await _authService.RefreshTokenAsync(request.RefreshToken);

            if (!result.IsSuccess)
            {
                return BadRequest(new { message = result.ErrorMessage });
            }

            return Ok(new
            {
                success = true,
                data = new
                {
                    accessToken = result.Data!.AccessToken,
                    refreshToken = result.Data.RefreshToken
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            return StatusCode(500, new { message = "An error occurred during token refresh" });
        }
    }

    /// <summary>
    /// Logout and revoke refresh token
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request)
    {
        try
        {
            await _authService.LogoutAsync(request.RefreshToken);

            _logger.LogInformation("User {UserId} logged out successfully", _currentUserService.UserId);

            return Ok(new { success = true, message = "Logged out successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout for user {UserId}", _currentUserService.UserId);
            return StatusCode(500, new { message = "An error occurred during logout" });
        }
    }

    /// <summary>
    /// Get current authenticated user information
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        try
        {
            var user = await _currentUserService.GetCurrentUserAsync();
            var organization = await _currentUserService.GetCurrentOrganizationAsync();

            if (user == null)
            {
                return Unauthorized(new { message = "User not found" });
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
                        timeZone = user.TimeZone,
                        isEmailVerified = user.IsEmailVerified,
                        lastLoginAt = user.LastLoginAt
                    },
                    organization = organization != null ? new
                    {
                        id = organization.Id,
                        name = organization.Name,
                        slug = organization.Slug,
                        plan = organization.Plan.ToString(),
                        role = _currentUserService.OrganizationRole?.ToString()
                    } : null
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user {UserId}", _currentUserService.UserId);
            return StatusCode(500, new { message = "An error occurred while retrieving user information" });
        }
    }

    /// <summary>
    /// Send email verification link
    /// </summary>
    [HttpPost("send-verification")]
    [Authorize]
    public async Task<IActionResult> SendEmailVerification()
    {
        try
        {
            if (_currentUserService.IsEmailVerified)
            {
                return BadRequest(new { message = "Email is already verified" });
            }

            var success = await _authService.SendEmailVerificationAsync(_currentUserService.Email!);

            if (!success)
            {
                return BadRequest(new { message = "Failed to send verification email" });
            }

            return Ok(new { success = true, message = "Verification email sent successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending verification email for user {UserId}", _currentUserService.UserId);
            return StatusCode(500, new { message = "An error occurred while sending verification email" });
        }
    }

    /// <summary>
    /// Verify email with token
    /// </summary>
    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request)
    {
        try
        {
            var success = await _authService.VerifyEmailAsync(request.Token);

            if (!success)
            {
                return BadRequest(new { message = "Invalid or expired verification token" });
            }

            return Ok(new { success = true, message = "Email verified successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying email with token");
            return StatusCode(500, new { message = "An error occurred during email verification" });
        }
    }

    /// <summary>
    /// Send password reset email
    /// </summary>
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        try
        {
            var success = await _authService.SendPasswordResetAsync(request.Email);

            // Always return success to avoid email enumeration
            return Ok(new { success = true, message = "If the email exists, a password reset link has been sent" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending password reset email");
            return StatusCode(500, new { message = "An error occurred while sending password reset email" });
        }
    }

    /// <summary>
    /// Reset password with token
    /// </summary>
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        try
        {
            var success = await _authService.ResetPasswordAsync(request.Token, request.Password);

            if (!success)
            {
                return BadRequest(new { message = "Invalid or expired reset token" });
            }

            return Ok(new { success = true, message = "Password reset successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting password with token");
            return StatusCode(500, new { message = "An error occurred during password reset" });
        }
    }
}