using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using WorkflowEngine.Application.DTOs.Auth;
using WorkflowEngine.Core.Entities;
using WorkflowEngine.Infrastructure.Data;
using WorkflowEngine.Infrastructure.Services.Auth;

namespace WorkflowEngine.UnitTests.Services;

public class AuthServiceTests : IDisposable
{
    private readonly WorkflowEngineDbContext _context;
    private readonly AuthService _authService;
    private readonly JwtService _jwtService;
    private readonly IPasswordHasher<User> _passwordHasher;

    public AuthServiceTests()
    {
        var options = new DbContextOptionsBuilder<WorkflowEngineDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new WorkflowEngineDbContext(options);
        _context.Database.EnsureCreated();

        _passwordHasher = new PasswordHasher<User>();

        // Mock configuration for JWT service
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                {"JWT:Secret", "your-super-secret-jwt-key-that-is-at-least-32-characters-long"},
                {"JWT:Issuer", "WorkflowEngine"},
                {"JWT:Audience", "WorkflowEngine"},
                {"JWT:AccessTokenExpiryMinutes", "15"},
                {"JWT:RefreshTokenExpiryDays", "7"}
            })
            .Build();

        _jwtService = new JwtService(configuration, _context);
        _authService = new AuthService(_context, _jwtService, _passwordHasher);
    }

    [Fact]
    public async Task RegisterAsync_ShouldCreateUserAndOrganization_WhenValidRequest()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "test@example.com",
            Password = "TestPassword123!",
            FirstName = "John",
            LastName = "Doe",
            TimeZone = "UTC"
        };

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(request.Email, result.Data.User.Email);
        Assert.NotNull(result.Data.CurrentOrganization);

        // Verify user was created in database
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        Assert.NotNull(user);

        // Verify organization was created
        var org = await _context.Organizations.FirstOrDefaultAsync();
        Assert.NotNull(org);

        // Verify membership was created
        var membership = await _context.OrganizationMembers
            .FirstOrDefaultAsync(m => m.UserId == user.Id && m.OrganizationId == org.Id);
        Assert.NotNull(membership);
        Assert.Equal(Core.Enums.OrganizationRole.Owner, membership.Role);
    }

    [Fact]
    public async Task RegisterAsync_ShouldReturnError_WhenEmailAlreadyExists()
    {
        // Arrange
        var existingUser = new User
        {
            Email = "existing@example.com",
            PasswordHash = "hashedpassword"
        };
        _context.Users.Add(existingUser);
        await _context.SaveChangesAsync();

        var request = new RegisterRequest
        {
            Email = "existing@example.com",
            Password = "TestPassword123!",
            FirstName = "John",
            LastName = "Doe"
        };

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("already exists", result.ErrorMessage);
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnSuccess_WhenValidCredentials()
    {
        // Arrange
        var user = new User
        {
            Email = "test@example.com",
            FirstName = "John",
            LastName = "Doe",
            IsActive = true
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, "TestPassword123!");

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var request = new LoginRequest
        {
            Email = "test@example.com",
            Password = "TestPassword123!"
        };

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(request.Email, result.Data.User.Email);
        Assert.NotNull(result.Data.AccessToken);
        Assert.NotNull(result.Data.RefreshToken);
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnFailure_WhenInvalidCredentials()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "nonexistent@example.com",
            Password = "WrongPassword"
        };

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Invalid email or password", result.ErrorMessage);
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnFailure_WhenUserIsInactive()
    {
        // Arrange
        var user = new User
        {
            Email = "inactive@example.com",
            FirstName = "John",
            LastName = "Doe",
            IsActive = false // User is inactive
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, "TestPassword123!");

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var request = new LoginRequest
        {
            Email = "inactive@example.com",
            Password = "TestPassword123!"
        };

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("deactivated", result.ErrorMessage);
    }

    [Fact]
    public async Task RefreshTokenAsync_ShouldReturnNewToken_WhenValidRefreshToken()
    {
        // Arrange
        var user = new User
        {
            Email = "test@example.com",
            FirstName = "John",
            IsActive = true
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var refreshToken = await _jwtService.CreateRefreshTokenAsync(user);

        // Act
        var result = await _authService.RefreshTokenAsync(refreshToken.TokenHash);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.AccessToken);
        Assert.NotNull(result.Data.RefreshToken);
        Assert.NotEqual(refreshToken.TokenHash, result.Data.RefreshToken); // Should be different
    }

    [Fact]
    public async Task RefreshTokenAsync_ShouldReturnFailure_WhenInvalidRefreshToken()
    {
        // Arrange
        var invalidToken = "invalid-refresh-token";

        // Act
        var result = await _authService.RefreshTokenAsync(invalidToken);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Invalid refresh token", result.ErrorMessage);
    }

    [Fact]
    public async Task LogoutAsync_ShouldRevokeRefreshToken()
    {
        // Arrange
        var user = new User
        {
            Email = "test@example.com",
            FirstName = "John",
            IsActive = true
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var refreshToken = await _jwtService.CreateRefreshTokenAsync(user);

        // Act
        var result = await _authService.LogoutAsync(refreshToken.TokenHash);

        // Assert
        Assert.True(result);

        // Verify token is revoked
        var revokedToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.TokenHash == refreshToken.TokenHash);
        Assert.NotNull(revokedToken);
        Assert.True(revokedToken.IsRevoked);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}