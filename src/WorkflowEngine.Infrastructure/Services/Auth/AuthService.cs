using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkflowEngine.Application.DTOs.Auth;
using WorkflowEngine.Application.Interfaces.Auth;
using WorkflowEngine.Application.Interfaces.Services;
using WorkflowEngine.Application.Services.Auth;
using WorkflowEngine.Core.Entities;
using WorkflowEngine.Infrastructure.Data;

namespace WorkflowEngine.Infrastructure.Services.Auth;

public class AuthService : IAuthService
{
    private readonly WorkflowEngineDbContext _context;
    private readonly IJwtService _jwtService;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IEmailService _emailService;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        WorkflowEngineDbContext context,
        IJwtService jwtService,
        IPasswordHasher<User> passwordHasher,
        IEmailService emailService,
        ITokenService tokenService,
        ILogger<AuthService> logger)
    {
        _context = context;
        _jwtService = jwtService;
        _passwordHasher = passwordHasher;
        _emailService = emailService;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request)
    {
        var user = await _context.Users
            .Include(u => u.CurrentOrganization)
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null)
            return AuthResult.Failure("Invalid email or password");

        if (!user.IsActive)
            return AuthResult.Failure("Account is deactivated");

        var passwordResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (passwordResult == PasswordVerificationResult.Failed)
            return AuthResult.Failure("Invalid email or password");

        // Update last login
        user.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Generate tokens
        var accessToken = await _jwtService.GenerateAccessTokenAsync(user, user.CurrentOrganization);
        var refreshToken = await _jwtService.CreateRefreshTokenAsync(user);

        var response = new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken.TokenHash,
            User = user,
            CurrentOrganization = user.CurrentOrganization
        };

        return AuthResult.Success(response);
    }

    public async Task<AuthResult> RegisterAsync(RegisterRequest request)
    {
        // Check if user already exists
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (existingUser != null)
            return AuthResult.Failure("User with this email already exists");

        // Create new user
        var user = new User
        {
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            TimeZone = request.TimeZone ?? "UTC",
            IsActive = true,
            IsEmailVerified = false
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var organization = new Organization
        {
            Name = request.OrganizationName,
            Slug = GenerateSlug(request.OrganizationName),
            IsActive = true,
            Plan = Core.Enums.SubscriptionPlan.Free,
            MaxUsers = 5,
            MaxWorkflows = 10
        };

        _context.Organizations.Add(organization);
        await _context.SaveChangesAsync();

        // Add user as owner of the organization
        var membership = new OrganizationMember
        {
            OrganizationId = organization.Id,
            UserId = user.Id,
            Role = Core.Enums.OrganizationRole.Owner,
            IsActive = true
        };

        _context.OrganizationMembers.Add(membership);

        // Set as user's current organization
        user.CurrentOrganizationId = organization.Id;
        await _context.SaveChangesAsync();

        // Generate tokens
        var accessToken = await _jwtService.GenerateAccessTokenAsync(user, organization);
        var refreshToken = await _jwtService.CreateRefreshTokenAsync(user);

        var response = new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken.TokenHash,
            User = user,
            CurrentOrganization = organization
        };

        return AuthResult.Success(response);
    }

    public async Task<AuthResult> RefreshTokenAsync(string refreshToken)
    {
        var isValid = await _jwtService.ValidateRefreshTokenAsync(refreshToken);
        if (!isValid)
            return AuthResult.Failure("Invalid refresh token");

        var token = await _context.RefreshTokens
            .Include(rt => rt.User)
            .ThenInclude(u => u.CurrentOrganization)
            .FirstOrDefaultAsync(rt => rt.TokenHash == refreshToken);

        if (token?.User == null)
            return AuthResult.Failure("Invalid refresh token");

        // Revoke old token
        await _jwtService.RevokeRefreshTokenAsync(refreshToken);

        // Generate new tokens
        var accessToken = await _jwtService.GenerateAccessTokenAsync(token.User, token.User.CurrentOrganization);
        var newRefreshToken = await _jwtService.CreateRefreshTokenAsync(token.User);

        var response = new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshToken.TokenHash,
            User = token.User,
            CurrentOrganization = token.User.CurrentOrganization
        };

        return AuthResult.Success(response);
    }

    public async Task<bool> LogoutAsync(string refreshToken)
    {
        await _jwtService.RevokeRefreshTokenAsync(refreshToken);
        return true;
    }

    public async Task<bool> SendEmailVerificationAsync(string email)
    {
        try
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                _logger.LogWarning("Email verification requested for non-existent user: {Email}", email);
                return false;
            }

            if (user.IsEmailVerified)
            {
                _logger.LogInformation("Email verification requested for already verified user: {Email}", email);
                return true;
            }

            var token = await _tokenService.GenerateEmailVerificationTokenAsync(user.Id);

            var success = await _emailService.SendEmailVerificationAsync(user.Email, user.FirstName, token);

            if (success)
            {
                _logger.LogInformation("Email verification sent successfully to: {Email}", email);
            }
            else
            {
                _logger.LogError("Failed to send email verification to: {Email}", email);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email verification to: {Email}", email);
            return false;
        }
    }

    public async Task<bool> VerifyEmailAsync(string token)
    {
        try
        {
            var success = await _tokenService.ValidateEmailVerificationTokenAsync(token);

            if (success)
            {
                _logger.LogInformation("Email verification successful for token");

                // Send welcome email after successful verification
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var verificationToken = await _context.EmailVerificationTokens
                            .Include(t => t.User)
                            .ThenInclude(u => u.CurrentOrganization)
                            .FirstOrDefaultAsync(t => t.Token == token);

                        if (verificationToken?.User != null)
                        {
                            await _emailService.SendWelcomeEmailAsync(
                                verificationToken.User.Email,
                                verificationToken.User.FirstName,
                                verificationToken.User.CurrentOrganization?.Name ?? "Your Organization"
                            );
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send welcome email after verification");
                    }
                });
            }
            else
            {
                _logger.LogWarning("Invalid email verification token provided");
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during email verification");
            return false;
        }
    }

    public async Task<bool> SendPasswordResetAsync(string email)
    {
        try
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email && u.IsActive);

            if (user == null)
            {
                _logger.LogWarning("Password reset requested for non-existent or inactive user: {Email}", email);
                // Always return true to prevent email enumeration
                return true;
            }

            var token = await _tokenService.GeneratePasswordResetTokenAsync(
                user.Id,
                "unknown", // IP address should be passed from controller
                "unknown"  // User agent should be passed from controller
            );

            var success = await _emailService.SendPasswordResetAsync(user.Email, user.FirstName, token);

            if (success)
            {
                _logger.LogInformation("Password reset email sent to: {Email}", email);
            }
            else
            {
                _logger.LogError("Failed to send password reset email to: {Email}", email);
            }

            // Always return true to prevent email enumeration
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending password reset to: {Email}", email);
            return true; // Still return true to prevent email enumeration
        }
    }

    public async Task<bool> ResetPasswordAsync(string token, string newPassword)
    {
        try
        {
            var userId = await _tokenService.GetUserIdFromPasswordResetTokenAsync(token);
            if (userId == null)
            {
                _logger.LogWarning("Invalid password reset token provided");
                return false;
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                _logger.LogError("User not found for password reset: {UserId}", userId);
                return false;
            }

            user.PasswordHash = _passwordHasher.HashPassword(user, newPassword);

            // Invalidate all existing refresh tokens for security
            var existingTokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == userId && !rt.IsRevoked)
                .ToListAsync();

            foreach (var rt in existingTokens)
            {
                rt.IsRevoked = true;
                rt.RevokedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Password reset successful for user: {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during password reset");
            return false;
        }
    }


    private string GenerateSlug(string input)
    {
        return input.ToLower()
            .Replace(" ", "-")
            .Replace("'", "")
            + "-" + Guid.NewGuid().ToString()[..8];
    }
}