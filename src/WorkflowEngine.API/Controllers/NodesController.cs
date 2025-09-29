using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkflowEngine.Nodes.Interfaces;
using WorkflowEngine.Nodes.Models;

namespace WorkflowEngine.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NodesController : ControllerBase
{
    private readonly INodeRegistry _nodeRegistry;
    private readonly ILogger<NodesController> _logger;

    public NodesController(INodeRegistry nodeRegistry, ILogger<NodesController> logger)
    {
        _nodeRegistry = nodeRegistry;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult GetAllNodes([FromQuery] string? category = null, [FromQuery] string? search = null)
    {
        var nodes = _nodeRegistry.GetAllNodeDefinitions();

        if (!string.IsNullOrEmpty(category))
        {
            nodes = nodes.Where(n => n.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrEmpty(search))
        {
            nodes = nodes.Where(n =>
                n.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (n.Description?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        return Ok(new
        {
            success = true,
            data = new { nodes = nodes.ToList() }
        });
    }

    [HttpGet("{nodeType}")]
    public IActionResult GetNodeDefinition(string nodeType)
    {
        try
        {
            var definition = _nodeRegistry.GetNodeDefinition(nodeType);
            return Ok(new
            {
                success = true,
                data = new { node = definition }
            });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new
            {
                success = false,
                error = new { message = ex.Message }
            });
        }
    }

    [HttpPost("{nodeType}/test")]
    public async Task<IActionResult> TestNode(
        string nodeType,
        [FromBody] TestNodeRequest request)
    {
        try
        {
            var node = _nodeRegistry.CreateNode(nodeType);

            var context = new NodeExecutionContext
            {
                ExecutionId = Guid.NewGuid(),
                NodeId = "test-node",
                UserId = GetCurrentUserId(),
                Parameters = request.Parameters ?? new(),
                InputData = request.InputData ?? new(),
                WorkflowContext = request.WorkflowContext ?? new(),
                Services = HttpContext.RequestServices,
                CancellationToken = HttpContext.RequestAborted
            };

            var result = await node.ExecuteAsync(context);

            return Ok(new
            {
                success = result.Success,
                data = result.Success ? new { output = result.OutputData } : null,
                error = result.Success ? null : new { message = result.ErrorMessage }
            });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new
            {
                success = false,
                error = new { message = ex.Message }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing node {NodeType}", nodeType);
            return StatusCode(500, new
            {
                success = false,
                error = new { message = "Internal server error" }
            });
        }
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("sub") ?? User.FindFirst("userId");
        return userIdClaim != null ? Guid.Parse(userIdClaim.Value) : Guid.Empty;
    }
}

public class TestNodeRequest
{
    public Dictionary<string, object>? Parameters { get; set; }
    public Dictionary<string, object>? InputData { get; set; }
    public Dictionary<string, object>? WorkflowContext { get; set; }
}