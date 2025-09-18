// File: src/WorkflowEngine.Infrastructure/Services/WorkflowService.cs (Complete Final Version)
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using WorkflowEngine.Application.DTOs.Common;
using WorkflowEngine.Application.DTOs.Execution;
using WorkflowEngine.Application.DTOs.Workflow;
using WorkflowEngine.Application.Exceptions;
using WorkflowEngine.Application.Interfaces.Auth;
using WorkflowEngine.Application.Interfaces.Workflow;
using WorkflowEngine.Core.Entities;
using WorkflowEngine.Core.Enums;
using WorkflowEngine.Infrastructure.Data;

namespace WorkflowEngine.Infrastructure.Services;

public class WorkflowService : IWorkflowService
{
    private readonly WorkflowEngineDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<WorkflowService> _logger;

    public WorkflowService(
        WorkflowEngineDbContext context,
        ICurrentUserService currentUserService,
        ILogger<WorkflowService> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    #region Core CRUD Operations

    public async Task<WorkflowListResponse> GetWorkflowsAsync(GetWorkflowsRequest request)
    {
        var organizationId = _currentUserService.OrganizationId;
        if (organizationId == null)
            throw new InvalidOperationException("No active organization found");

        var query = _context.Workflows
            .Where(w => w.OrganizationId == organizationId.Value)
            .Include(w => w.Creator)
            .Include(w => w.Organization)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrEmpty(request.Search))
        {
            var searchLower = request.Search.ToLower();
            query = query.Where(w => w.Name.ToLower().Contains(searchLower) ||
                                   (w.Description != null && w.Description.ToLower().Contains(searchLower)));
        }

        if (request.Status.HasValue)
            query = query.Where(w => w.Status == request.Status.Value);

        if (request.Visibility.HasValue)
            query = query.Where(w => w.Visibility == request.Visibility.Value);

        if (request.IsTemplate.HasValue)
            query = query.Where(w => w.IsTemplate == request.IsTemplate.Value);

        if (request.CreatedBy.HasValue)
            query = query.Where(w => w.CreatedBy == request.CreatedBy.Value);

        // Date filters
        if (request.CreatedAfter.HasValue)
            query = query.Where(w => w.CreatedAt >= request.CreatedAfter.Value);

        if (request.CreatedBefore.HasValue)
            query = query.Where(w => w.CreatedAt <= request.CreatedBefore.Value);

        if (request.ModifiedAfter.HasValue)
            query = query.Where(w => w.LastModified >= request.ModifiedAfter.Value);

        if (request.ModifiedBefore.HasValue)
            query = query.Where(w => w.LastModified <= request.ModifiedBefore.Value);


        // Apply sorting
        query = ApplySorting(query, request.SortBy, request.SortOrder);

        // Get total count before pagination
        var totalCount = await query.CountAsync();

        // Apply pagination
        var workflows = await query
            .Skip((request.Page - 1) * request.Limit)
            .Take(request.Limit)
            .Select(w => new WorkflowSummaryDto
            {
                Id = w.Id,
                Name = w.Name,
                Description = w.Description,
                Status = w.Status,
                Visibility = w.Visibility,
                IsTemplate = w.IsTemplate,
                CreatedAt = w.CreatedAt,
                LastModified = w.LastModified,
                CreatorName = w.Creator != null ? $"{w.Creator.FirstName} {w.Creator.LastName}".Trim() : null,
                NodeCount = w.NodesJson != null ? JsonSerializer.Deserialize<List<WorkflowNodeDto>>(w.NodesJson)!.Count : 0,
                ExecutionCount = w.Executions.Count(),
                LastExecutedAt = w.Executions.OrderByDescending(e => e.StartedAt).FirstOrDefault()!.StartedAt,
                LastExecutionStatus = w.Executions.OrderByDescending(e => e.StartedAt).FirstOrDefault()!.Status
            })
            .ToListAsync();

        return new WorkflowListResponse
        {
            Workflows = workflows,
            Pagination = new PaginationDto
            {
                Page = request.Page,
                Limit = request.Limit,
                Total = totalCount,
                TotalPages = (int)Math.Ceiling((double)totalCount / request.Limit),
                HasNext = request.Page * request.Limit < totalCount,
                HasPrevious = request.Page > 1
            }
        };
    }

    public async Task<WorkflowResponse?> GetWorkflowByIdAsync(Guid id)
    {
        var workflow = await _context.Workflows
            .Include(w => w.Creator)
            .Include(w => w.Organization)
            .FirstOrDefaultAsync(w => w.Id == id);

        if (workflow == null)
            return null;

        // Check access permissions using existing service
        if (!await _currentUserService.CanAccessWorkflowAsync(id))
            return null;

        // Get execution statistics
        var executions = await _context.WorkflowExecutions
            .Where(e => e.WorkflowId == id)
            .ToListAsync();

        return new WorkflowResponse
        {
            Id = workflow.Id,
            Name = workflow.Name,
            Description = workflow.Description,
            Status = workflow.Status,
            Visibility = workflow.Visibility,
            IsTemplate = workflow.IsTemplate,
            Version = workflow.Version,
            CreatedAt = workflow.CreatedAt,
            LastModified = workflow.LastModified,
            CreatedBy = workflow.CreatedBy,
            CreatorName = workflow.Creator != null ? $"{workflow.Creator.FirstName} {workflow.Creator.LastName}".Trim() : null,
            CreatorEmail = workflow.Creator?.Email,
            OrganizationId = workflow.OrganizationId,
            OrganizationName = workflow.Organization?.Name,
            Nodes = workflow.NodesJson != null ? JsonSerializer.Deserialize<List<WorkflowNodeDto>>(workflow.NodesJson) : null,
            Connections = workflow.ConnectionsJson != null ? JsonSerializer.Deserialize<List<NodeConnectionDto>>(workflow.ConnectionsJson) : null,
            Settings = workflow.SettingsJson != null ? JsonSerializer.Deserialize<Dictionary<string, object>>(workflow.SettingsJson) : null,
            ExecutionCount = executions.Count,
            LastExecutedAt = executions.OrderByDescending(e => e.StartedAt).FirstOrDefault()?.StartedAt,
            AverageExecutionTime = executions.Where(e => e.Duration.HasValue).Average(e => e.Duration!.Value.TotalMilliseconds),
            SuccessRate = executions.Count > 0 ?
                (double)executions.Count(e => e.Status == ExecutionStatus.Completed) / executions.Count * 100 : 0
        };
    }

    public async Task<WorkflowResponse?> CreateWorkflowAsync(CreateWorkflowRequest request)
    {
        var organizationId = _currentUserService.OrganizationId;
        var userId = _currentUserService.UserId;

        if (organizationId == null || userId == null)
            throw new InvalidOperationException("No active organization or user found");

        var workflow = new Workflow
        {
            Name = request.Name,
            Description = request.Description,
            Status = WorkflowStatus.Draft,
            Visibility = request.Visibility,
            IsTemplate = request.IsTemplate,
            Version = 1,
            CreatedBy = userId.Value,
            CreatedAt = DateTime.UtcNow,
            //UpdatedAt = DateTime.UtcNow, 
            OrganizationId = organizationId.Value,
            NodesJson = request.Nodes != null ? JsonSerializer.Serialize(request.Nodes) : null,
            ConnectionsJson = request.Connections != null ? JsonSerializer.Serialize(request.Connections) : null,
            SettingsJson = request.Settings != null ? JsonSerializer.Serialize(request.Settings) : null
        };

        _context.Workflows.Add(workflow);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created workflow {WorkflowId} for user {UserId}", workflow.Id, userId);

        return await GetWorkflowByIdAsync(workflow.Id);
    }

    public async Task<WorkflowResponse?> UpdateWorkflowAsync(Guid id, UpdateWorkflowRequest request)
    {
        var workflow = await _context.Workflows.FirstOrDefaultAsync(w => w.Id == id);
        if (workflow == null)
            return null;

        var userId = _currentUserService.UserId ?? Guid.Empty;
        if (!await CanUserEditWorkflowAsync(id, userId))
            return null;

        // Update properties if provided
        if (!string.IsNullOrEmpty(request.Name))
            workflow.Name = request.Name;

        if (request.Description != null)
            workflow.Description = request.Description;

        if (request.Visibility.HasValue)
            workflow.Visibility = request.Visibility.Value;

        if (request.IsTemplate.HasValue)
            workflow.IsTemplate = request.IsTemplate.Value;

        if (request.Nodes != null)
            workflow.NodesJson = JsonSerializer.Serialize(request.Nodes);

        if (request.Connections != null)
            workflow.ConnectionsJson = JsonSerializer.Serialize(request.Connections);

        if (request.Settings != null)
            workflow.SettingsJson = JsonSerializer.Serialize(request.Settings);

        // workflow.UpdatedAt = DateTime.UtcNow; TO FIX
        workflow.Version++;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated workflow {WorkflowId} by user {UserId}", id, userId);

        return await GetWorkflowByIdAsync(id);
    }

    public async Task<bool> DeleteWorkflowAsync(Guid id)
    {
        var workflow = await _context.Workflows.FirstOrDefaultAsync(w => w.Id == id);
        if (workflow == null)
            return false;

        var userId = _currentUserService.UserId ?? Guid.Empty;
        if (!await CanUserDeleteWorkflowAsync(id, userId))
            return false;

        _context.Workflows.Remove(workflow);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted workflow {WorkflowId} by user {UserId}", id, userId);

        return true;
    }

    #endregion

    #region Workflow Operations

    public async Task<WorkflowResponse?> DuplicateWorkflowAsync(Guid sourceId, DuplicateWorkflowRequest request)
    {
        var sourceWorkflow = await _context.Workflows.FirstOrDefaultAsync(w => w.Id == sourceId);
        if (sourceWorkflow == null)
            return null;

        if (!await _currentUserService.CanAccessWorkflowAsync(sourceId))
            return null;

        var organizationId = _currentUserService.OrganizationId;
        var userId = _currentUserService.UserId;

        if (organizationId == null || userId == null)
            throw new InvalidOperationException("No active organization or user found");

        var duplicatedWorkflow = new Workflow
        {
            Name = request.Name,
            Description = request.Description,
            Status = WorkflowStatus.Draft,
            Visibility = request.Visibility,
            IsTemplate = request.IsTemplate,
            Version = 1,
            CreatedBy = userId.Value,
            CreatedAt = DateTime.UtcNow,
            LastModified = DateTime.UtcNow,
            OrganizationId = organizationId.Value,
            NodesJson = sourceWorkflow.NodesJson,
            ConnectionsJson = sourceWorkflow.ConnectionsJson,
            SettingsJson = sourceWorkflow.SettingsJson
        };

        _context.Workflows.Add(duplicatedWorkflow);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Duplicated workflow {SourceId} to {NewId} by user {UserId}",
            sourceId, duplicatedWorkflow.Id, userId);

        return await GetWorkflowByIdAsync(duplicatedWorkflow.Id);
    }

    public async Task<WorkflowResponse?> UpdateWorkflowStatusAsync(Guid id, WorkflowStatus status)
    {
        var workflow = await _context.Workflows.FirstOrDefaultAsync(w => w.Id == id);
        if (workflow == null)
            return null;

        var userId = _currentUserService.UserId ?? Guid.Empty;
        if (!await CanUserEditWorkflowAsync(id, userId))
            return null;

        // Validate workflow before activation
        if (status == WorkflowStatus.Active)
        {
            var validationResult = await ValidateWorkflowAsync(id);
            if (!validationResult.IsValid)
            {
                throw new WorkflowValidationException(validationResult.Errors);
            }
        }

        workflow.Status = status;
        workflow.LastModified = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated workflow {WorkflowId} status to {Status} by user {UserId}",
            id, status, userId);

        return await GetWorkflowByIdAsync(id);
    }

    #endregion

    #region Validation and Testing

    public async Task<WorkflowValidationResult> ValidateWorkflowAsync(Guid id)
    {
        var workflow = await _context.Workflows.FirstOrDefaultAsync(w => w.Id == id);
        if (workflow == null)
            throw new WorkflowNotFoundException(id);

        var nodes = workflow.NodesJson != null ?
            JsonSerializer.Deserialize<List<WorkflowNodeDto>>(workflow.NodesJson) : new List<WorkflowNodeDto>();

        var connections = workflow.ConnectionsJson != null ?
            JsonSerializer.Deserialize<List<NodeConnectionDto>>(workflow.ConnectionsJson) : new List<NodeConnectionDto>();

        return await ValidateWorkflowDataAsync(nodes, connections);
    }

    public async Task<WorkflowValidationResult> ValidateWorkflowDataAsync(List<WorkflowNodeDto> nodes, List<NodeConnectionDto> connections)
    {
        var result = new WorkflowValidationResult { IsValid = true };

        // Check for empty workflow
        if (!nodes.Any())
        {
            result.Errors.Add(new ValidationError
            {
                Code = "EMPTY_WORKFLOW",
                Message = "Workflow must contain at least one node"
            });
        }

        // Check for trigger nodes
        var triggerNodes = nodes.Where(n => IsTriggerNode(n.Type)).ToList();
        if (!triggerNodes.Any())
        {
            result.Errors.Add(new ValidationError
            {
                Code = "NO_TRIGGER",
                Message = "Workflow must have at least one trigger node"
            });
        }

        // Check for disconnected nodes
        var connectedNodeIds = connections
            .SelectMany(c => new[] { c.SourceNodeId, c.TargetNodeId })
            .Distinct()
            .ToHashSet();

        var disconnectedNodes = nodes
            .Where(n => !IsTriggerNode(n.Type) && !connectedNodeIds.Contains(n.Id))
            .ToList();

        foreach (var node in disconnectedNodes)
        {
            result.Warnings.Add(new ValidationWarning
            {
                Code = "DISCONNECTED_NODE",
                Message = $"Node '{node.Name}' is not connected to any other node",
                NodeId = node.Id
            });
        }

        // Check for circular dependencies
        if (HasCircularDependency(nodes, connections))
        {
            result.Errors.Add(new ValidationError
            {
                Code = "CIRCULAR_DEPENDENCY",
                Message = "Workflow contains circular dependencies"
            });
        }

        // Validate individual nodes
        foreach (var node in nodes)
        {
            var nodeValidation = await ValidateNodeAsync(node);
            result.Errors.AddRange(nodeValidation.Errors);
            result.Warnings.AddRange(nodeValidation.Warnings);
        }

        result.IsValid = !result.Errors.Any();
        return result;
    }

    #endregion

    #region Statistics and Analytics

    public async Task<WorkflowStatistics?> GetWorkflowStatisticsAsync(Guid id)
    {
        var workflow = await _context.Workflows
            .Include(w => w.Executions)
            .FirstOrDefaultAsync(w => w.Id == id);

        if (workflow == null)
            return null;

        if (!await _currentUserService.CanAccessWorkflowAsync(id))
            return null;

        var executions = workflow.Executions.ToList();
        var now = DateTime.UtcNow;

        var stats = new WorkflowStatistics
        {
            WorkflowId = id,
            WorkflowName = workflow.Name,
            TotalExecutions = executions.Count,
            SuccessfulExecutions = executions.Count(e => e.Status == ExecutionStatus.Completed),
            FailedExecutions = executions.Count(e => e.Status == ExecutionStatus.Failed),
            LastExecutedAt = executions.OrderByDescending(e => e.StartedAt).FirstOrDefault()?.StartedAt,
            FirstExecutedAt = executions.OrderBy(e => e.StartedAt).FirstOrDefault()?.StartedAt,
            ExecutionsLast24Hours = executions.Count(e => e.StartedAt >= now.AddHours(-24)),
            ExecutionsLast7Days = executions.Count(e => e.StartedAt >= now.AddDays(-7)),
            ExecutionsLast30Days = executions.Count(e => e.StartedAt >= now.AddDays(-30))
        };

        if (stats.TotalExecutions > 0)
        {
            stats.SuccessRate = (double)stats.SuccessfulExecutions / stats.TotalExecutions * 100;

            var completedExecutions = executions.Where(e => e.Duration.HasValue).ToList();
            if (completedExecutions.Any())
            {
                var durations = completedExecutions.Select(e => e.Duration!.Value.TotalMilliseconds).ToList();
                stats.AverageExecutionTimeMs = durations.Average();
                stats.MinExecutionTimeMs = durations.Min();
                stats.MaxExecutionTimeMs = durations.Max();

                // Calculate median
                var sortedDurations = durations.OrderBy(d => d).ToList();
                var count = sortedDurations.Count;
                stats.MedianExecutionTimeMs = count % 2 == 0
                    ? (sortedDurations[count / 2 - 1] + sortedDurations[count / 2]) / 2
                    : sortedDurations[count / 2];
            }
        }

        return stats;
    }

    public async Task<ExecutionListResponse> GetWorkflowExecutionsAsync(Guid id, GetExecutionsRequest request)
    {
        if (!await _currentUserService.CanAccessWorkflowAsync(id))
            throw new WorkflowAccessDeniedException(id);

        var query = _context.WorkflowExecutions
            .Where(e => e.WorkflowId == id)
            .Include(e => e.Workflow)
            .Include(e => e.User)
            .AsQueryable();

        // Apply filters
        if (request.Status.HasValue)
            query = query.Where(e => e.Status == request.Status.Value);

        if (request.TriggerType.HasValue)
            query = query.Where(e => e.TriggerType == request.TriggerType.Value);

        // Date filters
        if (request.StartedAfter.HasValue)
            query = query.Where(e => e.StartedAt >= request.StartedAfter.Value);

        if (request.StartedBefore.HasValue)
            query = query.Where(e => e.StartedAt <= request.StartedBefore.Value);

        if (request.CompletedAfter.HasValue)
            query = query.Where(e => e.CompletedAt >= request.CompletedAfter.Value);

        if (request.CompletedBefore.HasValue)
            query = query.Where(e => e.CompletedAt <= request.CompletedBefore.Value);

        // Apply sorting
        query = request.SortBy.ToLower() switch
        {
            "startedat" => request.SortOrder.ToLower() == "asc"
                ? query.OrderBy(e => e.StartedAt)
                : query.OrderByDescending(e => e.StartedAt),
            "completedat" => request.SortOrder.ToLower() == "asc"
                ? query.OrderBy(e => e.CompletedAt)
                : query.OrderByDescending(e => e.CompletedAt),
            "status" => request.SortOrder.ToLower() == "asc"
                ? query.OrderBy(e => e.Status)
                : query.OrderByDescending(e => e.Status),
            "duration" => request.SortOrder.ToLower() == "asc"
                ? query.OrderBy(e => e.Duration)
                : query.OrderByDescending(e => e.Duration),
            _ => query.OrderByDescending(e => e.StartedAt)
        };

        // Get total count
        var totalCount = await query.CountAsync();

        // Apply pagination and select
        var executions = await query
            .Skip((request.Page - 1) * request.Limit)
            .Take(request.Limit)
            .Select(e => new ExecutionSummaryDto
            {
                Id = e.Id,
                WorkflowId = e.WorkflowId,
                WorkflowName = e.Workflow.Name,
                Status = e.Status,
                TriggerType = e.TriggerType,
                StartedAt = e.StartedAt,
                CompletedAt = e.CompletedAt,
                DurationMs = e.Duration.HasValue ? (int)e.Duration.Value.TotalMilliseconds : null,
                ErrorMessage = e.ErrorDataJson,
                ExecutedBy = e.UserId,
                ExecutorName = e.User != null ? $"{e.User.FirstName} {e.User.LastName}".Trim() : null
            })
            .ToListAsync();

        return new ExecutionListResponse
        {
            Executions = executions,
            Pagination = new PaginationDto
            {
                Page = request.Page,
                Limit = request.Limit,
                Total = totalCount,
                TotalPages = (int)Math.Ceiling((double)totalCount / request.Limit),
                HasNext = request.Page * request.Limit < totalCount,
                HasPrevious = request.Page > 1
            }
        };
    }

    #endregion

    #region Permissions and Access Control

    public async Task<bool> CanUserAccessWorkflowAsync(Guid workflowId, Guid userId)
    {
        // Use the existing method from CurrentUserService
        return await _currentUserService.CanAccessWorkflowAsync(workflowId);
    }

    public async Task<bool> CanUserEditWorkflowAsync(Guid workflowId, Guid userId)
    {
        var workflow = await _context.Workflows.FirstOrDefaultAsync(w => w.Id == workflowId);
        if (workflow == null)
            return false;

        // Owner can always edit
        if (workflow.CreatedBy == userId)
            return true;

        // Check if user has edit permission through organization role
        return await _currentUserService.HasPermissionAsync("edit_workflows");
    }

    public async Task<bool> CanUserDeleteWorkflowAsync(Guid workflowId, Guid userId)
    {
        var workflow = await _context.Workflows.FirstOrDefaultAsync(w => w.Id == workflowId);
        if (workflow == null)
            return false;

        // Owner can always delete
        if (workflow.CreatedBy == userId)
            return true;

        // Check if user has delete permission through organization role
        return await _currentUserService.HasPermissionAsync("delete_workflows");
    }

    #endregion

    #region Templates and Sharing

    public async Task<WorkflowListResponse> GetWorkflowTemplatesAsync(GetWorkflowsRequest request)
    {
        var organizationId = _currentUserService.OrganizationId;
        if (organizationId == null)
            throw new InvalidOperationException("No active organization found");

        var query = _context.Workflows
            .Where(w => w.IsTemplate &&
                       (w.Visibility == WorkflowVisibility.Public ||
                        w.OrganizationId == organizationId.Value))
            .Include(w => w.Creator)
            .Include(w => w.Organization)
            .AsQueryable();

        // Apply similar filters as GetWorkflowsAsync
        if (!string.IsNullOrEmpty(request.Search))
        {
            var searchLower = request.Search.ToLower();
            query = query.Where(w => w.Name.ToLower().Contains(searchLower) ||
                                   (w.Description != null && w.Description.ToLower().Contains(searchLower)));
        }

        if (request.Status.HasValue)
            query = query.Where(w => w.Status == request.Status.Value);

        // Apply sorting
        query = ApplySorting(query, request.SortBy, request.SortOrder);

        var totalCount = await query.CountAsync();

        var templates = await query
            .Skip((request.Page - 1) * request.Limit)
            .Take(request.Limit)
            .Select(w => new WorkflowSummaryDto
            {
                Id = w.Id,
                Name = w.Name,
                Description = w.Description,
                Status = w.Status,
                Visibility = w.Visibility,
                IsTemplate = w.IsTemplate,
                CreatedAt = w.CreatedAt,
                LastModified = w.LastModified,
                CreatorName = w.Creator != null ? $"{w.Creator.FirstName} {w.Creator.LastName}".Trim() : null,
                NodeCount = w.NodesJson != null ? JsonSerializer.Deserialize<List<WorkflowNodeDto>>(w.NodesJson)!.Count : 0,
                ExecutionCount = 0, // Templates don't have executions
                LastExecutedAt = null,
                LastExecutionStatus = null
            })
            .ToListAsync();

        return new WorkflowListResponse
        {
            Workflows = templates,
            Pagination = new PaginationDto
            {
                Page = request.Page,
                Limit = request.Limit,
                Total = totalCount,
                TotalPages = (int)Math.Ceiling((double)totalCount / request.Limit),
                HasNext = request.Page * request.Limit < totalCount,
                HasPrevious = request.Page > 1
            }
        };
    }

    public async Task<WorkflowResponse?> CreateFromTemplateAsync(Guid templateId, CreateWorkflowRequest request)
    {
        var template = await _context.Workflows.FirstOrDefaultAsync(w => w.Id == templateId && w.IsTemplate);
        if (template == null)
            return null;

        if (!await _currentUserService.CanAccessWorkflowAsync(templateId))
            return null;

        var organizationId = _currentUserService.OrganizationId;
        var userId = _currentUserService.UserId;

        if (organizationId == null || userId == null)
            throw new InvalidOperationException("No active organization or user found");

        var workflow = new Workflow
        {
            Name = request.Name,
            Description = request.Description,
            Status = WorkflowStatus.Draft,
            Visibility = request.Visibility,
            IsTemplate = false, // Created from template is not a template itself
            Version = 1,
            CreatedBy = userId.Value,
            CreatedAt = DateTime.UtcNow,
            LastModified = DateTime.UtcNow,
            OrganizationId = organizationId.Value,
            NodesJson = template.NodesJson,
            ConnectionsJson = template.ConnectionsJson,
            SettingsJson = template.SettingsJson
        };

        _context.Workflows.Add(workflow);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created workflow {WorkflowId} from template {TemplateId} by user {UserId}",
            workflow.Id, templateId, userId);

        return await GetWorkflowByIdAsync(workflow.Id);
    }

    #endregion

    #region Versioning (Future Implementation)

    public async Task<List<WorkflowVersionDto>> GetWorkflowVersionsAsync(Guid id)
    {
        var workflow = await _context.Workflows.FirstOrDefaultAsync(w => w.Id == id);

        if (workflow == null || !await _currentUserService.CanAccessWorkflowAsync(id))
            return new List<WorkflowVersionDto>();

        return new List<WorkflowVersionDto>
        {
            new()
            {
                Id = workflow.Id,
                WorkflowId = workflow.Id,
                VersionNumber = workflow.Version,
                Name = workflow.Name,
                Description = workflow.Description,
                CreatedAt = workflow.CreatedAt,
                UpdatedAt = workflow.LastModified ?? workflow.CreatedAt,
                CreatedBy = workflow.CreatedBy,
                Nodes = workflow.NodesJson != null ? JsonSerializer.Deserialize<List<WorkflowNodeDto>>(workflow.NodesJson)! : new(),
                Connections = workflow.ConnectionsJson != null ? JsonSerializer.Deserialize<List<NodeConnectionDto>>(workflow.ConnectionsJson)! : new(),
                Settings = workflow.SettingsJson != null ? JsonSerializer.Deserialize<Dictionary<string, object>>(workflow.SettingsJson)! : new()
            }
        };
    }

    public async Task<WorkflowResponse?> CreateWorkflowVersionAsync(Guid id, string? versionNote = null)
    {
        // Future implementation: Create a new version entry in WorkflowVersions table
        throw new NotImplementedException("Workflow versioning will be implemented in a future release");
    }

    public async Task<WorkflowResponse?> RestoreWorkflowVersionAsync(Guid id, int version)
    {
        // Future implementation: Restore workflow from a specific version
        throw new NotImplementedException("Workflow versioning will be implemented in a future release");
    }

    #endregion

    #region Private Helper Methods

    private static IQueryable<Workflow> ApplySorting(IQueryable<Workflow> query, string sortBy, string sortOrder)
    {
        var isAscending = sortOrder.ToLower() == "asc";
        return sortBy.ToLower() switch
        {
            "name" => isAscending ? query.OrderBy(w => w.Name) : query.OrderByDescending(w => w.Name),
            "createdat" => isAscending ? query.OrderBy(w => w.CreatedAt) : query.OrderByDescending(w => w.CreatedAt),
            "updatedat" => isAscending ? query.OrderBy(w => w.LastModified) : query.OrderByDescending(w => w.LastModified),
            "lastmodified" => isAscending ? query.OrderBy(w => w.LastModified) : query.OrderByDescending(w => w.LastModified),
            "status" => isAscending ? query.OrderBy(w => w.Status) : query.OrderByDescending(w => w.Status),
            _ => query.OrderByDescending(w => w.LastModified ?? w.CreatedAt)
        };
    }

    private static bool IsTriggerNode(string nodeType)
    {
        var triggerTypes = new[] { "manual_trigger", "webhook", "schedule", "file_watcher", "http_trigger", "timer_trigger" };
        return triggerTypes.Contains(nodeType.ToLower());
    }

    private static bool HasCircularDependency(List<WorkflowNodeDto> nodes, List<NodeConnectionDto> connections)
    {
        var adjacencyList = new Dictionary<string, List<string>>();

        // Build adjacency list
        foreach (var node in nodes)
        {
            adjacencyList[node.Id] = new List<string>();
        }

        foreach (var connection in connections)
        {
            if (adjacencyList.ContainsKey(connection.SourceNodeId))
            {
                adjacencyList[connection.SourceNodeId].Add(connection.TargetNodeId);
            }
        }

        // Use DFS to detect cycles
        var visited = new HashSet<string>();
        var recursionStack = new HashSet<string>();

        foreach (var nodeId in adjacencyList.Keys)
        {
            if (!visited.Contains(nodeId))
            {
                if (HasCycleDFS(nodeId, adjacencyList, visited, recursionStack))
                    return true;
            }
        }

        return false;
    }

    private static bool HasCycleDFS(string nodeId, Dictionary<string, List<string>> adjacencyList,
        HashSet<string> visited, HashSet<string> recursionStack)
    {
        visited.Add(nodeId);
        recursionStack.Add(nodeId);

        foreach (var neighbor in adjacencyList.GetValueOrDefault(nodeId, new List<string>()))
        {
            if (!visited.Contains(neighbor))
            {
                if (HasCycleDFS(neighbor, adjacencyList, visited, recursionStack))
                    return true;
            }
            else if (recursionStack.Contains(neighbor))
            {
                return true;
            }
        }

        recursionStack.Remove(nodeId);
        return false;
    }

    private async Task<WorkflowValidationResult> ValidateNodeAsync(WorkflowNodeDto node)
    {
        var result = new WorkflowValidationResult { IsValid = true };

        // Basic node validation
        if (string.IsNullOrEmpty(node.Name))
        {
            result.Errors.Add(new ValidationError
            {
                Code = "NODE_NAME_REQUIRED",
                Message = "Node name is required",
                NodeId = node.Id,
                Field = "name"
            });
        }

        if (string.IsNullOrEmpty(node.Type))
        {
            result.Errors.Add(new ValidationError
            {
                Code = "NODE_TYPE_REQUIRED",
                Message = "Node type is required",
                NodeId = node.Id,
                Field = "type"
            });
        }

        // Position validation
        if (node.Position.X < 0 || node.Position.Y < 0)
        {
            result.Warnings.Add(new ValidationWarning
            {
                Code = "NODE_NEGATIVE_POSITION",
                Message = "Node has negative position coordinates",
                NodeId = node.Id
            });
        }

        // TODO: Add node-specific validation based on node type
        // This would involve:
        // - Checking required parameters for each node type
        // - Validating parameter data types and formats
        // - Checking credential references exist and are valid
        // - Validating expressions and formulas
        // - Checking connection compatibility

        result.IsValid = !result.Errors.Any();
        return result;
    }


    #endregion
}