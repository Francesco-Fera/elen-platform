using WorkflowEngine.Application.DTOs.Auth;

namespace WorkflowEngine.Application.Interfaces.Auth;

public interface IAuthService
{
    Task<AuthResult> LoginAsync(LoginRequest request);
    Task<AuthResult> RegisterAsync(RegisterRequest request);
    Task<AuthResult> RefreshTokenAsync(string refreshToken);
    Task<bool> LogoutAsync(string refreshToken);
    Task<bool> SendEmailVerificationAsync(string email);
    Task<bool> VerifyEmailAsync(string token);
    Task<bool> SendPasswordResetAsync(string email);
    Task<bool> ResetPasswordAsync(string token, string newPassword);
}