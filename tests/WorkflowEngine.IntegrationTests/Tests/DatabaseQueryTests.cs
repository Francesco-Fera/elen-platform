using Microsoft.EntityFrameworkCore;
using WorkflowEngine.Core.Enums;
using WorkflowEngine.IntegrationTests.Fixtures;
using WorkflowEngine.IntegrationTests.Helpers;

namespace WorkflowEngine.IntegrationTests.Tests;

public class DatabaseQueryTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;

    public DatabaseQueryTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    #region Execution Queries

    [Fact]
    public async Task QueryExecutions_ByWorkflow_ReturnsCorrectResults()
    {
        _fixture.ResetDatabase();

        var workflow1 = WorkflowTestDataFactory.CreateLinearWorkflow(
            _fixture.TestOrganization.Id,
            _fixture.TestUser.Id,
            "Workflow 1");

        var workflow2 = WorkflowTestDataFactory.CreateLinearWorkflow(
            _fixture.TestOrganization.Id,
            _fixture.TestUser.Id,
            "Workflow 2");

        _fixture.DbContext.Workflows.AddRange(workflow1, workflow2);
        await _fixture.DbContext.SaveChangesAsync();

        await CreateExecutionsForWorkflow(workflow1.Id, 5);
        await CreateExecutionsForWorkflow(workflow2.Id, 3);

        var workflow1Executions = await _fixture.DbContext.WorkflowExecutions
            .Where(e => e.WorkflowId == workflow1.Id)
            .ToListAsync();

        var workflow2Executions = await _fixture.DbContext.WorkflowExecutions
            .Where(e => e.WorkflowId == workflow2.Id)
            .ToListAsync();

        Assert.Equal(5, workflow1Executions.Count);
        Assert.Equal(3, workflow2Executions.Count);
        Assert.All(workflow1Executions, e => Assert.Equal(workflow1.Id, e.WorkflowId));
        Assert.All(workflow2Executions, e => Assert.Equal(workflow2.Id, e.WorkflowId));
    }

    [Fact]
    public async Task QueryExecutions_ByStatus_FiltersCorrectly()
    {
        _fixture.ResetDatabase();

        var workflow = WorkflowTestDataFactory.CreateLinearWorkflow(
            _fixture.TestOrganization.Id,
            _fixture.TestUser.Id);

        _fixture.DbContext.Workflows.Add(workflow);
        await _fixture.DbContext.SaveChangesAsync();

        await CreateExecutionWithStatus(workflow.Id, ExecutionStatus.Completed);
        await CreateExecutionWithStatus(workflow.Id, ExecutionStatus.Completed);
        await CreateExecutionWithStatus(workflow.Id, ExecutionStatus.Completed);
        await CreateExecutionWithStatus(workflow.Id, ExecutionStatus.Failed);
        await CreateExecutionWithStatus(workflow.Id, ExecutionStatus.Failed);
        await CreateExecutionWithStatus(workflow.Id, ExecutionStatus.Cancelled);

        var completedCount = await _fixture.DbContext.WorkflowExecutions
            .CountAsync(e => e.WorkflowId == workflow.Id && e.Status == ExecutionStatus.Completed);

        var failedCount = await _fixture.DbContext.WorkflowExecutions
            .CountAsync(e => e.WorkflowId == workflow.Id && e.Status == ExecutionStatus.Failed);

        var cancelledCount = await _fixture.DbContext.WorkflowExecutions
            .CountAsync(e => e.WorkflowId == workflow.Id && e.Status == ExecutionStatus.Cancelled);

        Assert.Equal(3, completedCount);
        Assert.Equal(2, failedCount);
        Assert.Equal(1, cancelledCount);
    }

    [Fact]
    public async Task QueryExecutions_WithPagination_ReturnsCorrectPage()
    {
        _fixture.ResetDatabase();

        var workflow = WorkflowTestDataFactory.CreateLinearWorkflow(
            _fixture.TestOrganization.Id,
            _fixture.TestUser.Id);

        _fixture.DbContext.Workflows.Add(workflow);
        await _fixture.DbContext.SaveChangesAsync();

        await CreateExecutionsForWorkflow(workflow.Id, 25);

        var pageSize = 10;
        var page1 = await _fixture.DbContext.WorkflowExecutions
            .Where(e => e.WorkflowId == workflow.Id)
            .OrderByDescending(e => e.StartedAt)
            .Take(pageSize)
            .ToListAsync();

        var page2 = await _fixture.DbContext.WorkflowExecutions
            .Where(e => e.WorkflowId == workflow.Id)
            .OrderByDescending(e => e.StartedAt)
            .Skip(pageSize)
            .Take(pageSize)
            .ToListAsync();

        var page3 = await _fixture.DbContext.WorkflowExecutions
            .Where(e => e.WorkflowId == workflow.Id)
            .OrderByDescending(e => e.StartedAt)
            .Skip(pageSize * 2)
            .Take(pageSize)
            .ToListAsync();

        Assert.Equal(10, page1.Count);
        Assert.Equal(10, page2.Count);
        Assert.Equal(5, page3.Count);

        var allIds = page1.Concat(page2).Concat(page3).Select(e => e.Id).ToList();
        Assert.Equal(25, allIds.Distinct().Count());
    }

    [Fact]
    public async Task QueryExecutions_OrderByStartedAt_OrdersCorrectly()
    {
        _fixture.ResetDatabase();

        var workflow = WorkflowTestDataFactory.CreateLinearWorkflow(
            _fixture.TestOrganization.Id,
            _fixture.TestUser.Id);

        _fixture.DbContext.Workflows.Add(workflow);
        await _fixture.DbContext.SaveChangesAsync();

        var execution1 = await CreateExecutionWithTimestamp(workflow.Id, DateTime.UtcNow.AddHours(-3));
        var execution2 = await CreateExecutionWithTimestamp(workflow.Id, DateTime.UtcNow.AddHours(-2));
        var execution3 = await CreateExecutionWithTimestamp(workflow.Id, DateTime.UtcNow.AddHours(-1));

        var executionsAsc = await _fixture.DbContext.WorkflowExecutions
            .Where(e => e.WorkflowId == workflow.Id)
            .OrderBy(e => e.StartedAt)
            .ToListAsync();

        var executionsDesc = await _fixture.DbContext.WorkflowExecutions
            .Where(e => e.WorkflowId == workflow.Id)
            .OrderByDescending(e => e.StartedAt)
            .ToListAsync();

        Assert.Equal(execution1.Id, executionsAsc[0].Id);
        Assert.Equal(execution2.Id, executionsAsc[1].Id);
        Assert.Equal(execution3.Id, executionsAsc[2].Id);

        Assert.Equal(execution3.Id, executionsDesc[0].Id);
        Assert.Equal(execution2.Id, executionsDesc[1].Id);
        Assert.Equal(execution1.Id, executionsDesc[2].Id);
    }

    [Fact]
    public async Task QueryExecutions_ByDateRange_FiltersCorrectly()
    {
        _fixture.ResetDatabase();

        var workflow = WorkflowTestDataFactory.CreateLinearWorkflow(
            _fixture.TestOrganization.Id,
            _fixture.TestUser.Id);

        _fixture.DbContext.Workflows.Add(workflow);
        await _fixture.DbContext.SaveChangesAsync();

        await CreateExecutionWithTimestamp(workflow.Id, DateTime.UtcNow.AddDays(-10));
        await CreateExecutionWithTimestamp(workflow.Id, DateTime.UtcNow.AddDays(-5));
        await CreateExecutionWithTimestamp(workflow.Id, DateTime.UtcNow.AddDays(-3));
        await CreateExecutionWithTimestamp(workflow.Id, DateTime.UtcNow.AddDays(-1));

        var startDate = DateTime.UtcNow.AddDays(-7);
        var endDate = DateTime.UtcNow.AddDays(-2);

        var filteredExecutions = await _fixture.DbContext.WorkflowExecutions
            .Where(e => e.WorkflowId == workflow.Id &&
                       e.StartedAt >= startDate &&
                       e.StartedAt <= endDate)
            .ToListAsync();

        Assert.Equal(2, filteredExecutions.Count);
        Assert.All(filteredExecutions, e =>
        {
            Assert.True(e.StartedAt >= startDate);
            Assert.True(e.StartedAt <= endDate);
        });
    }

    #endregion

    #region Log Queries

    [Fact]
    public async Task QueryLogs_ByExecution_ReturnsAllLogs()
    {
        _fixture.ResetDatabase();

        var workflow = WorkflowTestDataFactory.CreateLinearWorkflow(
            _fixture.TestOrganization.Id,
            _fixture.TestUser.Id);

        _fixture.DbContext.Workflows.Add(workflow);
        await _fixture.DbContext.SaveChangesAsync();

        var execution = await CreateExecutionWithStatus(workflow.Id, ExecutionStatus.Completed);

        await CreateLogForExecution(execution.Id, LogLevel.Info, "Log 1");
        await CreateLogForExecution(execution.Id, LogLevel.Info, "Log 2");
        await CreateLogForExecution(execution.Id, LogLevel.Warning, "Log 3");
        await CreateLogForExecution(execution.Id, LogLevel.Error, "Log 4");

        var logs = await _fixture.DbContext.ExecutionLogs
            .Where(l => l.ExecutionId == execution.Id)
            .OrderBy(l => l.Timestamp)
            .ToListAsync();

        Assert.Equal(4, logs.Count);
        Assert.Equal("Log 1", logs[0].Message);
        Assert.Equal("Log 4", logs[3].Message);
    }

    [Fact]
    public async Task QueryLogs_ByLogLevel_FiltersCorrectly()
    {
        _fixture.ResetDatabase();

        var workflow = WorkflowTestDataFactory.CreateLinearWorkflow(
            _fixture.TestOrganization.Id,
            _fixture.TestUser.Id);

        _fixture.DbContext.Workflows.Add(workflow);
        await _fixture.DbContext.SaveChangesAsync();

        var execution = await CreateExecutionWithStatus(workflow.Id, ExecutionStatus.Completed);

        await CreateLogForExecution(execution.Id, LogLevel.Info, "Info log");
        await CreateLogForExecution(execution.Id, LogLevel.Warning, "Warning log 1");
        await CreateLogForExecution(execution.Id, LogLevel.Warning, "Warning log 2");
        await CreateLogForExecution(execution.Id, LogLevel.Error, "Error log");

        var warningLogs = await _fixture.DbContext.ExecutionLogs
            .Where(l => l.ExecutionId == execution.Id && l.Level == LogLevel.Warning)
            .ToListAsync();

        var errorLogs = await _fixture.DbContext.ExecutionLogs
            .Where(l => l.ExecutionId == execution.Id && l.Level == LogLevel.Error)
            .ToListAsync();

        Assert.Equal(2, warningLogs.Count);
        Assert.Single(errorLogs);
        Assert.All(warningLogs, l => Assert.Equal(LogLevel.Warning, l.Level));
        Assert.All(errorLogs, l => Assert.Equal(LogLevel.Error, l.Level));
    }

    [Fact]
    public async Task QueryLogs_WithPagination_ReturnsCorrectPage()
    {
        _fixture.ResetDatabase();

        var workflow = WorkflowTestDataFactory.CreateLinearWorkflow(
            _fixture.TestOrganization.Id,
            _fixture.TestUser.Id);

        _fixture.DbContext.Workflows.Add(workflow);
        await _fixture.DbContext.SaveChangesAsync();

        var execution = await CreateExecutionWithStatus(workflow.Id, ExecutionStatus.Completed);

        for (int i = 0; i < 50; i++)
        {
            await CreateLogForExecution(execution.Id, LogLevel.Info, $"Log {i}");
        }

        var pageSize = 20;
        var page1 = await _fixture.DbContext.ExecutionLogs
            .Where(l => l.ExecutionId == execution.Id)
            .OrderBy(l => l.Timestamp)
            .Take(pageSize)
            .ToListAsync();

        var page2 = await _fixture.DbContext.ExecutionLogs
            .Where(l => l.ExecutionId == execution.Id)
            .OrderBy(l => l.Timestamp)
            .Skip(pageSize)
            .Take(pageSize)
            .ToListAsync();

        Assert.Equal(20, page1.Count);
        Assert.Equal(20, page2.Count);
        Assert.NotEqual(page1[0].Id, page2[0].Id);
    }

    [Fact]
    public async Task QueryLogs_ByNodeId_ReturnsNodeSpecificLogs()
    {
        _fixture.ResetDatabase();

        var workflow = WorkflowTestDataFactory.CreateLinearWorkflow(
            _fixture.TestOrganization.Id,
            _fixture.TestUser.Id);

        _fixture.DbContext.Workflows.Add(workflow);
        await _fixture.DbContext.SaveChangesAsync();

        var execution = await CreateExecutionWithStatus(workflow.Id, ExecutionStatus.Completed);

        await CreateLogForExecution(execution.Id, LogLevel.Info, "Node 1 log", "node-1");
        await CreateLogForExecution(execution.Id, LogLevel.Info, "Node 1 log 2", "node-1");
        await CreateLogForExecution(execution.Id, LogLevel.Info, "Node 2 log", "node-2");

        var node1Logs = await _fixture.DbContext.ExecutionLogs
            .Where(l => l.ExecutionId == execution.Id && l.NodeId == "node-1")
            .ToListAsync();

        var node2Logs = await _fixture.DbContext.ExecutionLogs
            .Where(l => l.ExecutionId == execution.Id && l.NodeId == "node-2")
            .ToListAsync();

        Assert.Equal(2, node1Logs.Count);
        Assert.Single(node2Logs);
        Assert.All(node1Logs, l => Assert.Equal("node-1", l.NodeId));
        Assert.All(node2Logs, l => Assert.Equal("node-2", l.NodeId));
    }

    #endregion

    #region Statistics Queries

    [Fact]
    public async Task QueryStatistics_SuccessRate_CalculatesCorrectly()
    {
        _fixture.ResetDatabase();

        var workflow = WorkflowTestDataFactory.CreateLinearWorkflow(
            _fixture.TestOrganization.Id,
            _fixture.TestUser.Id);

        _fixture.DbContext.Workflows.Add(workflow);
        await _fixture.DbContext.SaveChangesAsync();

        await CreateExecutionWithStatus(workflow.Id, ExecutionStatus.Completed);
        await CreateExecutionWithStatus(workflow.Id, ExecutionStatus.Completed);
        await CreateExecutionWithStatus(workflow.Id, ExecutionStatus.Completed);
        await CreateExecutionWithStatus(workflow.Id, ExecutionStatus.Completed);
        await CreateExecutionWithStatus(workflow.Id, ExecutionStatus.Failed);

        var totalCount = await _fixture.DbContext.WorkflowExecutions
            .CountAsync(e => e.WorkflowId == workflow.Id);

        var successCount = await _fixture.DbContext.WorkflowExecutions
            .CountAsync(e => e.WorkflowId == workflow.Id && e.Status == ExecutionStatus.Completed);

        var successRate = (double)successCount / totalCount * 100;

        Assert.Equal(5, totalCount);
        Assert.Equal(4, successCount);
        Assert.Equal(80.0, successRate);
    }

    [Fact]
    public async Task QueryStatistics_AverageDuration_CalculatesCorrectly()
    {
        _fixture.ResetDatabase();

        var workflow = WorkflowTestDataFactory.CreateLinearWorkflow(
            _fixture.TestOrganization.Id,
            _fixture.TestUser.Id);

        _fixture.DbContext.Workflows.Add(workflow);
        await _fixture.DbContext.SaveChangesAsync();

        await CreateExecutionWithDuration(workflow.Id, 1000);
        await CreateExecutionWithDuration(workflow.Id, 2000);
        await CreateExecutionWithDuration(workflow.Id, 3000);
        await CreateExecutionWithDuration(workflow.Id, 4000);

        var avgDuration = await _fixture.DbContext.WorkflowExecutions
            .Where(e => e.WorkflowId == workflow.Id && e.Duration.HasValue)
            .Select(e => e.Duration!.Value.TotalMilliseconds)
            .AverageAsync();

        Assert.Equal(2500.0, avgDuration);
    }

    [Fact]
    public async Task QueryStatistics_ExecutionsPerDay_GroupsCorrectly()
    {
        _fixture.ResetDatabase();

        var workflow = WorkflowTestDataFactory.CreateLinearWorkflow(
            _fixture.TestOrganization.Id,
            _fixture.TestUser.Id);

        _fixture.DbContext.Workflows.Add(workflow);
        await _fixture.DbContext.SaveChangesAsync();

        var today = DateTime.UtcNow.Date;
        await CreateExecutionWithTimestamp(workflow.Id, today.AddHours(10));
        await CreateExecutionWithTimestamp(workflow.Id, today.AddHours(14));
        await CreateExecutionWithTimestamp(workflow.Id, today.AddDays(-1).AddHours(10));
        await CreateExecutionWithTimestamp(workflow.Id, today.AddDays(-2).AddHours(10));
        await CreateExecutionWithTimestamp(workflow.Id, today.AddDays(-2).AddHours(15));

        var executionsPerDay = await _fixture.DbContext.WorkflowExecutions
            .Where(e => e.WorkflowId == workflow.Id)
            .GroupBy(e => e.StartedAt.Date)
            .Select(g => new
            {
                Date = g.Key,
                Count = g.Count()
            })
            .OrderByDescending(x => x.Date)
            .ToListAsync();

        Assert.Equal(3, executionsPerDay.Count);
        Assert.Equal(2, executionsPerDay[0].Count);
        Assert.Equal(1, executionsPerDay[1].Count);
        Assert.Equal(2, executionsPerDay[2].Count);
    }

    #endregion

    #region Performance Tests

    [Fact]
    public async Task Performance_Query100Executions_CompletesQuickly()
    {
        _fixture.ResetDatabase();

        var workflow = WorkflowTestDataFactory.CreateLinearWorkflow(
            _fixture.TestOrganization.Id,
            _fixture.TestUser.Id);

        _fixture.DbContext.Workflows.Add(workflow);
        await _fixture.DbContext.SaveChangesAsync();

        await CreateExecutionsForWorkflow(workflow.Id, 100);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        var executions = await _fixture.DbContext.WorkflowExecutions
            .Where(e => e.WorkflowId == workflow.Id)
            .OrderByDescending(e => e.StartedAt)
            .ToListAsync();

        stopwatch.Stop();

        Assert.Equal(100, executions.Count);
        Assert.True(stopwatch.ElapsedMilliseconds < 1000,
            $"Query took {stopwatch.ElapsedMilliseconds}ms, expected < 1000ms");
    }

    [Fact]
    public async Task Performance_Query100ExecutionsWithLogs_CompletesQuickly()
    {
        _fixture.ResetDatabase();

        var workflow = WorkflowTestDataFactory.CreateLinearWorkflow(
            _fixture.TestOrganization.Id,
            _fixture.TestUser.Id);

        _fixture.DbContext.Workflows.Add(workflow);
        await _fixture.DbContext.SaveChangesAsync();

        for (int i = 0; i < 100; i++)
        {
            var execution = await CreateExecutionWithStatus(workflow.Id, ExecutionStatus.Completed);
            await CreateLogForExecution(execution.Id, LogLevel.Info, $"Log {i}");
        }

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        var executions = await _fixture.DbContext.WorkflowExecutions
            .Include(e => e.Logs)
            .Where(e => e.WorkflowId == workflow.Id)
            .ToListAsync();

        stopwatch.Stop();

        Assert.Equal(100, executions.Count);
        Assert.All(executions, e => Assert.NotEmpty(e.Logs));
        Assert.True(stopwatch.ElapsedMilliseconds < 2000,
            $"Query took {stopwatch.ElapsedMilliseconds}ms, expected < 2000ms");
    }

    [Fact]
    public async Task Performance_FilterAndPaginate1000Executions_CompletesQuickly()
    {
        _fixture.ResetDatabase();

        var workflow = WorkflowTestDataFactory.CreateLinearWorkflow(
            _fixture.TestOrganization.Id,
            _fixture.TestUser.Id);

        _fixture.DbContext.Workflows.Add(workflow);
        await _fixture.DbContext.SaveChangesAsync();

        for (int i = 0; i < 1000; i++)
        {
            var status = i % 3 == 0 ? ExecutionStatus.Failed : ExecutionStatus.Completed;
            await CreateExecutionWithStatus(workflow.Id, status);
        }

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        var filteredPage = await _fixture.DbContext.WorkflowExecutions
            .Where(e => e.WorkflowId == workflow.Id && e.Status == ExecutionStatus.Completed)
            .OrderByDescending(e => e.StartedAt)
            .Skip(0)
            .Take(50)
            .ToListAsync();

        stopwatch.Stop();

        Assert.Equal(50, filteredPage.Count);
        Assert.True(stopwatch.ElapsedMilliseconds < 1000,
            $"Query took {stopwatch.ElapsedMilliseconds}ms, expected < 1000ms");
    }

    #endregion

    #region Helper Methods

    private async Task CreateExecutionsForWorkflow(Guid workflowId, int count)
    {
        for (int i = 0; i < count; i++)
        {
            await CreateExecutionWithStatus(workflowId, ExecutionStatus.Completed);
        }
    }

    private async Task<Core.Entities.WorkflowExecution> CreateExecutionWithStatus(
        Guid workflowId,
        ExecutionStatus status)
    {
        var execution = new Core.Entities.WorkflowExecution
        {
            Id = Guid.NewGuid(),
            WorkflowId = workflowId,
            Status = status,
            TriggerType = ExecutionTrigger.Manual,
            StartedAt = DateTime.UtcNow,
            CompletedAt = status != ExecutionStatus.Running ? DateTime.UtcNow.AddSeconds(5) : null,
            Duration = status != ExecutionStatus.Running ? TimeSpan.FromSeconds(5) : null,
            InputDataJson = "{}",
            OutputDataJson = status == ExecutionStatus.Completed ? "{}" : null,
            ErrorDataJson = status == ExecutionStatus.Failed ? "{\"error\":\"Test error\"}" : null
        };

        _fixture.DbContext.WorkflowExecutions.Add(execution);
        await _fixture.DbContext.SaveChangesAsync();
        return execution;
    }

    private async Task<Core.Entities.WorkflowExecution> CreateExecutionWithTimestamp(
        Guid workflowId,
        DateTime timestamp)
    {
        var execution = new Core.Entities.WorkflowExecution
        {
            Id = Guid.NewGuid(),
            WorkflowId = workflowId,
            Status = ExecutionStatus.Completed,
            TriggerType = ExecutionTrigger.Manual,
            StartedAt = timestamp,
            CompletedAt = timestamp.AddSeconds(5),
            Duration = TimeSpan.FromSeconds(5),
            InputDataJson = "{}",
            OutputDataJson = "{}"
        };

        _fixture.DbContext.WorkflowExecutions.Add(execution);
        await _fixture.DbContext.SaveChangesAsync();
        return execution;
    }

    private async Task<Core.Entities.WorkflowExecution> CreateExecutionWithDuration(
        Guid workflowId,
        int durationMs)
    {
        var execution = new Core.Entities.WorkflowExecution
        {
            Id = Guid.NewGuid(),
            WorkflowId = workflowId,
            Status = ExecutionStatus.Completed,
            TriggerType = ExecutionTrigger.Manual,
            StartedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow.AddMilliseconds(durationMs),
            Duration = TimeSpan.FromMilliseconds(durationMs),
            InputDataJson = "{}",
            OutputDataJson = "{}"
        };

        _fixture.DbContext.WorkflowExecutions.Add(execution);
        await _fixture.DbContext.SaveChangesAsync();
        return execution;
    }

    private async Task<Core.Entities.ExecutionLog> CreateLogForExecution(
        Guid executionId,
        LogLevel level,
        string message,
        string? nodeId = null)
    {
        var log = new Core.Entities.ExecutionLog
        {
            Id = Guid.NewGuid(),
            ExecutionId = executionId,
            Level = level,
            Message = message,
            NodeId = nodeId,
            Timestamp = DateTime.UtcNow
        };

        _fixture.DbContext.ExecutionLogs.Add(log);
        await _fixture.DbContext.SaveChangesAsync();
        return log;
    }

    #endregion
}