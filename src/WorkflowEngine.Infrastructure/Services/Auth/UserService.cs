using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WorkflowEngine.Application.DTOs.User;
using WorkflowEngine.Application.Interfaces.Auth;
using WorkflowEngine.Core.Entities;
using WorkflowEngine.Infrastructure.Data;

namespace WorkflowEngine.Infrastructure.Services.Auth;

public class UserService : IUserService
{
    private readonly WorkflowEngineDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPasswordHasher<User> _passwordHasher;

    public UserService(
        WorkflowEngineDbContext context,
        ICurrentUserService currentUserService,
        IPasswordHasher<User> passwordHasher)
    {
        _context = context;
        _currentUserService = currentUserService;
        _passwordHasher = passwordHasher;
    }

    public async Task<User?> GetUserByIdAsync(Guid userId)
    {
        return await _context.Users
            .Include(u => u.CurrentOrganization)
            .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
    }

    public async Task<User?> UpdateProfileAsync(UpdateProfileRequest request)
    {
        if (_currentUserService.UserId == null)
            return null;

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == _currentUserService.UserId);

        if (user == null)
            return null;

        // Update profile fields
        if (!string.IsNullOrWhiteSpace(request.FirstName))
            user.FirstName = request.FirstName.Trim();

        if (!string.IsNullOrWhiteSpace(request.LastName))
            user.LastName = request.LastName.Trim();

        if (!string.IsNullOrWhiteSpace(request.TimeZone))
            user.TimeZone = request.TimeZone;

        await _context.SaveChangesAsync();
        return user;
    }

    public async Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

        if (user == null)
            return false;

        // Verify current password
        var passwordResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, currentPassword);
        if (passwordResult == PasswordVerificationResult.Failed)
            return false;

        // Hash and set new password
        user.PasswordHash = _passwordHasher.HashPassword(user, newPassword);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeactivateUserAsync(Guid userId)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            return false;

        user.IsActive = false;

        // Also deactivate all organization memberships
        var memberships = await _context.OrganizationMembers
            .Where(m => m.UserId == userId && m.IsActive)
            .ToListAsync();

        foreach (var membership in memberships)
        {
            membership.IsActive = false;
        }

        // Revoke all refresh tokens
        var refreshTokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked)
            .ToListAsync();

        foreach (var token in refreshTokens)
        {
            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<User>> SearchUsersAsync(string searchTerm, int limit = 10)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return new List<User>();

        var searchTermLower = searchTerm.ToLower();

        return await _context.Users
            .Where(u => u.IsActive &&
                       (u.Email.ToLower().Contains(searchTermLower) ||
                        (u.FirstName != null && u.FirstName.ToLower().Contains(searchTermLower)) ||
                        (u.LastName != null && u.LastName.ToLower().Contains(searchTermLower))))
            .Take(limit)
            .ToListAsync();
    }
}