using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using WorkflowEngine.Application.Interfaces.Services;
using WorkflowEngine.Core.Entities;
using WorkflowEngine.Infrastructure.Data;

namespace WorkflowEngine.Infrastructure.Services;

public class TokenService : ITokenService
{
    private readonly WorkflowEngineDbContext _context;
    private readonly ILogger<TokenService> _logger;

    public TokenService(WorkflowEngineDbContext context, ILogger<TokenService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<string> GenerateEmailVerificationTokenAsync(Guid userId)
    {
        try
        {
            // Invalidate any existing verification tokens for this user
            await InvalidateAllEmailVerificationTokensAsync(userId);

            // Generate cryptographically secure token
            var token = GenerateSecureToken();

            var verificationToken = new EmailVerificationToken
            {
                UserId = userId,
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddHours(24), // 24 hours expiry
                IsUsed = false
            };

            _context.EmailVerificationTokens.Add(verificationToken);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Email verification token generated for user {UserId}", userId);
            return token;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate email verification token for user {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> ValidateEmailVerificationTokenAsync(string token)
    {
        try
        {
            var verificationToken = await _context.EmailVerificationTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Token == token);

            if (verificationToken == null || !verificationToken.IsValid)
            {
                _logger.LogWarning("Invalid or expired email verification token: {Token}", token);
                return false;
            }

            // Mark token as used
            verificationToken.IsUsed = true;
            verificationToken.UsedAt = DateTime.UtcNow;

            // Mark user as email verified
            verificationToken.User.IsEmailVerified = true;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Email verified for user {UserId}", verificationToken.UserId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating email verification token: {Token}", token);
            return false;
        }
    }

    public async Task<string> GeneratePasswordResetTokenAsync(Guid userId, string ipAddress, string userAgent)
    {
        try
        {
            // Invalidate any existing reset tokens for this user
            await InvalidateAllPasswordResetTokensAsync(userId);

            // Generate cryptographically secure token
            var token = GenerateSecureToken();

            var resetToken = new PasswordResetToken
            {
                UserId = userId,
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddHours(1), // 1 hour expiry for security
                IsUsed = false,
                IpAddress = ipAddress,
                UserAgent = userAgent
            };

            _context.PasswordResetTokens.Add(resetToken);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Password reset token generated for user {UserId} from IP {IpAddress}", userId, ipAddress);
            return token;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate password reset token for user {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> ValidatePasswordResetTokenAsync(string token)
    {
        try
        {
            var resetToken = await _context.PasswordResetTokens
                .FirstOrDefaultAsync(t => t.Token == token);

            return resetToken?.IsValid == true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating password reset token: {Token}", token);
            return false;
        }
    }

    public async Task<Guid?> GetUserIdFromPasswordResetTokenAsync(string token)
    {
        try
        {
            var resetToken = await _context.PasswordResetTokens
                .FirstOrDefaultAsync(t => t.Token == token);

            if (resetToken?.IsValid == true)
            {
                // Mark token as used
                resetToken.IsUsed = true;
                resetToken.UsedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Password reset token used for user {UserId}", resetToken.UserId);
                return resetToken.UserId;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user ID from password reset token: {Token}", token);
            return null;
        }
    }

    public async Task InvalidateAllPasswordResetTokensAsync(Guid userId)
    {
        try
        {
            var existingTokens = await _context.PasswordResetTokens
                .Where(t => t.UserId == userId && !t.IsUsed)
                .ToListAsync();

            foreach (var token in existingTokens)
            {
                token.IsUsed = true;
                token.UsedAt = DateTime.UtcNow;
            }

            if (existingTokens.Any())
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Invalidated {Count} password reset tokens for user {UserId}",
                    existingTokens.Count, userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating password reset tokens for user {UserId}", userId);
        }
    }

    public async Task InvalidateAllEmailVerificationTokensAsync(Guid userId)
    {
        try
        {
            var existingTokens = await _context.EmailVerificationTokens
                .Where(t => t.UserId == userId && !t.IsUsed)
                .ToListAsync();

            foreach (var token in existingTokens)
            {
                token.IsUsed = true;
                token.UsedAt = DateTime.UtcNow;
            }

            if (existingTokens.Any())
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Invalidated {Count} email verification tokens for user {UserId}",
                    existingTokens.Count, userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating email verification tokens for user {UserId}", userId);
        }
    }

    public async Task CleanupExpiredTokensAsync()
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-7); // Keep expired tokens for 7 days for audit

            // Delete old email verification tokens
            var expiredEmailTokens = await _context.EmailVerificationTokens
                .Where(t => t.ExpiresAt < cutoffDate)
                .ToListAsync();

            // Delete old password reset tokens
            var expiredResetTokens = await _context.PasswordResetTokens
                .Where(t => t.ExpiresAt < cutoffDate)
                .ToListAsync();

            _context.EmailVerificationTokens.RemoveRange(expiredEmailTokens);
            _context.PasswordResetTokens.RemoveRange(expiredResetTokens);

            await _context.SaveChangesAsync();

            _logger.LogInformation("Cleaned up {EmailTokenCount} email verification tokens and {ResetTokenCount} password reset tokens",
                expiredEmailTokens.Count, expiredResetTokens.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token cleanup");
        }
    }

    private static string GenerateSecureToken()
    {
        // Generate 32 random bytes (256 bits)
        var tokenBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(tokenBytes);

        // Convert to URL-safe base64 string
        return Convert.ToBase64String(tokenBytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }
}