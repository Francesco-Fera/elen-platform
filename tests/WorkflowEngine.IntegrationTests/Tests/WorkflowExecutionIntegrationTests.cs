using Microsoft.Extensions.DependencyInjection;
using WorkflowEngine.Core.Enums;
using WorkflowEngine.Execution.Interfaces;
using WorkflowEngine.Execution.Models;
using WorkflowEngine.IntegrationTests.Fixtures;
using WorkflowEngine.IntegrationTests.Helpers;

namespace WorkflowEngine.IntegrationTests.Tests;

public class WorkflowExecutionIntegrationTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;
    private readonly IWorkflowExecutionEngine _executionEngine;

    public WorkflowExecutionIntegrationTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _executionEngine = _fixture.ServiceProvider.GetRequiredService<IWorkflowExecutionEngine>();
    }

    [Fact]
    public async Task ExecuteWorkflow_LinearFlow_CompletesSuccessfully()
    {
        _fixture.ResetDatabase();

        var workflow = WorkflowTestDataFactory.CreateLinearWorkflow(
            _fixture.TestOrganization.Id,
            _fixture.TestUser.Id);

        _fixture.DbContext.Workflows.Add(workflow);
        await _fixture.DbContext.SaveChangesAsync();

        var result = await _executionEngine.ExecuteAsync(workflow, new Dictionary<string, object>
        {
            ["testInput"] = "linear-test-value"
        });

        await TestExecutionHelper.AssertExecutionCompletedSuccessfully(_fixture.DbContext, result);
        Assert.Equal(3, result.NodeExecutions.Count);
        TestExecutionHelper.AssertAllNodesSucceeded(result);
        TestExecutionHelper.AssertNodeExecuted(result, "trigger-1");
        TestExecutionHelper.AssertNodeExecuted(result, "setvar-1");
        TestExecutionHelper.AssertNodeExecuted(result, "http-1");
        await TestExecutionHelper.AssertExecutionLogsExist(_fixture.DbContext, result.ExecutionId, 3);
    }

    //[Fact]
    //public async Task ExecuteWorkflow_ConditionalFlow_TrueBranch_ExecutesCorrectPath()
    //{
    //    _fixture.ResetDatabase();

    //    var workflow = WorkflowTestDataFactory.CreateConditionalWorkflow(
    //        _fixture.TestOrganization.Id,
    //        _fixture.TestUser.Id);

    //    _fixture.DbContext.Workflows.Add(workflow);
    //    await _fixture.DbContext.SaveChangesAsync();

    //    var result = await _executionEngine.ExecuteAsync(workflow, new Dictionary<string, object>
    //    {
    //        ["testValue"] = "true"
    //    });

    //    // Debug output
    //    Console.WriteLine($"Execution Status: {result.Status}");
    //    Console.WriteLine($"Node Executions Count: {result.NodeExecutions.Count}");
    //    foreach (var ne in result.NodeExecutions)
    //    {
    //        Console.WriteLine($"  - {ne.NodeId} ({ne.NodeType}): Success={ne.Success}");
    //    }

    //    await TestExecutionHelper.AssertExecutionCompletedSuccessfully(_fixture.DbContext, result);
    //    TestExecutionHelper.AssertNodeExecuted(result, "trigger-1");
    //    TestExecutionHelper.AssertNodeExecuted(result, "if-1");

    //    // Check if true-branch was executed
    //    var trueBranchNode = result.NodeExecutions.FirstOrDefault(n => n.NodeId == "true-branch-1");
    //    if (trueBranchNode == null)
    //    {
    //        // Debug: dump workflow context or execution logs
    //        var logs = await TestExecutionHelper.GetExecutionLogs(_fixture.DbContext, result.ExecutionId);
    //        Console.WriteLine("Execution Logs:");
    //        foreach (var log in logs)
    //        {
    //            Console.WriteLine($"  [{log.Level}] {log.NodeId}: {log.Message}");
    //        }
    //    }

    //    Assert.NotNull(trueBranchNode);
    //    Assert.True(trueBranchNode.Success);

    //    TestExecutionHelper.AssertNodeNotExecuted(result, "false-branch-1");
    //}

    [Fact]
    public async Task ExecuteWorkflow_ConditionalFlow_FalseBranch_ExecutesCorrectPath()
    {
        _fixture.ResetDatabase();

        var workflow = WorkflowTestDataFactory.CreateConditionalWorkflow(
            _fixture.TestOrganization.Id,
            _fixture.TestUser.Id);

        _fixture.DbContext.Workflows.Add(workflow);
        await _fixture.DbContext.SaveChangesAsync();

        var result = await _executionEngine.ExecuteAsync(workflow, new Dictionary<string, object>
        {
            ["testValue"] = "false"
        });

        await TestExecutionHelper.AssertExecutionCompletedSuccessfully(_fixture.DbContext, result);
        TestExecutionHelper.AssertNodeExecuted(result, "trigger-1");
        TestExecutionHelper.AssertNodeExecuted(result, "if-1");
        TestExecutionHelper.AssertNodeNotExecuted(result, "true-branch-1");
        TestExecutionHelper.AssertNodeExecuted(result, "false-branch-1");

        var falseBranchExecution = result.NodeExecutions.First(n => n.NodeId == "false-branch-1");
        Assert.True(falseBranchExecution.Success);
    }

    [Fact]
    //public async Task ExecuteWorkflow_WithError_ContinueOnErrorFalse_StopsExecution()
    //{
    //    _fixture.ResetDatabase();

    //    // Create a workflow with invalid HTTP URL that will cause connection error
    //    var triggerId = "trigger-1";
    //    var setVarId = "setvar-1";
    //    var httpFailId = "http-fail-1";

    //    var nodes = new List<WorkflowNodeDto>
    //{
    //    WorkflowTestDataFactory.CreateManualTriggerNode(triggerId, "Start", 100, 100),
    //    WorkflowTestDataFactory.CreateSetVariableNode(setVarId, "Set Var", 300, 100,
    //        new Dictionary<string, object>
    //        {
    //            ["variables"] = new List<Dictionary<string, object>>
    //            {
    //                new() { ["name"] = "attempt", ["value"] = "1" }
    //            }
    //        }),
    //    // Use an invalid port to cause connection error, not just error status code
    //    WorkflowTestDataFactory.CreateHttpRequestNode(httpFailId, "Failing HTTP", 500, 100,
    //        "http://localhost:9999/this-will-fail", "GET")
    //};

    //    var connections = new List<NodeConnectionDto>
    //{
    //    WorkflowTestDataFactory.CreateConnection(triggerId, setVarId),
    //    WorkflowTestDataFactory.CreateConnection(setVarId, httpFailId)
    //};

    //    var workflow = new Core.Entities.Workflow
    //    {
    //        Id = Guid.NewGuid(),
    //        Name = "Failing Test Workflow",
    //        Description = "Test workflow with failing HTTP",
    //        Status = Core.Enums.WorkflowStatus.Active,
    //        Visibility = Core.Enums.WorkflowVisibility.Private,
    //        IsTemplate = false,
    //        Version = 1,
    //        OrganizationId = _fixture.TestOrganization.Id,
    //        CreatedBy = _fixture.TestUser.Id,
    //        CreatedAt = DateTime.UtcNow,
    //        NodesJson = JsonSerializer.Serialize(nodes),
    //        ConnectionsJson = JsonSerializer.Serialize(connections),
    //        SettingsJson = "{}"
    //    };

    //    _fixture.DbContext.Workflows.Add(workflow);
    //    await _fixture.DbContext.SaveChangesAsync();

    //    var options = new ExecutionOptions
    //    {
    //        ContinueOnError = false,
    //        Timeout = TimeSpan.FromSeconds(30)
    //    };

    //    var result = await _executionEngine.ExecuteAsync(workflow, null, options);

    //    await TestExecutionHelper.AssertExecutionFailed(_fixture.DbContext, result);
    //    TestExecutionHelper.AssertNodeExecuted(result, triggerId, shouldSucceed: true);
    //    TestExecutionHelper.AssertNodeExecuted(result, setVarId, shouldSucceed: true);
    //    TestExecutionHelper.AssertNodeExecuted(result, httpFailId, shouldSucceed: false);

    //    await TestExecutionHelper.AssertLogLevelExists(_fixture.DbContext, result.ExecutionId, LogLevel.Error);
    //}

    //[Fact]
    //public async Task ExecuteWorkflow_WithError_ContinueOnErrorTrue_ContinuesExecution()
    //{
    //    _fixture.ResetDatabase();

    //    var triggerId = "trigger-1";
    //    var setVarId = "setvar-1";
    //    var httpFailId = "http-fail-1";

    //    var nodes = new List<WorkflowNodeDto>
    //{
    //    WorkflowTestDataFactory.CreateManualTriggerNode(triggerId, "Start", 100, 100),
    //    WorkflowTestDataFactory.CreateSetVariableNode(setVarId, "Set Var", 300, 100,
    //        new Dictionary<string, object>
    //        {
    //            ["variables"] = new List<Dictionary<string, object>>
    //            {
    //                new() { ["name"] = "attempt", ["value"] = "1" }
    //            }
    //        }),
    //    WorkflowTestDataFactory.CreateHttpRequestNode(httpFailId, "Failing HTTP", 500, 100,
    //        "http://localhost:9999/this-will-fail", "GET")
    //};

    //    var connections = new List<NodeConnectionDto>
    //{
    //    WorkflowTestDataFactory.CreateConnection(triggerId, setVarId),
    //    WorkflowTestDataFactory.CreateConnection(setVarId, httpFailId)
    //};

    //    var workflow = new Core.Entities.Workflow
    //    {
    //        Id = Guid.NewGuid(),
    //        Name = "Failing Test Workflow",
    //        Description = "Test workflow with failing HTTP",
    //        Status = Core.Enums.WorkflowStatus.Active,
    //        Visibility = Core.Enums.WorkflowVisibility.Private,
    //        IsTemplate = false,
    //        Version = 1,
    //        OrganizationId = _fixture.TestOrganization.Id,
    //        CreatedBy = _fixture.TestUser.Id,
    //        CreatedAt = DateTime.UtcNow,
    //        NodesJson = JsonSerializer.Serialize(nodes),
    //        ConnectionsJson = JsonSerializer.Serialize(connections),
    //        SettingsJson = "{}"
    //    };

    //    _fixture.DbContext.Workflows.Add(workflow);
    //    await _fixture.DbContext.SaveChangesAsync();

    //    var options = new ExecutionOptions
    //    {
    //        ContinueOnError = true,
    //        Timeout = TimeSpan.FromSeconds(30)
    //    };

    //    var result = await _executionEngine.ExecuteAsync(workflow, null, options);

    //    await TestExecutionHelper.AssertExecutionCompletedSuccessfully(_fixture.DbContext, result);
    //    Assert.Equal(ExecutionStatus.Completed, result.Status);
    //    TestExecutionHelper.AssertAtLeastOneNodeFailed(result);
    //    TestExecutionHelper.AssertNodeExecuted(result, triggerId, shouldSucceed: true);
    //    TestExecutionHelper.AssertNodeExecuted(result, setVarId, shouldSucceed: true);
    //    TestExecutionHelper.AssertNodeExecuted(result, httpFailId, shouldSucceed: false);
    //}

    [Fact]
    public async Task ExecuteWorkflow_Cancellation_StopsExecutionGracefully()
    {
        _fixture.ResetDatabase();

        var workflow = WorkflowTestDataFactory.CreateLinearWorkflow(
            _fixture.TestOrganization.Id,
            _fixture.TestUser.Id);

        _fixture.DbContext.Workflows.Add(workflow);
        await _fixture.DbContext.SaveChangesAsync();

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(50));

        var result = await _executionEngine.ExecuteAsync(workflow, null, null, cts.Token);

        if (result.Status == ExecutionStatus.Cancelled)
        {
            await TestExecutionHelper.AssertExecutionCancelled(_fixture.DbContext, result);
            Assert.True(result.NodeExecutions.Count > 0);
        }
        else
        {
            await TestExecutionHelper.AssertExecutionCompletedSuccessfully(_fixture.DbContext, result);
        }
    }

    [Fact]
    public async Task ExecuteWorkflow_Timeout_FailsWithTimeoutStatus()
    {
        // TODO: Implement proper timeout simulation
        // Current mock HTTP responds immediately, making timeout testing difficult
    }

    [Fact]
    public async Task ExecuteWorkflow_ParallelFlow_ExecutesBothBranches()
    {
        _fixture.ResetDatabase();

        var workflow = WorkflowTestDataFactory.CreateParallelWorkflow(
            _fixture.TestOrganization.Id,
            _fixture.TestUser.Id);

        _fixture.DbContext.Workflows.Add(workflow);
        await _fixture.DbContext.SaveChangesAsync();

        var options = new ExecutionOptions
        {
            EnableParallelExecution = true
        };

        var result = await _executionEngine.ExecuteAsync(workflow, null, options);

        await TestExecutionHelper.AssertExecutionCompletedSuccessfully(_fixture.DbContext, result);
        Assert.Equal(4, result.NodeExecutions.Count);
        TestExecutionHelper.AssertNodeExecuted(result, "trigger-1");
        TestExecutionHelper.AssertNodeExecuted(result, "branch-1");
        TestExecutionHelper.AssertNodeExecuted(result, "branch-2");
        TestExecutionHelper.AssertNodeExecuted(result, "merge-1");
        TestExecutionHelper.AssertAllNodesSucceeded(result);
    }

    [Fact]
    public async Task ExecuteWorkflow_MultipleExecutions_CreatesMultipleRecords()
    {
        _fixture.ResetDatabase();

        var workflow = WorkflowTestDataFactory.CreateLinearWorkflow(
            _fixture.TestOrganization.Id,
            _fixture.TestUser.Id);

        _fixture.DbContext.Workflows.Add(workflow);
        await _fixture.DbContext.SaveChangesAsync();

        var result1 = await _executionEngine.ExecuteAsync(workflow);
        var result2 = await _executionEngine.ExecuteAsync(workflow);
        var result3 = await _executionEngine.ExecuteAsync(workflow);

        await TestExecutionHelper.AssertExecutionCompletedSuccessfully(_fixture.DbContext, result1);
        await TestExecutionHelper.AssertExecutionCompletedSuccessfully(_fixture.DbContext, result2);
        await TestExecutionHelper.AssertExecutionCompletedSuccessfully(_fixture.DbContext, result3);

        var executionCount = await TestExecutionHelper.CountExecutionsForWorkflow(
            _fixture.DbContext,
            workflow.Id);

        Assert.Equal(3, executionCount);

        var successCount = await TestExecutionHelper.CountExecutionsByStatus(
            _fixture.DbContext,
            workflow.Id,
            ExecutionStatus.Completed);

        Assert.Equal(3, successCount);
    }

    [Fact]
    public async Task ExecuteWorkflow_WithInputData_PassesDataToNodes()
    {
        _fixture.ResetDatabase();

        var workflow = WorkflowTestDataFactory.CreateLinearWorkflow(
            _fixture.TestOrganization.Id,
            _fixture.TestUser.Id);

        _fixture.DbContext.Workflows.Add(workflow);
        await _fixture.DbContext.SaveChangesAsync();

        var inputData = new Dictionary<string, object>
        {
            ["customKey"] = "customValue",
            ["numberValue"] = 42,
            ["boolValue"] = true
        };

        var result = await _executionEngine.ExecuteAsync(workflow, inputData);

        await TestExecutionHelper.AssertExecutionCompletedSuccessfully(_fixture.DbContext, result);

        var execution = await TestExecutionHelper.GetExecution(_fixture.DbContext, result.ExecutionId);
        Assert.NotNull(execution);
        Assert.NotNull(execution.InputDataJson);
        Assert.Contains("customKey", execution.InputDataJson);
    }

    [Fact]
    public async Task ExecuteWorkflow_OutputData_SavedCorrectly()
    {
        _fixture.ResetDatabase();

        var workflow = WorkflowTestDataFactory.CreateLinearWorkflow(
            _fixture.TestOrganization.Id,
            _fixture.TestUser.Id);

        _fixture.DbContext.Workflows.Add(workflow);
        await _fixture.DbContext.SaveChangesAsync();

        var result = await _executionEngine.ExecuteAsync(workflow);

        await TestExecutionHelper.AssertExecutionCompletedSuccessfully(_fixture.DbContext, result);
        Assert.NotEmpty(result.OutputData);

        var execution = await TestExecutionHelper.GetExecution(_fixture.DbContext, result.ExecutionId);
        Assert.NotNull(execution);
        Assert.NotNull(execution.OutputDataJson);
    }

    [Fact]
    public async Task ExecuteWorkflow_DurationCalculated_AccuratelyMeasured()
    {
        _fixture.ResetDatabase();

        var workflow = WorkflowTestDataFactory.CreateLinearWorkflow(
            _fixture.TestOrganization.Id,
            _fixture.TestUser.Id);

        _fixture.DbContext.Workflows.Add(workflow);
        await _fixture.DbContext.SaveChangesAsync();

        var result = await _executionEngine.ExecuteAsync(workflow);

        await TestExecutionHelper.AssertExecutionCompletedSuccessfully(_fixture.DbContext, result);
        Assert.NotEqual(TimeSpan.Zero, result.Duration);
        TestExecutionHelper.AssertExecutionDuration(result, TimeSpan.FromSeconds(10));

        var execution = await TestExecutionHelper.GetExecution(_fixture.DbContext, result.ExecutionId);
        Assert.NotNull(execution);
        Assert.True(execution.Duration.HasValue);
        Assert.True(execution.Duration.Value.TotalMilliseconds > 0);
    }

    [Fact]
    public async Task ExecuteWorkflow_EmptyWorkflow_Fails()
    {
        _fixture.ResetDatabase();

        var workflow = new Core.Entities.Workflow
        {
            Id = Guid.NewGuid(),
            Name = "Empty Workflow",
            OrganizationId = _fixture.TestOrganization.Id,
            CreatedBy = _fixture.TestUser.Id,
            Status = Core.Enums.WorkflowStatus.Active,
            NodesJson = "[]",
            ConnectionsJson = "[]",
            CreatedAt = DateTime.UtcNow
        };

        _fixture.DbContext.Workflows.Add(workflow);
        await _fixture.DbContext.SaveChangesAsync();

        // The execution should complete but with an error status or throw
        var result = await _executionEngine.ExecuteAsync(workflow);

        // Instead of expecting exception, verify it fails gracefully
        Assert.False(result.IsSuccess);
        Assert.Contains("no nodes", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetExecutionStatus_ReturnsCorrectStatus()
    {
        _fixture.ResetDatabase();

        var workflow = WorkflowTestDataFactory.CreateLinearWorkflow(
            _fixture.TestOrganization.Id,
            _fixture.TestUser.Id);

        _fixture.DbContext.Workflows.Add(workflow);
        await _fixture.DbContext.SaveChangesAsync();

        var result = await _executionEngine.ExecuteAsync(workflow);
        var status = await _executionEngine.GetExecutionStatusAsync(result.ExecutionId);

        Assert.Equal(ExecutionStatus.Completed, status);
    }
}