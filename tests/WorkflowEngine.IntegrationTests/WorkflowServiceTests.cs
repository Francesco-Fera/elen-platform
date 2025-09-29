// File: tests/WorkflowEngine.IntegrationTests/WorkflowServiceTests.cs
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Text.Json;
using WorkflowEngine.Application.DTOs.Workflow;
using WorkflowEngine.Application.Exceptions;
using WorkflowEngine.Application.Interfaces.Auth;
using WorkflowEngine.Core.Entities;
using WorkflowEngine.Core.Enums;
using WorkflowEngine.Infrastructure.Data;
using WorkflowEngine.Infrastructure.Services;

namespace WorkflowEngine.IntegrationTests;

public class WorkflowServiceTests : IDisposable
{
    private readonly WorkflowEngineDbContext _context;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly WorkflowService _workflowService;
    private readonly Guid _testUserId;
    private readonly Guid _testOrgId;
    private readonly User _testUser;
    private readonly Organization _testOrg;

    public WorkflowServiceTests()
    {
        var options = new DbContextOptionsBuilder<WorkflowEngineDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new WorkflowEngineDbContext(options);
        _context.Database.EnsureCreated();

        _testUserId = Guid.NewGuid();
        _testOrgId = Guid.NewGuid();

        // Setup test user and organization
        _testUser = new User
        {
            Id = _testUserId,
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            IsActive = true,
            CurrentOrganizationId = _testOrgId
        };

        _testOrg = new Organization
        {
            Id = _testOrgId,
            Name = "Test Organization",
            IsActive = true,
            MaxUsers = 10
        };

        _context.Users.Add(_testUser);
        _context.Organizations.Add(_testOrg);

        var membership = new OrganizationMember
        {
            UserId = _testUserId,
            OrganizationId = _testOrgId,
            Role = OrganizationRole.Owner,
            IsActive = true
        };
        _context.OrganizationMembers.Add(membership);
        _context.SaveChanges();

        // Setup mock CurrentUserService
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockCurrentUserService.Setup(x => x.UserId).Returns(_testUserId);
        _mockCurrentUserService.Setup(x => x.OrganizationId).Returns(_testOrgId);
        _mockCurrentUserService.Setup(x => x.Email).Returns("test@example.com");
        _mockCurrentUserService.Setup(x => x.OrganizationRole).Returns(OrganizationRole.Owner);
        _mockCurrentUserService.Setup(x => x.CanAccessWorkflowAsync(It.IsAny<Guid>()))
            .ReturnsAsync(true);
        _mockCurrentUserService.Setup(x => x.HasPermissionAsync(It.IsAny<string>()))
            .ReturnsAsync(true);

        _workflowService = new WorkflowService(
            _context,
            _mockCurrentUserService.Object,
            Mock.Of<Microsoft.Extensions.Logging.ILogger<WorkflowService>>()
        );
    }

    #region Create Workflow Tests

    [Fact]
    public async Task CreateWorkflowAsync_ShouldCreateWorkflow_WhenValidRequest()
    {
        // Arrange
        var request = new CreateWorkflowRequest
        {
            Name = "Test Workflow",
            Description = "Test Description",
            Visibility = WorkflowVisibility.Private,
            IsTemplate = false
        };

        // Act
        var result = await _workflowService.CreateWorkflowAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.Name, result.Name);
        Assert.Equal(request.Description, result.Description);
        Assert.Equal(WorkflowStatus.Draft, result.Status);
        Assert.Equal(_testUserId, result.CreatedBy);
        Assert.Equal(_testOrgId, result.OrganizationId);
        Assert.Equal(0, result.ExecutionCount); // No executions yet
        Assert.Null(result.LastExecutedAt);

        // Verify in database
        var workflow = await _context.Workflows.FirstOrDefaultAsync(w => w.Id == result.Id);
        Assert.NotNull(workflow);
        Assert.Equal(1, workflow.Version);
    }

    [Fact]
    public async Task CreateWorkflowAsync_ShouldCreateWorkflowWithNodes_WhenNodesProvided()
    {
        // Arrange
        var nodes = new List<WorkflowNodeDto>
    {
        new() { Id = "node1", Type = "manual_trigger", Name = "Start", Position = new() { X = 0, Y = 0 } },
        new() { Id = "node2", Type = "http_request", Name = "API Call", Position = new() { X = 200, Y = 0 } }
    };

        var connections = new List<NodeConnectionDto>
    {
        new() { SourceNodeId = "node1", TargetNodeId = "node2" }
    };

        var request = new CreateWorkflowRequest
        {
            Name = "Workflow with Nodes",
            Nodes = nodes,
            Connections = connections
        };

        // Act
        var result = await _workflowService.CreateWorkflowAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Nodes);
        Assert.Equal(2, result.Nodes.Count);
        Assert.NotNull(result.Connections);
        Assert.Single(result.Connections);
        Assert.Equal(0, result.ExecutionCount);
    }

    [Fact]
    public async Task CreateWorkflowAsync_ShouldThrowException_WhenNoOrganization()
    {
        // Arrange
        _mockCurrentUserService.Setup(x => x.OrganizationId).Returns((Guid?)null);
        var request = new CreateWorkflowRequest { Name = "Test" };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _workflowService.CreateWorkflowAsync(request)
        );
    }

    #endregion

    #region Get Workflows Tests

    [Fact]
    public async Task GetWorkflowsAsync_ShouldReturnWorkflows_WhenWorkflowsExist()
    {
        // Arrange
        var workflow1 = new Workflow
        {
            Name = "Workflow 1",
            OrganizationId = _testOrgId,
            CreatedBy = _testUserId,
            Status = WorkflowStatus.Active
        };
        var workflow2 = new Workflow
        {
            Name = "Workflow 2",
            OrganizationId = _testOrgId,
            CreatedBy = _testUserId,
            Status = WorkflowStatus.Draft
        };

        _context.Workflows.AddRange(workflow1, workflow2);
        await _context.SaveChangesAsync();

        var request = new GetWorkflowsRequest { Page = 1, Limit = 10 };

        // Act
        var result = await _workflowService.GetWorkflowsAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Workflows.Count);
        Assert.Equal(2, result.Pagination.Total);
    }

    [Fact]
    public async Task GetWorkflowsAsync_ShouldFilterByStatus_WhenStatusProvided()
    {
        // Arrange
        var activeWorkflow = new Workflow
        {
            Name = "Active Workflow",
            OrganizationId = _testOrgId,
            CreatedBy = _testUserId,
            Status = WorkflowStatus.Active
        };
        var draftWorkflow = new Workflow
        {
            Name = "Draft Workflow",
            OrganizationId = _testOrgId,
            CreatedBy = _testUserId,
            Status = WorkflowStatus.Draft
        };

        _context.Workflows.AddRange(activeWorkflow, draftWorkflow);
        await _context.SaveChangesAsync();

        var request = new GetWorkflowsRequest
        {
            Page = 1,
            Limit = 10,
            Status = WorkflowStatus.Active
        };

        // Act
        var result = await _workflowService.GetWorkflowsAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Workflows);
        Assert.Equal("Active Workflow", result.Workflows[0].Name);
    }

    [Fact]
    public async Task GetWorkflowsAsync_ShouldSearchByName_WhenSearchProvided()
    {
        // Arrange
        var workflow1 = new Workflow
        {
            Name = "Customer Onboarding",
            OrganizationId = _testOrgId,
            CreatedBy = _testUserId
        };
        var workflow2 = new Workflow
        {
            Name = "Order Processing",
            OrganizationId = _testOrgId,
            CreatedBy = _testUserId
        };

        _context.Workflows.AddRange(workflow1, workflow2);
        await _context.SaveChangesAsync();

        var request = new GetWorkflowsRequest
        {
            Page = 1,
            Limit = 10,
            Search = "customer"
        };

        // Act
        var result = await _workflowService.GetWorkflowsAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Workflows);
        Assert.Contains("Customer", result.Workflows[0].Name);
    }

    [Fact]
    public async Task GetWorkflowsAsync_ShouldPaginate_WhenMultiplePages()
    {
        // Arrange
        for (int i = 1; i <= 25; i++)
        {
            _context.Workflows.Add(new Workflow
            {
                Name = $"Workflow {i}",
                OrganizationId = _testOrgId,
                CreatedBy = _testUserId
            });
        }
        await _context.SaveChangesAsync();

        var request = new GetWorkflowsRequest { Page = 2, Limit = 10 };

        // Act
        var result = await _workflowService.GetWorkflowsAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(10, result.Workflows.Count);
        Assert.Equal(25, result.Pagination.Total);
        Assert.Equal(3, result.Pagination.TotalPages);
        Assert.True(result.Pagination.HasPrevious);
        Assert.True(result.Pagination.HasNext);
    }

    #endregion

    #region Get Workflow By Id Tests

    [Fact]
    public async Task GetWorkflowByIdAsync_ShouldReturnWorkflow_WhenExists()
    {
        // Arrange
        var workflow = new Workflow
        {
            Name = "Test Workflow",
            Description = "Test Description",
            OrganizationId = _testOrgId,
            CreatedBy = _testUserId,
            Status = WorkflowStatus.Active,
            CreatedAt = DateTime.UtcNow,
            LastModified = DateTime.UtcNow
        };
        _context.Workflows.Add(workflow);
        await _context.SaveChangesAsync();

        // Act
        var result = await _workflowService.GetWorkflowByIdAsync(workflow.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(workflow.Id, result.Id);
        Assert.Equal(workflow.Name, result.Name);
        Assert.Equal(workflow.Description, result.Description);
        Assert.Equal(0, result.ExecutionCount); // No executions
        Assert.Null(result.LastExecutedAt);
    }

    [Fact]
    public async Task GetWorkflowByIdAsync_ShouldReturnNull_WhenNotExists()
    {
        // Act
        var result = await _workflowService.GetWorkflowByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetWorkflowByIdAsync_ShouldReturnNull_WhenNoAccess()
    {
        // Arrange
        var workflow = new Workflow
        {
            Name = "Private Workflow",
            OrganizationId = _testOrgId,
            CreatedBy = _testUserId
        };
        _context.Workflows.Add(workflow);
        await _context.SaveChangesAsync();

        _mockCurrentUserService.Setup(x => x.CanAccessWorkflowAsync(workflow.Id))
            .ReturnsAsync(false);

        // Act
        var result = await _workflowService.GetWorkflowByIdAsync(workflow.Id);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region Update Workflow Tests

    [Fact]
    public async Task UpdateWorkflowAsync_ShouldUpdateWorkflow_WhenValidRequest()
    {
        // Arrange
        var workflow = new Workflow
        {
            Name = "Original Name",
            Description = "Original Description",
            OrganizationId = _testOrgId,
            CreatedBy = _testUserId,
            Version = 1,
            CreatedAt = DateTime.UtcNow,
            LastModified = DateTime.UtcNow
        };
        _context.Workflows.Add(workflow);
        await _context.SaveChangesAsync();

        var request = new UpdateWorkflowRequest
        {
            Name = "Updated Name",
            Description = "Updated Description"
        };

        // Act
        var result = await _workflowService.UpdateWorkflowAsync(workflow.Id, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated Name", result.Name);
        Assert.Equal("Updated Description", result.Description);
        Assert.Equal(2, result.Version); // Version should increment
        Assert.Equal(0, result.ExecutionCount);

        // Verify in database
        var updated = await _context.Workflows.FindAsync(workflow.Id);
        Assert.NotNull(updated);
        Assert.Equal(2, updated.Version);
    }

    [Fact]
    public async Task UpdateWorkflowAsync_ShouldReturnNull_WhenNotExists()
    {
        // Arrange
        var request = new UpdateWorkflowRequest { Name = "Test" };

        // Act
        var result = await _workflowService.UpdateWorkflowAsync(Guid.NewGuid(), request);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateWorkflowAsync_ShouldReturnNull_WhenNoEditPermission()
    {
        // Arrange
        var workflow = new Workflow
        {
            Name = "Test Workflow",
            OrganizationId = _testOrgId,
            CreatedBy = Guid.NewGuid() // Different user
        };
        _context.Workflows.Add(workflow);
        await _context.SaveChangesAsync();

        _mockCurrentUserService.Setup(x => x.HasPermissionAsync("edit_workflows"))
            .ReturnsAsync(false);

        var request = new UpdateWorkflowRequest { Name = "Updated" };

        // Act
        var result = await _workflowService.UpdateWorkflowAsync(workflow.Id, request);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region Delete Workflow Tests

    [Fact]
    public async Task DeleteWorkflowAsync_ShouldDeleteWorkflow_WhenExists()
    {
        // Arrange
        var workflow = new Workflow
        {
            Name = "To Delete",
            OrganizationId = _testOrgId,
            CreatedBy = _testUserId
        };
        _context.Workflows.Add(workflow);
        await _context.SaveChangesAsync();

        // Act
        var result = await _workflowService.DeleteWorkflowAsync(workflow.Id);

        // Assert
        Assert.True(result);

        // Verify deleted from database
        var deleted = await _context.Workflows.FindAsync(workflow.Id);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task DeleteWorkflowAsync_ShouldReturnFalse_WhenNotExists()
    {
        // Act
        var result = await _workflowService.DeleteWorkflowAsync(Guid.NewGuid());

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteWorkflowAsync_ShouldReturnFalse_WhenNoDeletePermission()
    {
        // Arrange
        var workflow = new Workflow
        {
            Name = "Test Workflow",
            OrganizationId = _testOrgId,
            CreatedBy = Guid.NewGuid() // Different user
        };
        _context.Workflows.Add(workflow);
        await _context.SaveChangesAsync();

        _mockCurrentUserService.Setup(x => x.HasPermissionAsync("delete_workflows"))
            .ReturnsAsync(false);

        // Act
        var result = await _workflowService.DeleteWorkflowAsync(workflow.Id);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region Duplicate Workflow Tests

    [Fact]
    public async Task DuplicateWorkflowAsync_ShouldCreateCopy_WhenValidRequest()
    {
        // Arrange
        var nodes = new List<WorkflowNodeDto>
    {
        new() { Id = "node1", Type = "manual_trigger", Name = "Start", Position = new() }
    };
        var nodesJson = JsonSerializer.Serialize(nodes);

        var originalWorkflow = new Workflow
        {
            Name = "Original Workflow",
            Description = "Original Description",
            OrganizationId = _testOrgId,
            CreatedBy = _testUserId,
            NodesJson = nodesJson,
            CreatedAt = DateTime.UtcNow,
            LastModified = DateTime.UtcNow
        };
        _context.Workflows.Add(originalWorkflow);
        await _context.SaveChangesAsync();

        var request = new DuplicateWorkflowRequest
        {
            Name = "Duplicated Workflow",
            Description = "Duplicated Description"
        };

        // Act
        var result = await _workflowService.DuplicateWorkflowAsync(originalWorkflow.Id, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.Name, result.Name);
        Assert.Equal(request.Description, result.Description);
        Assert.NotEqual(originalWorkflow.Id, result.Id);
        Assert.Equal(1, result.Version); // New workflow starts at version 1
        Assert.NotNull(result.Nodes);
        Assert.Single(result.Nodes);
        Assert.Equal(0, result.ExecutionCount);
    }

    #endregion

    #region Update Status Tests

    [Fact]
    public async Task UpdateWorkflowStatusAsync_ShouldUpdateStatus_WhenValid()
    {
        // Arrange
        var nodes = new List<WorkflowNodeDto>
    {
        new() { Id = "node1", Type = "manual_trigger", Name = "Start", Position = new() }
    };
        var nodesJson = JsonSerializer.Serialize(nodes);

        var workflow = new Workflow
        {
            Name = "Test Workflow",
            OrganizationId = _testOrgId,
            CreatedBy = _testUserId,
            Status = WorkflowStatus.Draft,
            NodesJson = nodesJson,
            ConnectionsJson = "[]",
            CreatedAt = DateTime.UtcNow,
            LastModified = DateTime.UtcNow
        };
        _context.Workflows.Add(workflow);
        await _context.SaveChangesAsync();

        // Act
        var result = await _workflowService.UpdateWorkflowStatusAsync(workflow.Id, WorkflowStatus.Active);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(WorkflowStatus.Active, result.Status);
        Assert.Equal(0, result.ExecutionCount);

        // Verify in database
        var updated = await _context.Workflows.FindAsync(workflow.Id);
        Assert.Equal(WorkflowStatus.Active, updated.Status);
    }

    [Fact]
    public async Task UpdateWorkflowStatusAsync_ShouldThrowException_WhenValidationFails()
    {
        // Arrange - workflow with no trigger nodes (invalid)
        var workflow = new Workflow
        {
            Name = "Invalid Workflow",
            OrganizationId = _testOrgId,
            CreatedBy = _testUserId,
            Status = WorkflowStatus.Draft,
            NodesJson = "[]", // No nodes
            ConnectionsJson = "[]"
        };
        _context.Workflows.Add(workflow);
        await _context.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<WorkflowValidationException>(
            () => _workflowService.UpdateWorkflowStatusAsync(workflow.Id, WorkflowStatus.Active)
        );
    }

    #endregion

    #region Validation Tests

    [Fact]
    public async Task ValidateWorkflowAsync_ShouldReturnValid_WhenWorkflowIsValid()
    {
        // Arrange
        var nodes = new List<WorkflowNodeDto>
        {
            new() { Id = "node1", Type = "manual_trigger", Name = "Start", Position = new() },
            new() { Id = "node2", Type = "http_request", Name = "API", Position = new() }
        };
        var connections = new List<NodeConnectionDto>
        {
            new() { SourceNodeId = "node1", TargetNodeId = "node2" }
        };

        var workflow = new Workflow
        {
            Name = "Valid Workflow",
            OrganizationId = _testOrgId,
            CreatedBy = _testUserId,
            NodesJson = JsonSerializer.Serialize(nodes),
            ConnectionsJson = JsonSerializer.Serialize(connections)
        };
        _context.Workflows.Add(workflow);
        await _context.SaveChangesAsync();

        // Act
        var result = await _workflowService.ValidateWorkflowAsync(workflow.Id);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task ValidateWorkflowAsync_ShouldReturnError_WhenNoNodes()
    {
        // Arrange
        var workflow = new Workflow
        {
            Name = "Empty Workflow",
            OrganizationId = _testOrgId,
            CreatedBy = _testUserId,
            NodesJson = "[]",
            ConnectionsJson = "[]"
        };
        _context.Workflows.Add(workflow);
        await _context.SaveChangesAsync();

        // Act
        var result = await _workflowService.ValidateWorkflowAsync(workflow.Id);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Code == "EMPTY_WORKFLOW");
    }

    [Fact]
    public async Task ValidateWorkflowAsync_ShouldReturnError_WhenNoTriggerNode()
    {
        // Arrange
        var nodes = new List<WorkflowNodeDto>
        {
            new() { Id = "node1", Type = "http_request", Name = "API", Position = new() }
        };

        var workflow = new Workflow
        {
            Name = "No Trigger Workflow",
            OrganizationId = _testOrgId,
            CreatedBy = _testUserId,
            NodesJson = JsonSerializer.Serialize(nodes),
            ConnectionsJson = "[]"
        };
        _context.Workflows.Add(workflow);
        await _context.SaveChangesAsync();

        // Act
        var result = await _workflowService.ValidateWorkflowAsync(workflow.Id);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Code == "NO_TRIGGER");
    }

    [Fact]
    public async Task ValidateWorkflowAsync_ShouldReturnWarning_WhenDisconnectedNodes()
    {
        // Arrange
        var nodes = new List<WorkflowNodeDto>
        {
            new() { Id = "node1", Type = "manual_trigger", Name = "Start", Position = new() },
            new() { Id = "node2", Type = "http_request", Name = "API", Position = new() },
            new() { Id = "node3", Type = "http_request", Name = "Disconnected", Position = new() }
        };
        var connections = new List<NodeConnectionDto>
        {
            new() { SourceNodeId = "node1", TargetNodeId = "node2" }
        };

        var workflow = new Workflow
        {
            Name = "Disconnected Nodes Workflow",
            OrganizationId = _testOrgId,
            CreatedBy = _testUserId,
            NodesJson = JsonSerializer.Serialize(nodes),
            ConnectionsJson = JsonSerializer.Serialize(connections)
        };
        _context.Workflows.Add(workflow);
        await _context.SaveChangesAsync();

        // Act
        var result = await _workflowService.ValidateWorkflowAsync(workflow.Id);

        // Assert
        Assert.True(result.IsValid); // Still valid, just a warning
        Assert.Contains(result.Warnings, w => w.Code == "DISCONNECTED_NODE");
    }

    [Fact]
    public async Task ValidateWorkflowAsync_ShouldReturnError_WhenCircularDependency()
    {
        // Arrange
        var nodes = new List<WorkflowNodeDto>
        {
            new() { Id = "node1", Type = "manual_trigger", Name = "Start", Position = new() },
            new() { Id = "node2", Type = "http_request", Name = "API1", Position = new() },
            new() { Id = "node3", Type = "http_request", Name = "API2", Position = new() }
        };
        var connections = new List<NodeConnectionDto>
        {
            new() { SourceNodeId = "node1", TargetNodeId = "node2" },
            new() { SourceNodeId = "node2", TargetNodeId = "node3" },
            new() { SourceNodeId = "node3", TargetNodeId = "node2" } // Circular!
        };

        var workflow = new Workflow
        {
            Name = "Circular Workflow",
            OrganizationId = _testOrgId,
            CreatedBy = _testUserId,
            NodesJson = JsonSerializer.Serialize(nodes),
            ConnectionsJson = JsonSerializer.Serialize(connections)
        };
        _context.Workflows.Add(workflow);
        await _context.SaveChangesAsync();

        // Act
        var result = await _workflowService.ValidateWorkflowAsync(workflow.Id);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Code == "CIRCULAR_DEPENDENCY");
    }

    #endregion

    #region Statistics Tests

    [Fact]
    public async Task GetWorkflowStatisticsAsync_ShouldReturnStats_WhenExecutionsExist()
    {
        // Arrange
        var workflow = new Workflow
        {
            Name = "Test Workflow",
            OrganizationId = _testOrgId,
            CreatedBy = _testUserId
        };
        _context.Workflows.Add(workflow);

        // Add executions
        for (int i = 0; i < 10; i++)
        {
            _context.WorkflowExecutions.Add(new WorkflowExecution
            {
                WorkflowId = workflow.Id,
                Status = i < 8 ? ExecutionStatus.Completed : ExecutionStatus.Failed,
                Duration = TimeSpan.FromSeconds(30 + i),
                StartedAt = DateTime.UtcNow.AddDays(-i)
            });
        }
        await _context.SaveChangesAsync();

        // Act
        var result = await _workflowService.GetWorkflowStatisticsAsync(workflow.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(10, result.TotalExecutions);
        Assert.Equal(8, result.SuccessfulExecutions);
        Assert.Equal(2, result.FailedExecutions);
        Assert.Equal(80.0, result.SuccessRate);
        Assert.True(result.AverageExecutionTimeMs > 0);
    }

    [Fact]
    public async Task GetWorkflowStatisticsAsync_ShouldReturnNull_WhenNoAccess()
    {
        // Arrange
        var workflow = new Workflow
        {
            Name = "Private Workflow",
            OrganizationId = _testOrgId,
            CreatedBy = _testUserId
        };
        _context.Workflows.Add(workflow);
        await _context.SaveChangesAsync();

        _mockCurrentUserService.Setup(x => x.CanAccessWorkflowAsync(workflow.Id))
            .ReturnsAsync(false);

        // Act
        var result = await _workflowService.GetWorkflowStatisticsAsync(workflow.Id);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region Templates Tests

    [Fact]
    public async Task GetWorkflowTemplatesAsync_ShouldReturnOnlyTemplates()
    {
        // Arrange
        var template = new Workflow
        {
            Name = "Template Workflow",
            OrganizationId = _testOrgId,
            CreatedBy = _testUserId,
            IsTemplate = true,
            Visibility = WorkflowVisibility.Public
        };
        var regularWorkflow = new Workflow
        {
            Name = "Regular Workflow",
            OrganizationId = _testOrgId,
            CreatedBy = _testUserId,
            IsTemplate = false
        };

        _context.Workflows.AddRange(template, regularWorkflow);
        await _context.SaveChangesAsync();

        var request = new GetWorkflowsRequest { Page = 1, Limit = 10 };

        // Act
        var result = await _workflowService.GetWorkflowTemplatesAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Workflows);
        Assert.True(result.Workflows[0].IsTemplate);
    }

    [Fact]
    public async Task CreateFromTemplateAsync_ShouldCreateWorkflowFromTemplate()
    {
        // Arrange
        var nodes = new List<WorkflowNodeDto>
    {
        new() { Id = "node1", Type = "manual_trigger", Name = "Start", Position = new() }
    };

        var template = new Workflow
        {
            Name = "Template",
            OrganizationId = _testOrgId,
            CreatedBy = _testUserId,
            IsTemplate = true,
            NodesJson = JsonSerializer.Serialize(nodes),
            CreatedAt = DateTime.UtcNow,
            LastModified = DateTime.UtcNow
        };
        _context.Workflows.Add(template);
        await _context.SaveChangesAsync();

        var request = new CreateWorkflowRequest
        {
            Name = "From Template",
            Description = "Created from template"
        };

        // Act
        var result = await _workflowService.CreateFromTemplateAsync(template.Id, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.Name, result.Name);
        Assert.False(result.IsTemplate); // Should not be a template
        Assert.NotNull(result.Nodes);
        Assert.Single(result.Nodes); // Should have copied nodes
        Assert.Equal(0, result.ExecutionCount);
    }

    #endregion

    #region Get Executions Tests

    [Fact]
    public async Task GetWorkflowExecutionsAsync_ShouldReturnExecutions_WhenExecutionsExist()
    {
        // Arrange
        var workflow = new Workflow
        {
            Name = "Test Workflow",
            OrganizationId = _testOrgId,
            CreatedBy = _testUserId
        };
        _context.Workflows.Add(workflow);

        var execution1 = new WorkflowExecution
        {
            WorkflowId = workflow.Id,
            Status = ExecutionStatus.Completed,
            TriggerType = ExecutionTrigger.Manual,
            StartedAt = DateTime.UtcNow.AddHours(-2),
            CompletedAt = DateTime.UtcNow.AddHours(-1),
            Duration = TimeSpan.FromMinutes(60)
        };

        var execution2 = new WorkflowExecution
        {
            WorkflowId = workflow.Id,
            Status = ExecutionStatus.Failed,
            TriggerType = ExecutionTrigger.Webhook,
            StartedAt = DateTime.UtcNow.AddHours(-1),
            CompletedAt = DateTime.UtcNow,
            Duration = TimeSpan.FromMinutes(30)
        };

        _context.WorkflowExecutions.AddRange(execution1, execution2);
        await _context.SaveChangesAsync();

        var request = new GetExecutionsRequest { Page = 1, Limit = 10 };

        // Act
        var result = await _workflowService.GetWorkflowExecutionsAsync(workflow.Id, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Executions.Count);
        Assert.Equal(2, result.Pagination.Total);
    }

    [Fact]
    public async Task GetWorkflowExecutionsAsync_ShouldFilterByStatus_WhenStatusProvided()
    {
        // Arrange
        var workflow = new Workflow
        {
            Name = "Test Workflow",
            OrganizationId = _testOrgId,
            CreatedBy = _testUserId
        };
        _context.Workflows.Add(workflow);

        _context.WorkflowExecutions.AddRange(
            new WorkflowExecution
            {
                WorkflowId = workflow.Id,
                Status = ExecutionStatus.Completed,
                StartedAt = DateTime.UtcNow
            },
            new WorkflowExecution
            {
                WorkflowId = workflow.Id,
                Status = ExecutionStatus.Failed,
                StartedAt = DateTime.UtcNow
            },
            new WorkflowExecution
            {
                WorkflowId = workflow.Id,
                Status = ExecutionStatus.Completed,
                StartedAt = DateTime.UtcNow
            }
        );
        await _context.SaveChangesAsync();

        var request = new GetExecutionsRequest
        {
            Page = 1,
            Limit = 10,
            Status = ExecutionStatus.Completed
        };

        // Act
        var result = await _workflowService.GetWorkflowExecutionsAsync(workflow.Id, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Executions.Count);
        Assert.All(result.Executions, e => Assert.Equal(ExecutionStatus.Completed, e.Status));
    }

    [Fact]
    public async Task GetWorkflowExecutionsAsync_ShouldFilterByTriggerType_WhenTriggerTypeProvided()
    {
        // Arrange
        var workflow = new Workflow
        {
            Name = "Test Workflow",
            OrganizationId = _testOrgId,
            CreatedBy = _testUserId
        };
        _context.Workflows.Add(workflow);

        _context.WorkflowExecutions.AddRange(
            new WorkflowExecution
            {
                WorkflowId = workflow.Id,
                TriggerType = ExecutionTrigger.Manual,
                StartedAt = DateTime.UtcNow
            },
            new WorkflowExecution
            {
                WorkflowId = workflow.Id,
                TriggerType = ExecutionTrigger.Webhook,
                StartedAt = DateTime.UtcNow
            }
        );
        await _context.SaveChangesAsync();

        var request = new GetExecutionsRequest
        {
            Page = 1,
            Limit = 10,
            TriggerType = ExecutionTrigger.Manual
        };

        // Act
        var result = await _workflowService.GetWorkflowExecutionsAsync(workflow.Id, request);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Executions);
        Assert.Equal(ExecutionTrigger.Manual, result.Executions[0].TriggerType);
    }

    [Fact]
    public async Task GetWorkflowExecutionsAsync_ShouldFilterByDateRange_WhenDatesProvided()
    {
        // Arrange
        var workflow = new Workflow
        {
            Name = "Test Workflow",
            OrganizationId = _testOrgId,
            CreatedBy = _testUserId
        };
        _context.Workflows.Add(workflow);

        var now = DateTime.UtcNow;
        _context.WorkflowExecutions.AddRange(
            new WorkflowExecution
            {
                WorkflowId = workflow.Id,
                StartedAt = now.AddDays(-5),
                CompletedAt = now.AddDays(-5)
            },
            new WorkflowExecution
            {
                WorkflowId = workflow.Id,
                StartedAt = now.AddDays(-2),
                CompletedAt = now.AddDays(-2)
            },
            new WorkflowExecution
            {
                WorkflowId = workflow.Id,
                StartedAt = now.AddDays(-1),
                CompletedAt = now.AddDays(-1)
            }
        );
        await _context.SaveChangesAsync();

        var request = new GetExecutionsRequest
        {
            Page = 1,
            Limit = 10,
            StartedAfter = now.AddDays(-3),
            StartedBefore = now
        };

        // Act
        var result = await _workflowService.GetWorkflowExecutionsAsync(workflow.Id, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Executions.Count); // Only last 2 executions
    }

    [Fact]
    public async Task GetWorkflowExecutionsAsync_ShouldSortByDuration_WhenSortByDuration()
    {
        // Arrange
        var workflow = new Workflow
        {
            Name = "Test Workflow",
            OrganizationId = _testOrgId,
            CreatedBy = _testUserId
        };
        _context.Workflows.Add(workflow);

        _context.WorkflowExecutions.AddRange(
            new WorkflowExecution
            {
                WorkflowId = workflow.Id,
                StartedAt = DateTime.UtcNow,
                Duration = TimeSpan.FromMinutes(10)
            },
            new WorkflowExecution
            {
                WorkflowId = workflow.Id,
                StartedAt = DateTime.UtcNow,
                Duration = TimeSpan.FromMinutes(30)
            },
            new WorkflowExecution
            {
                WorkflowId = workflow.Id,
                StartedAt = DateTime.UtcNow,
                Duration = TimeSpan.FromMinutes(5)
            }
        );
        await _context.SaveChangesAsync();

        var request = new GetExecutionsRequest
        {
            Page = 1,
            Limit = 10,
            SortBy = "duration",
            SortOrder = "asc"
        };

        // Act
        var result = await _workflowService.GetWorkflowExecutionsAsync(workflow.Id, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Executions.Count);
        Assert.True(result.Executions[0].DurationMs < result.Executions[1].DurationMs);
        Assert.True(result.Executions[1].DurationMs < result.Executions[2].DurationMs);
    }

    [Fact]
    public async Task GetWorkflowExecutionsAsync_ShouldThrowException_WhenNoAccess()
    {
        // Arrange
        var workflow = new Workflow
        {
            Name = "Private Workflow",
            OrganizationId = _testOrgId,
            CreatedBy = _testUserId
        };
        _context.Workflows.Add(workflow);
        await _context.SaveChangesAsync();

        _mockCurrentUserService.Setup(x => x.CanAccessWorkflowAsync(workflow.Id))
            .ReturnsAsync(false);

        var request = new GetExecutionsRequest { Page = 1, Limit = 10 };

        // Act & Assert
        await Assert.ThrowsAsync<WorkflowAccessDeniedException>(
            () => _workflowService.GetWorkflowExecutionsAsync(workflow.Id, request)
        );
    }

    [Fact]
    public async Task GetWorkflowExecutionsAsync_ShouldPaginate_WhenMultiplePages()
    {
        // Arrange
        var workflow = new Workflow
        {
            Name = "Test Workflow",
            OrganizationId = _testOrgId,
            CreatedBy = _testUserId
        };
        _context.Workflows.Add(workflow);

        for (int i = 0; i < 25; i++)
        {
            _context.WorkflowExecutions.Add(new WorkflowExecution
            {
                WorkflowId = workflow.Id,
                StartedAt = DateTime.UtcNow.AddMinutes(-i)
            });
        }
        await _context.SaveChangesAsync();

        var request = new GetExecutionsRequest { Page = 2, Limit = 10 };

        // Act
        var result = await _workflowService.GetWorkflowExecutionsAsync(workflow.Id, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(10, result.Executions.Count);
        Assert.Equal(25, result.Pagination.Total);
        Assert.Equal(3, result.Pagination.TotalPages);
        Assert.True(result.Pagination.HasPrevious);
        Assert.True(result.Pagination.HasNext);
    }

    #endregion

    #region Permission Tests

    [Fact]
    public async Task CanUserAccessWorkflowAsync_ShouldReturnTrue_WhenUserHasAccess()
    {
        // Arrange
        var workflow = new Workflow
        {
            Name = "Test Workflow",
            OrganizationId = _testOrgId,
            CreatedBy = _testUserId
        };
        _context.Workflows.Add(workflow);
        await _context.SaveChangesAsync();

        _mockCurrentUserService.Setup(x => x.CanAccessWorkflowAsync(workflow.Id))
            .ReturnsAsync(true);

        // Act
        var result = await _workflowService.CanUserAccessWorkflowAsync(workflow.Id, _testUserId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CanUserEditWorkflowAsync_ShouldReturnTrue_WhenUserIsOwner()
    {
        // Arrange
        var workflow = new Workflow
        {
            Name = "Test Workflow",
            OrganizationId = _testOrgId,
            CreatedBy = _testUserId
        };
        _context.Workflows.Add(workflow);
        await _context.SaveChangesAsync();

        // Act
        var result = await _workflowService.CanUserEditWorkflowAsync(workflow.Id, _testUserId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CanUserEditWorkflowAsync_ShouldReturnTrue_WhenUserHasPermission()
    {
        // Arrange
        var otherUserId = Guid.NewGuid();
        var workflow = new Workflow
        {
            Name = "Test Workflow",
            OrganizationId = _testOrgId,
            CreatedBy = otherUserId // Different user
        };
        _context.Workflows.Add(workflow);
        await _context.SaveChangesAsync();

        _mockCurrentUserService.Setup(x => x.HasPermissionAsync("edit_workflows"))
            .ReturnsAsync(true);

        // Act
        var result = await _workflowService.CanUserEditWorkflowAsync(workflow.Id, _testUserId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CanUserEditWorkflowAsync_ShouldReturnFalse_WhenNoPermission()
    {
        // Arrange
        var otherUserId = Guid.NewGuid();
        var workflow = new Workflow
        {
            Name = "Test Workflow",
            OrganizationId = _testOrgId,
            CreatedBy = otherUserId
        };
        _context.Workflows.Add(workflow);
        await _context.SaveChangesAsync();

        _mockCurrentUserService.Setup(x => x.HasPermissionAsync("edit_workflows"))
            .ReturnsAsync(false);

        // Act
        var result = await _workflowService.CanUserEditWorkflowAsync(workflow.Id, _testUserId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CanUserDeleteWorkflowAsync_ShouldReturnTrue_WhenUserIsOwner()
    {
        // Arrange
        var workflow = new Workflow
        {
            Name = "Test Workflow",
            OrganizationId = _testOrgId,
            CreatedBy = _testUserId
        };
        _context.Workflows.Add(workflow);
        await _context.SaveChangesAsync();

        // Act
        var result = await _workflowService.CanUserDeleteWorkflowAsync(workflow.Id, _testUserId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CanUserDeleteWorkflowAsync_ShouldReturnFalse_WhenNoPermission()
    {
        // Arrange
        var otherUserId = Guid.NewGuid();
        var workflow = new Workflow
        {
            Name = "Test Workflow",
            OrganizationId = _testOrgId,
            CreatedBy = otherUserId
        };
        _context.Workflows.Add(workflow);
        await _context.SaveChangesAsync();

        _mockCurrentUserService.Setup(x => x.HasPermissionAsync("delete_workflows"))
            .ReturnsAsync(false);

        // Act
        var result = await _workflowService.CanUserDeleteWorkflowAsync(workflow.Id, _testUserId);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region Edge Cases and Error Handling

    [Fact]
    public async Task GetWorkflowsAsync_ShouldReturnEmpty_WhenNoWorkflowsInOrganization()
    {
        // Arrange
        var request = new GetWorkflowsRequest { Page = 1, Limit = 10 };

        // Act
        var result = await _workflowService.GetWorkflowsAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Workflows);
        Assert.Equal(0, result.Pagination.Total);
    }

    [Fact]
    public async Task GetWorkflowsAsync_ShouldHandleLargePageSize_WhenLimitExceeds100()
    {
        // Arrange
        for (int i = 0; i < 150; i++)
        {
            _context.Workflows.Add(new Workflow
            {
                Name = $"Workflow {i}",
                OrganizationId = _testOrgId,
                CreatedBy = _testUserId
            });
        }
        await _context.SaveChangesAsync();

        var request = new GetWorkflowsRequest
        {
            Page = 1,
            Limit = 100 // Max allowed
        };

        // Act
        var result = await _workflowService.GetWorkflowsAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(100, result.Workflows.Count);
        Assert.Equal(150, result.Pagination.Total);
    }

    [Fact]
    public async Task ValidateWorkflowAsync_ShouldThrowException_WhenWorkflowNotFound()
    {
        // Act & Assert
        await Assert.ThrowsAsync<WorkflowNotFoundException>(
            () => _workflowService.ValidateWorkflowAsync(Guid.NewGuid())
        );
    }

    [Fact]
    public async Task GetWorkflowStatisticsAsync_ShouldCalculateMedian_WhenMultipleExecutions()
    {
        // Arrange
        var workflow = new Workflow
        {
            Name = "Test Workflow",
            OrganizationId = _testOrgId,
            CreatedBy = _testUserId
        };
        _context.Workflows.Add(workflow);

        // Add executions with specific durations for median calculation
        var durations = new[] { 10, 20, 30, 40, 50 }; // Median should be 30
        foreach (var duration in durations)
        {
            _context.WorkflowExecutions.Add(new WorkflowExecution
            {
                WorkflowId = workflow.Id,
                Status = ExecutionStatus.Completed,
                Duration = TimeSpan.FromSeconds(duration),
                StartedAt = DateTime.UtcNow
            });
        }
        await _context.SaveChangesAsync();

        // Act
        var result = await _workflowService.GetWorkflowStatisticsAsync(workflow.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(30000, result.MedianExecutionTimeMs); // 30 seconds in ms
    }

    [Fact]
    public async Task GetWorkflowStatisticsAsync_ShouldCountExecutionsInTimeRanges()
    {
        // Arrange
        var workflow = new Workflow
        {
            Name = "Test Workflow",
            OrganizationId = _testOrgId,
            CreatedBy = _testUserId
        };
        _context.Workflows.Add(workflow);

        var now = DateTime.UtcNow;
        _context.WorkflowExecutions.AddRange(
            new WorkflowExecution { WorkflowId = workflow.Id, StartedAt = now.AddHours(-1) },
            new WorkflowExecution { WorkflowId = workflow.Id, StartedAt = now.AddHours(-12) },
            new WorkflowExecution { WorkflowId = workflow.Id, StartedAt = now.AddDays(-3) },
            new WorkflowExecution { WorkflowId = workflow.Id, StartedAt = now.AddDays(-10) },
            new WorkflowExecution { WorkflowId = workflow.Id, StartedAt = now.AddDays(-40) }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _workflowService.GetWorkflowStatisticsAsync(workflow.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.ExecutionsLast24Hours);
        Assert.Equal(3, result.ExecutionsLast7Days);
        Assert.Equal(4, result.ExecutionsLast30Days);
    }

    [Fact]
    public async Task UpdateWorkflowAsync_ShouldOnlyUpdateProvidedFields()
    {
        // Arrange
        var workflow = new Workflow
        {
            Name = "Original Name",
            Description = "Original Description",
            Visibility = WorkflowVisibility.Private,
            OrganizationId = _testOrgId,
            CreatedBy = _testUserId,
            CreatedAt = DateTime.UtcNow,
            LastModified = DateTime.UtcNow
        };
        _context.Workflows.Add(workflow);
        await _context.SaveChangesAsync();

        var request = new UpdateWorkflowRequest
        {
            Name = "Updated Name"
            // Description and Visibility not provided
        };

        // Act
        var result = await _workflowService.UpdateWorkflowAsync(workflow.Id, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated Name", result.Name);
        Assert.Equal("Original Description", result.Description); // Should remain unchanged
        Assert.Equal(WorkflowVisibility.Private, result.Visibility); // Should remain unchanged
    }

    #endregion

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}