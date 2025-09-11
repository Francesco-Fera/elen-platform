using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WorkflowEngine.Application.DTOs.Auth;
using WorkflowEngine.Application.Interfaces.Auth;
using WorkflowEngine.Application.Services.Auth;
using WorkflowEngine.Core.Entities;
using WorkflowEngine.Infrastructure.Data;
using WorkflowEngine.Infrastructure.Services.Auth;

namespace WorkflowEngine.IntegrationTests;

public class SimpleAuthTests
{
    [Fact]
    public async Task RegisterUser_ShouldCreateUserAndOrganization()
    {
        // Arrange - Create a simple in-memory test environment
        var services = new ServiceCollection();

        // Add DbContext with InMemory database
        services.AddDbContext<WorkflowEngineDbContext>(options =>
            options.UseInMemoryDatabase("TestDb_" + Guid.NewGuid()));

        // Add configuration
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
        services.AddSingleton<IConfiguration>(configuration);

        // Add services
        services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IAuthService, AuthService>();

        var serviceProvider = services.BuildServiceProvider();

        // Ensure database is created
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<WorkflowEngineDbContext>();
        await context.Database.EnsureCreatedAsync();

        var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();

        var registerRequest = new RegisterRequest
        {
            Email = "test@example.com",
            Password = "TestPassword123!",
            FirstName = "John",
            LastName = "Doe",
            TimeZone = "UTC"
        };

        // Act
        var result = await authService.RegisterAsync(registerRequest);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(registerRequest.Email, result.Data.User.Email);
        Assert.NotNull(result.Data.CurrentOrganization);
        Assert.Contains("John", result.Data.CurrentOrganization.Name);

        // Verify user was created in database
        var user = await context.Users.FirstOrDefaultAsync(u => u.Email == registerRequest.Email);
        Assert.NotNull(user);

        // Verify organization was created
        var org = await context.Organizations.FirstOrDefaultAsync();
        Assert.NotNull(org);

        // Verify membership was created
        var membership = await context.OrganizationMembers
            .FirstOrDefaultAsync(m => m.UserId == user.Id && m.OrganizationId == org.Id);
        Assert.NotNull(membership);
        Assert.Equal(Core.Enums.OrganizationRole.Owner, membership.Role);
    }

    [Fact]
    public async Task LoginUser_ShouldReturnTokens_WhenValidCredentials()
    {
        // Arrange
        var services = new ServiceCollection();

        services.AddDbContext<WorkflowEngineDbContext>(options =>
            options.UseInMemoryDatabase("TestDb_" + Guid.NewGuid()));

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
        services.AddSingleton<IConfiguration>(configuration);

        services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IAuthService, AuthService>();

        var serviceProvider = services.BuildServiceProvider();

        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<WorkflowEngineDbContext>();
        await context.Database.EnsureCreatedAsync();

        var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();

        // First register a user
        var registerRequest = new RegisterRequest
        {
            Email = "login@example.com",
            Password = "TestPassword123!",
            FirstName = "Jane",
            LastName = "Smith"
        };
        await authService.RegisterAsync(registerRequest);

        var loginRequest = new LoginRequest
        {
            Email = "login@example.com",
            Password = "TestPassword123!"
        };

        // Act
        var result = await authService.LoginAsync(loginRequest);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.AccessToken);
        Assert.NotNull(result.Data.RefreshToken);
        Assert.Equal(loginRequest.Email, result.Data.User.Email);
    }

    [Fact]
    public async Task LoginUser_ShouldReturnError_WhenInvalidCredentials()
    {
        // Arrange
        var services = new ServiceCollection();

        services.AddDbContext<WorkflowEngineDbContext>(options =>
            options.UseInMemoryDatabase("TestDb_" + Guid.NewGuid()));

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
        services.AddSingleton<IConfiguration>(configuration);

        services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IAuthService, AuthService>();

        var serviceProvider = services.BuildServiceProvider();

        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<WorkflowEngineDbContext>();
        await context.Database.EnsureCreatedAsync();

        var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();

        var loginRequest = new LoginRequest
        {
            Email = "nonexistent@example.com",
            Password = "WrongPassword"
        };

        // Act
        var result = await authService.LoginAsync(loginRequest);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Invalid email or password", result.ErrorMessage);
    }

    [Fact]
    public async Task RefreshToken_ShouldReturnNewTokens_WhenValidRefreshToken()
    {
        // Arrange
        var services = new ServiceCollection();

        services.AddDbContext<WorkflowEngineDbContext>(options =>
            options.UseInMemoryDatabase("TestDb_" + Guid.NewGuid()));

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
        services.AddSingleton<IConfiguration>(configuration);

        services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IAuthService, AuthService>();

        var serviceProvider = services.BuildServiceProvider();

        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<WorkflowEngineDbContext>();
        await context.Database.EnsureCreatedAsync();

        var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();

        // Register and get tokens
        var registerRequest = new RegisterRequest
        {
            Email = "refresh@example.com",
            Password = "TestPassword123!",
            FirstName = "Refresh",
            LastName = "User"
        };
        var registerResult = await authService.RegisterAsync(registerRequest);
        var originalRefreshToken = registerResult.Data.RefreshToken;

        // Act
        var result = await authService.RefreshTokenAsync(originalRefreshToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.AccessToken);
        Assert.NotNull(result.Data.RefreshToken);
        Assert.NotEqual(originalRefreshToken, result.Data.RefreshToken); // Should be different
    }
}