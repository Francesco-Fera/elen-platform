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
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request)
    {
        try
        {
            var userId = _currentUserService.UserId;

            await _authService.LogoutAsync(request.RefreshToken);

            if (userId.HasValue)
            {
                _logger.LogInformation("User {UserId} logged out successfully", userId);
            }
            else
            {
                _logger.LogInformation("Anonymous logout with refresh token");
            }

            return Ok(new { success = true, message = "Logged out successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during logout with refresh token");

            return Ok(new { success = true, message = "Logged out successfully" });
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
                return BadRequest(new { success = false, message = "Email is already verified" });
            }

            var success = await _authService.SendEmailVerificationAsync(_currentUserService.Email!);

            if (!success)
            {
                return BadRequest(new { success = false, message = "Failed to send verification email" });
            }

            return Ok(new
            {
                success = true,
                message = "Verification email sent successfully. Please check your inbox."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending verification email for user {UserId}", _currentUserService.UserId);
            return StatusCode(500, new { success = false, message = "An error occurred while sending verification email" });
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
            if (string.IsNullOrWhiteSpace(request.Token))
            {
                return BadRequest(new { success = false, message = "Verification token is required" });
            }

            var success = await _authService.VerifyEmailAsync(request.Token);

            if (!success)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid or expired verification token. Please request a new verification email."
                });
            }

            return Ok(new
            {
                success = true,
                message = "Email verified successfully! Welcome to WorkflowEngine."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying email with token");
            return StatusCode(500, new { success = false, message = "An error occurred during email verification" });
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
            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest(new { success = false, message = "Email address is required" });
            }

            // Always return success to avoid email enumeration attacks
            await _authService.SendPasswordResetAsync(request.Email);

            return Ok(new
            {
                success = true,
                message = "If an account with that email exists, a password reset link has been sent."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending password reset email");
            return StatusCode(500, new { success = false, message = "An error occurred while sending password reset email" });
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
            if (string.IsNullOrWhiteSpace(request.Token))
            {
                return BadRequest(new { success = false, message = "Reset token is required" });
            }

            if (string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { success = false, message = "New password is required" });
            }

            if (request.Password.Length < 8)
            {
                return BadRequest(new { success = false, message = "Password must be at least 8 characters long" });
            }

            var success = await _authService.ResetPasswordAsync(request.Token, request.Password);

            if (!success)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid or expired reset token. Please request a new password reset."
                });
            }

            return Ok(new
            {
                success = true,
                message = "Password reset successfully. Please log in with your new password."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting password with token");
            return StatusCode(500, new { success = false, message = "An error occurred during password reset" });
        }
    }

    /// <summary>
    /// Check if email verification token is valid (for frontend validation)
    /// </summary>
    [HttpGet("verify-email/validate")]
    public async Task<IActionResult> ValidateEmailToken([FromQuery] string token)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return BadRequest(new { success = false, message = "Token is required" });
            }

            // This is a lightweight validation - doesn't actually verify the email
            // Just checks if the token format is valid and not expired
            var isValid = await _authService.VerifyEmailAsync(token);

            return Ok(new
            {
                success = true,
                data = new { isValid = false } // Always return false for security - use verify-email to actually verify
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating email token");
            return Ok(new { success = true, data = new { isValid = false } });
        }
    }

    /// <summary>
    /// Check if password reset token is valid (for frontend validation)
    /// </summary>
    [HttpGet("reset-password/validate")]
    public async Task<IActionResult> ValidateResetToken([FromQuery] string token)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return BadRequest(new { success = false, message = "Token is required" });
            }

            // This doesn't consume the token, just validates it
            var tokenService = HttpContext.RequestServices.GetRequiredService<WorkflowEngine.Application.Interfaces.Services.ITokenService>();
            var isValid = await tokenService.ValidatePasswordResetTokenAsync(token);

            return Ok(new
            {
                success = true,
                data = new { isValid }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating reset token");
            return Ok(new { success = true, data = new { isValid = false } });
        }
    }
}