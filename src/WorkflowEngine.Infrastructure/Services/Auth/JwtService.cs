using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using WorkflowEngine.Application.Services.Auth;
using WorkflowEngine.Core.Entities;
using WorkflowEngine.Infrastructure.Data;

namespace WorkflowEngine.Infrastructure.Services.Auth;


public class JwtService : IJwtService
{
    private readonly IConfiguration _configuration;
    private readonly WorkflowEngineDbContext _context;
    private readonly string _secretKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly TimeSpan _accessTokenExpiry;
    private readonly TimeSpan _refreshTokenExpiry;

    public JwtService(IConfiguration configuration, WorkflowEngineDbContext context)
    {
        _configuration = configuration;
        _context = context;
        _secretKey = configuration["JWT:Secret"] ?? throw new InvalidOperationException("JWT Secret not configured");
        _issuer = configuration["JWT:Issuer"] ?? "WorkflowEngine";
        _audience = configuration["JWT:Audience"] ?? "WorkflowEngine";
        _accessTokenExpiry = TimeSpan.FromMinutes(int.Parse(configuration["JWT:AccessTokenExpiryMinutes"] ?? "15"));
        _refreshTokenExpiry = TimeSpan.FromDays(int.Parse(configuration["JWT:RefreshTokenExpiryDays"] ?? "7"));
    }

    public async Task<string> GenerateAccessTokenAsync(User user, Organization? currentOrganization = null)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new("user_id", user.Id.ToString()),
            new("email", user.Email),
            new("email_verified", user.IsEmailVerified.ToString().ToLower())
        };

        // Add name claims if available
        if (!string.IsNullOrEmpty(user.FirstName))
            claims.Add(new(ClaimTypes.GivenName, user.FirstName));

        if (!string.IsNullOrEmpty(user.LastName))
            claims.Add(new(ClaimTypes.Surname, user.LastName));

        if (!string.IsNullOrEmpty(user.TimeZone))
            claims.Add(new("timezone", user.TimeZone));

        // Add organization context if available
        if (currentOrganization != null)
        {
            claims.Add(new("org_id", currentOrganization.Id.ToString()));
            claims.Add(new("org_name", currentOrganization.Name));
            claims.Add(new("org_slug", currentOrganization.Slug ?? ""));
            claims.Add(new("org_plan", currentOrganization.Plan.ToString()));

            // Get user's role in this organization
            var membership = await _context.OrganizationMembers
                .Where(m => m.UserId == user.Id && m.OrganizationId == currentOrganization.Id && m.IsActive)
                .FirstOrDefaultAsync();

            if (membership != null)
            {
                claims.Add(new("org_role", membership.Role.ToString()));
                claims.Add(new(ClaimTypes.Role, $"org:{membership.Role.ToString().ToLower()}"));
            }
        }

        // Add all user's organizations for context switching
        var userOrganizations = await _context.OrganizationMembers
            .Where(m => m.UserId == user.Id && m.IsActive)
            .Include(m => m.Organization)
            .Select(m => new { m.Organization.Id, m.Organization.Name, m.Role })
            .ToListAsync();

        foreach (var org in userOrganizations)
        {
            claims.Add(new("user_orgs", $"{org.Id}:{org.Name}:{org.Role}"));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.Add(_accessTokenExpiry),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<string> GenerateRefreshTokenAsync()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_secretKey);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                ValidateAudience = true,
                ValidAudience = _audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
            return principal;
        }
        catch
        {
            return null;
        }
    }

    public async Task<RefreshToken> CreateRefreshTokenAsync(User user)
    {
        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = await GenerateRefreshTokenAsync(),
            ExpiresAt = DateTime.UtcNow.Add(_refreshTokenExpiry)
        };

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();

        return refreshToken;
    }

    public async Task<bool> ValidateRefreshTokenAsync(string token)
    {
        var refreshToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.TokenHash == token && !rt.IsRevoked && rt.ExpiresAt > DateTime.UtcNow);

        return refreshToken != null;
    }

    public async Task RevokeRefreshTokenAsync(string token)
    {
        var refreshToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.TokenHash == token);

        if (refreshToken != null)
        {
            refreshToken.IsRevoked = true;
            refreshToken.RevokedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}