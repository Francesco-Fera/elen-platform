using Microsoft.EntityFrameworkCore;
using WorkflowEngine.Infrastructure.Data;

namespace WorkflowEngine.API.Middleware;

public class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantResolutionMiddleware> _logger;

    public TenantResolutionMiddleware(RequestDelegate next, ILogger<TenantResolutionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, WorkflowEngineDbContext dbContext)
    {
        // Skip tenant resolution for authentication endpoints
        if (context.Request.Path.StartsWithSegments("/api/auth") ||
            context.Request.Path.StartsWithSegments("/health") ||
            context.Request.Path.StartsWithSegments("/swagger") ||
            context.Request.Path.StartsWithSegments("/test"))
        {
            await _next(context);
            return;
        }

        // Try to resolve tenant from different sources
        var tenantId = await ResolveTenantAsync(context, dbContext);

        if (tenantId.HasValue)
        {
            context.Items["TenantId"] = tenantId.Value;
            _logger.LogDebug("Resolved tenant: {TenantId}", tenantId.Value);
        }

        await _next(context);
    }

    private async Task<Guid?> ResolveTenantAsync(HttpContext context, WorkflowEngineDbContext dbContext)
    {
        // 1. Try from JWT claims (primary method)
        var orgIdClaim = context.User?.FindFirst("org_id")?.Value;
        if (Guid.TryParse(orgIdClaim, out var orgIdFromJwt))
        {
            return orgIdFromJwt;
        }

        // 2. Try from subdomain (e.g., tenant.workflowengine.com)
        var host = context.Request.Host.Host;
        if (host.Contains('.'))
        {
            var subdomain = host.Split('.')[0];
            if (subdomain != "www" && subdomain != "api")
            {
                var org = await dbContext.Organizations
                    .FirstOrDefaultAsync(o => o.Slug == subdomain && o.IsActive);
                if (org != null)
                {
                    return org.Id;
                }
            }
        }

        // 3. Try from custom domain
        var customDomainOrg = await dbContext.Organizations
            .FirstOrDefaultAsync(o => o.Domain == host && o.IsActive);
        if (customDomainOrg != null)
        {
            return customDomainOrg.Id;
        }

        // 4. Try from X-Tenant-ID header
        if (context.Request.Headers.TryGetValue("X-Tenant-ID", out var tenantHeader))
        {
            if (Guid.TryParse(tenantHeader.FirstOrDefault(), out var tenantFromHeader))
            {
                // Verify the tenant exists and user has access
                var userIdClaim = context.User?.FindFirst("user_id")?.Value;
                if (Guid.TryParse(userIdClaim, out var userId))
                {
                    var hasMembership = await dbContext.OrganizationMembers
                        .AnyAsync(m => m.UserId == userId &&
                                     m.OrganizationId == tenantFromHeader &&
                                     m.IsActive);
                    if (hasMembership)
                    {
                        return tenantFromHeader;
                    }
                }
            }
        }

        return null;
    }
}