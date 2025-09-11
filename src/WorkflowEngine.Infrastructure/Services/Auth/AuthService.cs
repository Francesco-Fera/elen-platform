using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WorkflowEngine.Application.DTOs.Auth;
using WorkflowEngine.Application.Interfaces.Auth;
using WorkflowEngine.Application.Services.Auth;
using WorkflowEngine.Core.Entities;
using WorkflowEngine.Infrastructure.Data;

namespace WorkflowEngine.Infrastructure.Services.Auth;

public class AuthService : IAuthService
{
    private readonly WorkflowEngineDbContext _context;
    private readonly IJwtService _jwtService;
    private readonly IPasswordHasher<User> _passwordHasher;

    public AuthService(
        WorkflowEngineDbContext context,
        IJwtService jwtService,
        IPasswordHasher<User> passwordHasher)
    {
        _context = context;
        _jwtService = jwtService;
        _passwordHasher = passwordHasher;
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
        // TODO: Implement email verification logic
        // Generate token, save to database, send email
        return await Task.FromResult(true);
    }

    public async Task<bool> VerifyEmailAsync(string token)
    {
        // TODO: Implement email verification logic
        return await Task.FromResult(true);
    }

    public async Task<bool> SendPasswordResetAsync(string email)
    {
        // TODO: Implement password reset logic
        return await Task.FromResult(true);
    }

    public async Task<bool> ResetPasswordAsync(string token, string newPassword)
    {
        // TODO: Implement password reset logic
        return await Task.FromResult(true);
    }

    private string GenerateSlug(string input)
    {
        return input.ToLower()
            .Replace(" ", "-")
            .Replace("'", "")
            + "-" + Guid.NewGuid().ToString()[..8];
    }
}