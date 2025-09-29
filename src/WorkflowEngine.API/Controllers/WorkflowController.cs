using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkflowEngine.Application.Constants;
using WorkflowEngine.Application.DTOs.Workflow;
using WorkflowEngine.Application.Interfaces.Auth;
using WorkflowEngine.Application.Interfaces.Workflow;

namespace WorkflowEngine.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WorkflowController : ControllerBase
{
    private readonly IWorkflowService _workflowService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<WorkflowController> _logger;

    public WorkflowController(
        IWorkflowService workflowService,
        ICurrentUserService currentUserService,
        ILogger<WorkflowController> logger)
    {
        _workflowService = workflowService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// Get all workflows for current organization with filtering and pagination
    /// </summary>
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.OrganizationMember)]
    public async Task<IActionResult> GetWorkflows([FromQuery] GetWorkflowsRequest request)
    {
        try
        {
            var workflows = await _workflowService.GetWorkflowsAsync(request);

            return Ok(new
            {
                success = true,
                data = workflows
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting workflows for organization {OrganizationId}",
                _currentUserService.OrganizationId);
            return StatusCode(500, new { message = "An error occurred while retrieving workflows" });
        }
    }

    /// <summary>
    /// Get workflow by ID with permission check
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Policy = AuthorizationPolicies.OrganizationMember)]
    public async Task<IActionResult> GetWorkflow(Guid id)
    {
        try
        {
            var workflow = await _workflowService.GetWorkflowByIdAsync(id);

            if (workflow == null)
            {
                return NotFound(new { message = "Workflow not found" });
            }

            // Check if user can access this workflow
            var canAccess = await _currentUserService.CanAccessWorkflowAsync(id);
            if (!canAccess)
            {
                return StatusCode(403, new { success = false, message = "Access denied to this workflow" });
            }

            return Ok(new
            {
                success = true,
                data = workflow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting workflow {WorkflowId}", id);
            return StatusCode(500, new { message = "An error occurred while retrieving the workflow" });
        }
    }

    /// <summary>
    /// Create a new workflow
    /// </summary>
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.OrganizationMember)]
    public async Task<IActionResult> CreateWorkflow([FromBody] CreateWorkflowRequest request)
    {
        try
        {
            if (_currentUserService.OrganizationId == null)
            {
                return BadRequest(new { message = "No active organization found" });
            }

            var workflow = await _workflowService.CreateWorkflowAsync(request);

            if (workflow == null)
            {
                return BadRequest(new { message = "Failed to create workflow" });
            }

            _logger.LogInformation("User {UserId} created workflow {WorkflowId} in organization {OrganizationId}",
                _currentUserService.UserId, workflow.Id, _currentUserService.OrganizationId);

            return CreatedAtAction(
                nameof(GetWorkflow),
                new { id = workflow.Id },
                new { success = true, data = workflow });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating workflow for user {UserId}", _currentUserService.UserId);
            return StatusCode(500, new { message = "An error occurred while creating the workflow" });
        }
    }

    /// <summary>
    /// Update existing workflow
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Policy = AuthorizationPolicies.OrganizationMember)]
    public async Task<IActionResult> UpdateWorkflow(Guid id, [FromBody] UpdateWorkflowRequest request)
    {
        try
        {
            // Check if user can edit this workflow
            var canAccess = await _currentUserService.CanAccessWorkflowAsync(id);
            if (!canAccess)
            {
                return StatusCode(403, new { success = false, message = "Access denied to this workflow" });
            }

            var workflow = await _workflowService.UpdateWorkflowAsync(id, request);

            if (workflow == null)
            {
                return NotFound(new { message = "Workflow not found or access denied" });
            }

            _logger.LogInformation("User {UserId} updated workflow {WorkflowId}",
                _currentUserService.UserId, id);

            return Ok(new
            {
                success = true,
                message = "Workflow updated successfully",
                data = workflow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating workflow {WorkflowId}", id);
            return StatusCode(500, new { message = "An error occurred while updating the workflow" });
        }
    }

    /// <summary>
    /// Delete workflow
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Policy = AuthorizationPolicies.OrganizationMember)]
    public async Task<IActionResult> DeleteWorkflow(Guid id)
    {
        try
        {
            // Check if user can delete this workflow (must be owner or admin)
            var canAccess = await _currentUserService.CanAccessWorkflowAsync(id);
            if (!canAccess)
            {
                return StatusCode(403, new { success = false, message = "Access denied to this workflow" });
            }

            var success = await _workflowService.DeleteWorkflowAsync(id);

            if (!success)
            {
                return NotFound(new { message = "Workflow not found or access denied" });
            }

            _logger.LogInformation("User {UserId} deleted workflow {WorkflowId}",
                _currentUserService.UserId, id);

            return Ok(new { success = true, message = "Workflow deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting workflow {WorkflowId}", id);
            return StatusCode(500, new { message = "An error occurred while deleting the workflow" });
        }
    }

    /// <summary>
    /// Duplicate an existing workflow
    /// </summary>
    [HttpPost("{id}/duplicate")]
    [Authorize(Policy = AuthorizationPolicies.OrganizationMember)]
    public async Task<IActionResult> DuplicateWorkflow(Guid id, [FromBody] DuplicateWorkflowRequest request)
    {
        try
        {
            // Check if user can access the source workflow
            var canAccess = await _currentUserService.CanAccessWorkflowAsync(id);
            if (!canAccess)
            {
                return StatusCode(403, new { success = false, message = "Access denied to this workflow" });
            }

            var duplicatedWorkflow = await _workflowService.DuplicateWorkflowAsync(id, request);

            if (duplicatedWorkflow == null)
            {
                return NotFound(new { message = "Source workflow not found or access denied" });
            }

            _logger.LogInformation("User {UserId} duplicated workflow {SourceWorkflowId} to {NewWorkflowId}",
                _currentUserService.UserId, id, duplicatedWorkflow.Id);

            return CreatedAtAction(
                nameof(GetWorkflow),
                new { id = duplicatedWorkflow.Id },
                new { success = true, data = duplicatedWorkflow });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error duplicating workflow {WorkflowId}", id);
            return StatusCode(500, new { message = "An error occurred while duplicating the workflow" });
        }
    }

    /// <summary>
    /// Update workflow status (activate/deactivate)
    /// </summary>
    [HttpPatch("{id}/status")]
    [Authorize(Policy = AuthorizationPolicies.OrganizationMember)]
    public async Task<IActionResult> UpdateWorkflowStatus(Guid id, [FromBody] UpdateWorkflowStatusRequest request)
    {
        try
        {
            // Check if user can edit this workflow
            var canAccess = await _currentUserService.CanAccessWorkflowAsync(id);
            if (!canAccess)
            {
                return StatusCode(403, new { success = false, message = "Access denied to this workflow" });
            }

            var workflow = await _workflowService.UpdateWorkflowStatusAsync(id, request.Status);

            if (workflow == null)
            {
                return NotFound(new { message = "Workflow not found or access denied" });
            }

            _logger.LogInformation("User {UserId} changed workflow {WorkflowId} status to {Status}",
                _currentUserService.UserId, id, request.Status);

            return Ok(new
            {
                success = true,
                message = $"Workflow status updated to {request.Status}",
                data = workflow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating workflow {WorkflowId} status", id);
            return StatusCode(500, new { message = "An error occurred while updating workflow status" });
        }
    }

    /// <summary>
    /// Get workflow execution history
    /// </summary>
    [HttpGet("{id}/executions")]
    [Authorize(Policy = AuthorizationPolicies.OrganizationMember)]
    public async Task<IActionResult> GetWorkflowExecutions(Guid id, [FromQuery] GetExecutionsRequest request)
    {
        try
        {
            // Check if user can access this workflow
            var canAccess = await _currentUserService.CanAccessWorkflowAsync(id);
            if (!canAccess)
            {
                return StatusCode(403, new { success = false, message = "Access denied to this workflow" });
            }

            var executions = await _workflowService.GetWorkflowExecutionsAsync(id, request);

            return Ok(new
            {
                success = true,
                data = executions
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting executions for workflow {WorkflowId}", id);
            return StatusCode(500, new { message = "An error occurred while retrieving workflow executions" });
        }
    }

    /// <summary>
    /// Get workflow statistics
    /// </summary>
    [HttpGet("{id}/stats")]
    [Authorize(Policy = AuthorizationPolicies.OrganizationMember)]
    public async Task<IActionResult> GetWorkflowStatistics(Guid id)
    {
        try
        {
            // Check if user can access this workflow
            var canAccess = await _currentUserService.CanAccessWorkflowAsync(id);
            if (!canAccess)
            {
                return StatusCode(403, new { success = false, message = "Access denied to this workflow" });
            }

            var stats = await _workflowService.GetWorkflowStatisticsAsync(id);

            if (stats == null)
            {
                return NotFound(new { message = "Workflow not found" });
            }

            return Ok(new
            {
                success = true,
                data = stats
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting statistics for workflow {WorkflowId}", id);
            return StatusCode(500, new { message = "An error occurred while retrieving workflow statistics" });
        }
    }

    /// <summary>
    /// Validate workflow configuration
    /// </summary>
    [HttpPost("{id}/validate")]
    [Authorize(Policy = AuthorizationPolicies.OrganizationMember)]
    public async Task<IActionResult> ValidateWorkflow(Guid id)
    {
        try
        {
            // Check if user can access this workflow
            var canAccess = await _currentUserService.CanAccessWorkflowAsync(id);
            if (!canAccess)
            {
                return StatusCode(403, new { success = false, message = "Access denied to this workflow" });
            }

            var validationResult = await _workflowService.ValidateWorkflowAsync(id);

            return Ok(new
            {
                success = true,
                data = validationResult
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating workflow {WorkflowId}", id);
            return StatusCode(500, new { message = "An error occurred while validating the workflow" });
        }
    }
}