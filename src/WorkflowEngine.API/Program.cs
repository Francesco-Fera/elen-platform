using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Security.Claims;
using System.Text;
using WorkflowEngine.API.Middleware;
using WorkflowEngine.Application.Constants;
using WorkflowEngine.Application.Interfaces.Auth;
using WorkflowEngine.Application.Interfaces.Services;
using WorkflowEngine.Application.Services.Auth;
using WorkflowEngine.Core.Entities;
using WorkflowEngine.Core.Enums;
using WorkflowEngine.Infrastructure.Authorization.Handlers;
using WorkflowEngine.Infrastructure.Authorization.Requirements;
using WorkflowEngine.Infrastructure.Data;
using WorkflowEngine.Infrastructure.Services;
using WorkflowEngine.Infrastructure.Services.Auth;
using WorkflowEngine.Infrastructure.Services.Email;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console()
    .WriteTo.File("logs/workflowengine-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();

// Configure Entity Framework
builder.Services.AddDbContext<WorkflowEngineDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JWT");
var secretKey = jwtSettings["Secret"] ?? throw new InvalidOperationException("JWT Secret not configured");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false; // Set to true in production
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };

    // Handle WebSocket authentication for SignalR
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;

            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            if (context.Principal?.Identity is ClaimsIdentity identity)
            {
                var claimsToAdd = new List<Claim>();
                var claimsToRemove = new List<Claim>();

                foreach (var claim in identity.Claims.ToList())
                {
                    switch (claim.Type)
                    {
                        case ClaimTypes.NameIdentifier:
                            claimsToAdd.Add(new Claim("user_id", claim.Value));
                            break;
                        case ClaimTypes.Email:
                            claimsToAdd.Add(new Claim("email", claim.Value));
                            break;
                        case ClaimTypes.GivenName:
                            claimsToAdd.Add(new Claim("first_name", claim.Value));
                            break;
                        case ClaimTypes.Surname:
                            claimsToAdd.Add(new Claim("last_name", claim.Value));
                            break;
                        case ClaimTypes.Name:
                            claimsToAdd.Add(new Claim("full_name", claim.Value));
                            break;
                    }
                }

                identity.AddClaims(claimsToAdd);
            }

            return Task.CompletedTask;
        }
    };
});

// Configure Authorization Policies
builder.Services.AddAuthorization(options =>
{
    // Organization policies
    options.AddPolicy(AuthorizationPolicies.OrganizationMember, policy =>
        policy.Requirements.Add(new OrganizationMemberRequirement()));

    options.AddPolicy(AuthorizationPolicies.OrganizationAdmin, policy =>
        policy.Requirements.Add(new OrganizationAdminRequirement()));

    options.AddPolicy(AuthorizationPolicies.OrganizationOwner, policy =>
        policy.Requirements.Add(new OrganizationOwnerRequirement()));

    // Workflow policies
    options.AddPolicy(AuthorizationPolicies.WorkflowView, policy =>
        policy.Requirements.Add(new WorkflowAccessRequirement(WorkflowPermissionType.View)));

    options.AddPolicy(AuthorizationPolicies.WorkflowEdit, policy =>
        policy.Requirements.Add(new WorkflowAccessRequirement(WorkflowPermissionType.Edit)));

    options.AddPolicy(AuthorizationPolicies.WorkflowExecute, policy =>
        policy.Requirements.Add(new WorkflowAccessRequirement(WorkflowPermissionType.Execute)));

    options.AddPolicy(AuthorizationPolicies.WorkflowManage, policy =>
        policy.Requirements.Add(new WorkflowAccessRequirement(WorkflowPermissionType.Manage)));

    options.AddPolicy(AuthorizationPolicies.WorkflowOwner, policy =>
        policy.Requirements.Add(new WorkflowOwnerRequirement()));

    // Email verification policy
    options.AddPolicy(AuthorizationPolicies.EmailVerified, policy =>
        policy.RequireClaim("email_verified", "true"));
});

// Register Authorization Handlers
builder.Services.AddScoped<IAuthorizationHandler, OrganizationMemberHandler>();
builder.Services.AddScoped<IAuthorizationHandler, OrganizationAdminHandler>();
builder.Services.AddScoped<IAuthorizationHandler, OrganizationOwnerHandler>();
builder.Services.AddScoped<IAuthorizationHandler, WorkflowAccessHandler>();
builder.Services.AddScoped<IAuthorizationHandler, WorkflowOwnerHandler>();

// Register Application Services
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IOrganizationService, OrganizationService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

// Email Services
var emailProvider = builder.Configuration["Email:Provider"];
if (emailProvider?.ToLower() == "sendgrid")
{
    builder.Services.AddScoped<IEmailService, SendGridEmailService>();
}
else
{
    builder.Services.AddScoped<IEmailService, SmtpEmailService>();
}

builder.Services.AddScoped<ITokenService, TokenService>();


// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.SetIsOriginAllowed(origin => true) // Allow any origin in development
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Background Services for token cleanup
builder.Services.AddHostedService<TokenCleanupService>();

// Configure Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "WorkflowEngine API",
        Version = "v1",
        Description = "Visual Workflow Automation Platform API with Multi-Tenancy"
    });

    // Add JWT Authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer' [space] and then your token"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Health Checks
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!)
    .AddRedis(builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379");

var app = builder.Build();

// Test endpoints
app.MapGet("/test-db", async (WorkflowEngineDbContext context) =>
{
    try
    {
        await context.Database.CanConnectAsync();
        return Results.Ok("Database connection successful!");
    }
    catch (Exception ex)
    {
        return Results.Problem($"Database connection failed: {ex.Message}");
    }
});

app.MapGet("/test", () => "API is working!");

// Test multi-tenant endpoint
app.MapGet("/test-auth", (ICurrentUserService currentUser) =>
{
    return Results.Ok(new
    {
        IsAuthenticated = currentUser.UserId != null,
        UserId = currentUser.UserId,
        OrganizationId = currentUser.OrganizationId,
        Email = currentUser.Email,
        Role = currentUser.OrganizationRole?.ToString()
    });
}).RequireAuthorization();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "WorkflowEngine API v1");
        c.RoutePrefix = string.Empty; // Serve Swagger UI at root
    });
}

app.UseCors("AllowFrontend");

app.UseHttpsRedirection();

// Add tenant resolution middleware before authentication

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<TenantResolutionMiddleware>();

app.MapControllers();

// Health check endpoint
app.MapHealthChecks("/health");

// Auto-migrate database on startup (only in development)
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<WorkflowEngineDbContext>();
    await context.Database.MigrateAsync();
}

try
{
    Log.Information("Starting WorkflowEngine API with Multi-Tenant Authentication");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}