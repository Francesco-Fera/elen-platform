namespace WorkflowEngine.Application.Interfaces.Services;

public interface ITokenService
{
    Task<string> GenerateEmailVerificationTokenAsync(Guid userId);
    Task<bool> ValidateEmailVerificationTokenAsync(string token);
    Task<string> GeneratePasswordResetTokenAsync(Guid userId, string ipAddress, string userAgent);
    Task<bool> ValidatePasswordResetTokenAsync(string token);
    Task<Guid?> GetUserIdFromPasswordResetTokenAsync(string token);
    Task InvalidateAllPasswordResetTokensAsync(Guid userId);
    Task InvalidateAllEmailVerificationTokensAsync(Guid userId);
    Task CleanupExpiredTokensAsync();
}