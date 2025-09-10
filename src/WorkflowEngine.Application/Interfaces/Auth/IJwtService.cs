using System.Security.Claims;
using WorkflowEngine.Core.Entities;

namespace WorkflowEngine.Application.Services.Auth;

public interface IJwtService
{
    Task<string> GenerateAccessTokenAsync(User user, Organization? currentOrganization = null);
    Task<string> GenerateRefreshTokenAsync();
    ClaimsPrincipal? ValidateToken(string token);
    Task<RefreshToken> CreateRefreshTokenAsync(User user);
    Task<bool> ValidateRefreshTokenAsync(string token);
    Task RevokeRefreshTokenAsync(string token);
}
