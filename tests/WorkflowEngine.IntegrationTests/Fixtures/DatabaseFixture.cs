using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WorkflowEngine.Application.Interfaces.Auth;
using WorkflowEngine.Application.Interfaces.Workflow;
using WorkflowEngine.Core.Entities;
using WorkflowEngine.Core.Enums;
using WorkflowEngine.Execution.Engine;
using WorkflowEngine.Execution.Graph;
using WorkflowEngine.Execution.Interfaces;
using WorkflowEngine.Execution.Logging;
using WorkflowEngine.Infrastructure.Data;
using WorkflowEngine.Infrastructure.Services;
using WorkflowEngine.IntegrationTests.Helpers;
using WorkflowEngine.Nodes.Expressions;
using WorkflowEngine.Nodes.Interfaces;
using WorkflowEngine.Nodes.Registry;

namespace WorkflowEngine.IntegrationTests.Fixtures;

public class DatabaseFixture : IDisposable
{
    public WorkflowEngineDbContext DbContext { get; private set; }
    public IServiceProvider ServiceProvider { get; private set; }
    public Organization TestOrganization { get; private set; }
    public User TestUser { get; private set; }
    public OrganizationMember TestMembership { get; private set; }
    public CredentialType ApiKeyCredentialType { get; private set; }
    public HttpMessageHandlerMock HttpMock { get; private set; }


    public DatabaseFixture()
    {
        var services = new ServiceCollection();

        services.AddDbContext<WorkflowEngineDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Warning));

        HttpMock = new HttpMessageHandlerMock();
        HttpMock.SetupDefaultSuccess(new { success = true, data = "mock response" });

        services.AddSingleton<HttpMessageHandlerMock>(HttpMock);
        services.AddHttpClient("", client => { })
            .ConfigurePrimaryHttpMessageHandler(() => HttpMock);

        services.AddScoped<ICurrentUserService, TestCurrentUserService>();
        services.AddScoped<IWorkflowService, WorkflowService>();
        services.AddScoped<INodeRegistry, NodeRegistry>();
        services.AddScoped<IExpressionEvaluator, ExpressionEvaluator>();
        services.AddScoped<IWorkflowGraphBuilder, WorkflowGraphBuilder>();
        services.AddScoped<ITopologicalSorter, TopologicalSorter>();
        services.AddScoped<INodeExecutor, NodeExecutor>();
        services.AddScoped<IExecutionLogger, ExecutionLogger>();
        services.AddScoped<IExecutionContextFactory, ExecutionContextFactory>();
        services.AddScoped<IWorkflowExecutionEngine, WorkflowExecutionEngine>();
        services.AddScoped<ICredentialService, TestCredentialService>();

        ServiceProvider = services.BuildServiceProvider();
        DbContext = ServiceProvider.GetRequiredService<WorkflowEngineDbContext>();

        SeedDatabase();
    }

    private void SeedDatabase()
    {
        TestOrganization = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Test Organization",
            Slug = "test-org",
            IsActive = true,
            Plan = SubscriptionPlan.Free,
            MaxUsers = 10,
            MaxWorkflows = 100,
            CreatedAt = DateTime.UtcNow
        };

        TestUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            PasswordHash = "hash",
            FirstName = "Test",
            LastName = "User",
            IsActive = true,
            IsEmailVerified = true,
            CurrentOrganizationId = TestOrganization.Id,
            CreatedAt = DateTime.UtcNow
        };

        TestMembership = new OrganizationMember
        {
            Id = Guid.NewGuid(),
            OrganizationId = TestOrganization.Id,
            UserId = TestUser.Id,
            Role = OrganizationRole.Owner,
            IsActive = true,
            JoinedAt = DateTime.UtcNow
        };

        ApiKeyCredentialType = new CredentialType
        {
            Id = 1,
            Name = "API Key",
            Key = "api_key",
            AuthType = "api_key",
            FieldsJson = "[{\"name\":\"api_key\",\"type\":\"string\"}]",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        DbContext.Organizations.Add(TestOrganization);
        DbContext.Users.Add(TestUser);
        DbContext.OrganizationMembers.Add(TestMembership);
        DbContext.CredentialTypes.Add(ApiKeyCredentialType);
        DbContext.SaveChanges();
    }

    public void ResetDatabase()
    {
        var executions = DbContext.WorkflowExecutions.ToList();
        DbContext.WorkflowExecutions.RemoveRange(executions);

        var workflows = DbContext.Workflows.ToList();
        DbContext.Workflows.RemoveRange(workflows);

        DbContext.SaveChanges();

        HttpMock.Reset();
        HttpMock.SetupDefaultSuccess(new { success = true, data = "mock response" });
    }

    public void Dispose()
    {
        DbContext?.Database.EnsureDeleted();
        DbContext?.Dispose();
        if (ServiceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}

public class TestCurrentUserService : ICurrentUserService
{
    private readonly WorkflowEngineDbContext _context;
    private User? _cachedUser;
    private Organization? _cachedOrg;

    public TestCurrentUserService(WorkflowEngineDbContext context)
    {
        _context = context;
    }

    public Guid? UserId => GetCurrentUserAsync().Result?.Id;
    public Guid? OrganizationId => GetCurrentOrganizationAsync().Result?.Id;
    public string? Email => GetCurrentUserAsync().Result?.Email;
    public string? OrganizationSlug => GetCurrentOrganizationAsync().Result?.Slug;
    public OrganizationRole? OrganizationRole
    {
        get
        {
            var user = GetCurrentUserAsync().Result;
            var org = GetCurrentOrganizationAsync().Result;
            if (user == null || org == null) return null;

            var membership = _context.OrganizationMembers
                .FirstOrDefault(m => m.UserId == user.Id && m.OrganizationId == org.Id && m.IsActive);
            return membership?.Role;
        }
    }
    public bool IsEmailVerified => GetCurrentUserAsync().Result?.IsEmailVerified ?? false;

    public Task<User?> GetCurrentUserAsync()
    {
        _cachedUser ??= _context.Users
            .Include(u => u.CurrentOrganization)
            .FirstOrDefault(u => u.IsActive);
        return Task.FromResult(_cachedUser);
    }

    public Task<Organization?> GetCurrentOrganizationAsync()
    {
        if (_cachedOrg != null) return Task.FromResult<Organization?>(_cachedOrg);

        var user = GetCurrentUserAsync().Result;
        if (user?.CurrentOrganizationId == null) return Task.FromResult<Organization?>(null);

        _cachedOrg = _context.Organizations
            .FirstOrDefault(o => o.Id == user.CurrentOrganizationId && o.IsActive);
        return Task.FromResult(_cachedOrg);
    }

    public Task<bool> HasPermissionAsync(string permission)
    {
        var role = OrganizationRole;
        var hasPermission = role == Core.Enums.OrganizationRole.Owner ||
                           role == Core.Enums.OrganizationRole.Admin;
        return Task.FromResult(hasPermission);
    }

    public Task<bool> CanAccessWorkflowAsync(Guid workflowId)
    {
        var workflow = _context.Workflows.Find(workflowId);
        var canAccess = workflow?.OrganizationId == OrganizationId;
        return Task.FromResult(canAccess);
    }

    public async Task<bool> SwitchOrganizationAsync(Guid organizationId)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return false;

        var membership = await _context.OrganizationMembers
            .FirstOrDefaultAsync(m => m.UserId == user.Id &&
                                    m.OrganizationId == organizationId &&
                                    m.IsActive);

        if (membership == null) return false;

        user.CurrentOrganizationId = organizationId;
        _cachedOrg = null;
        await _context.SaveChangesAsync();
        return true;
    }
}

public class TestCredentialService : ICredentialService
{
    private readonly Dictionary<int, Dictionary<string, string>> _mockCredentials = new()
    {
        [1] = new Dictionary<string, string>
        {
            ["api_key"] = "test-api-key-123",
            ["token"] = "test-bearer-token"
        },
        [2] = new Dictionary<string, string>
        {
            ["username"] = "testuser",
            ["password"] = "testpass"
        },
        [3] = new Dictionary<string, string>
        {
            ["access_token"] = "test-oauth-token",
            ["refresh_token"] = "test-refresh-token"
        }
    };

    public Task<T> GetCredentialDataAsync<T>(int credentialId, Guid userId) where T : class
    {
        if (!_mockCredentials.TryGetValue(credentialId, out var data))
        {
            throw new KeyNotFoundException($"Credential {credentialId} not found");
        }

        if (typeof(T) == typeof(Dictionary<string, string>))
        {
            return Task.FromResult((data as T)!);
        }

        var instance = Activator.CreateInstance<T>();
        foreach (var prop in typeof(T).GetProperties())
        {
            if (data.TryGetValue(prop.Name.ToLower(), out var value) ||
                data.TryGetValue(prop.Name, out value))
            {
                prop.SetValue(instance, value);
            }
        }

        return Task.FromResult(instance);
    }

    public Task<Dictionary<string, string>> GetCredentialDataAsync(int credentialId, Guid userId)
    {
        if (!_mockCredentials.TryGetValue(credentialId, out var data))
        {
            throw new KeyNotFoundException($"Credential {credentialId} not found");
        }

        return Task.FromResult(new Dictionary<string, string>(data));
    }

    public Task<bool> ValidateCredentialAsync(int credentialId, Guid userId)
    {
        var isValid = _mockCredentials.ContainsKey(credentialId);
        return Task.FromResult(isValid);
    }

    public void AddMockCredential(int credentialId, Dictionary<string, string> data)
    {
        _mockCredentials[credentialId] = data;
    }
}