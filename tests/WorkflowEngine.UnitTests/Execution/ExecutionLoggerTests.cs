using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using WorkflowEngine.Core.Enums;
using WorkflowEngine.Execution.Logging;
using WorkflowEngine.Infrastructure.Data;
using WorkflowEngine.Nodes.Models;
using LogLevel = WorkflowEngine.Core.Enums.LogLevel;

namespace WorkflowEngine.UnitTests.Execution;

public class ExecutionLoggerTests : IDisposable
{
    private readonly WorkflowEngineDbContext _dbContext;
    private readonly ExecutionLogger _logger;
    private readonly Mock<ILogger<ExecutionLogger>> _loggerMock;

    public ExecutionLoggerTests()
    {
        var options = new DbContextOptionsBuilder<WorkflowEngineDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new WorkflowEngineDbContext(options);
        _loggerMock = new Mock<ILogger<ExecutionLogger>>();
        _logger = new ExecutionLogger(_dbContext, _loggerMock.Object);
    }

    [Fact]
    public async Task LogExecutionStartAsync_CreatesLogEntry()
    {
        var executionId = Guid.NewGuid();
        var workflowId = Guid.NewGuid();

        await _logger.LogExecutionStartAsync(executionId, workflowId);

        var logs = await _dbContext.ExecutionLogs.ToListAsync();
        Assert.Single(logs);
        Assert.Equal(executionId, logs[0].ExecutionId);
        Assert.Null(logs[0].NodeId);
        Assert.Equal(LogLevel.Info, logs[0].Level);
        Assert.Contains("started", logs[0].Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LogExecutionCompleteAsync_CompletedStatus_CreatesInfoLog()
    {
        var executionId = Guid.NewGuid();
        var duration = TimeSpan.FromSeconds(5);

        await _logger.LogExecutionCompleteAsync(executionId, ExecutionStatus.Completed, duration);

        var logs = await _dbContext.ExecutionLogs.ToListAsync();
        Assert.Single(logs);
        Assert.Equal(LogLevel.Info, logs[0].Level);
        Assert.Contains("completed", logs[0].Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LogExecutionCompleteAsync_FailedStatus_CreatesErrorLog()
    {
        var executionId = Guid.NewGuid();
        var duration = TimeSpan.FromSeconds(3);

        await _logger.LogExecutionCompleteAsync(executionId, ExecutionStatus.Failed, duration);

        var logs = await _dbContext.ExecutionLogs.ToListAsync();
        Assert.Single(logs);
        Assert.Equal(LogLevel.Error, logs[0].Level);
        Assert.Contains("failed", logs[0].Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LogNodeExecutionAsync_SuccessfulExecution_CreatesInfoLog()
    {
        var executionId = Guid.NewGuid();
        var nodeId = "node1";
        var nodeType = "http_request";
        var duration = TimeSpan.FromMilliseconds(500);

        var result = new NodeExecutionResult
        {
            Success = true,
            OutputData = new Dictionary<string, object>
            {
                ["status"] = 200,
                ["data"] = "test"
            }
        };

        await _logger.LogNodeExecutionAsync(executionId, nodeId, nodeType, result, duration);

        var logs = await _dbContext.ExecutionLogs.ToListAsync();
        Assert.Single(logs);
        Assert.Equal(executionId, logs[0].ExecutionId);
        Assert.Equal(nodeId, logs[0].NodeId);
        Assert.Equal(LogLevel.Info, logs[0].Level);
        Assert.Contains("successfully", logs[0].Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LogNodeExecutionAsync_FailedExecution_CreatesErrorLog()
    {
        var executionId = Guid.NewGuid();
        var nodeId = "node1";
        var nodeType = "http_request";
        var duration = TimeSpan.FromMilliseconds(300);

        var result = new NodeExecutionResult
        {
            Success = false,
            ErrorMessage = "HTTP 500 Internal Server Error"
        };

        await _logger.LogNodeExecutionAsync(executionId, nodeId, nodeType, result, duration);

        var logs = await _dbContext.ExecutionLogs.ToListAsync();
        Assert.Single(logs);
        Assert.Equal(LogLevel.Error, logs[0].Level);
        Assert.Contains("failed", logs[0].Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("500", logs[0].Message);
    }

    [Fact]
    public async Task LogErrorAsync_WithException_StoresExceptionDetails()
    {
        var executionId = Guid.NewGuid();
        var nodeId = "node1";
        var message = "An error occurred";
        var exception = new InvalidOperationException("Test exception");

        await _logger.LogErrorAsync(executionId, nodeId, message, exception);

        var logs = await _dbContext.ExecutionLogs.ToListAsync();
        Assert.Single(logs);
        Assert.Equal(LogLevel.Error, logs[0].Level);
        Assert.Equal(nodeId, logs[0].NodeId);
    }

    [Fact]
    public async Task LogInfoAsync_WithDetails_StoresDetails()
    {
        var executionId = Guid.NewGuid();
        var nodeId = "node1";
        var message = "Processing data";
        var details = new Dictionary<string, object>
        {
            ["itemCount"] = 10,
            ["status"] = "processing"
        };

        await _logger.LogInfoAsync(executionId, nodeId, message, details);

        var logs = await _dbContext.ExecutionLogs.ToListAsync();
        Assert.Single(logs);
        Assert.Equal(LogLevel.Info, logs[0].Level);
        Assert.Equal(message, logs[0].Message);
    }

    [Fact]
    public async Task GetExecutionLogsAsync_ReturnsLogsInOrder()
    {
        var executionId = Guid.NewGuid();

        await _logger.LogExecutionStartAsync(executionId, Guid.NewGuid());
        await Task.Delay(10);
        await _logger.LogInfoAsync(executionId, "node1", "Processing");
        await Task.Delay(10);
        await _logger.LogExecutionCompleteAsync(executionId, ExecutionStatus.Completed, TimeSpan.FromSeconds(1));

        var logs = await _logger.GetExecutionLogsAsync(executionId);

        Assert.Equal(3, logs.Count);
        Assert.True(logs[0].Timestamp < logs[1].Timestamp);
        Assert.True(logs[1].Timestamp < logs[2].Timestamp);
    }

    [Fact]
    public async Task GetNodeLogsAsync_ReturnsOnlyNodeLogs()
    {
        var executionId = Guid.NewGuid();
        var nodeId = "node1";

        await _logger.LogInfoAsync(executionId, nodeId, "Node log 1");
        await _logger.LogInfoAsync(executionId, "node2", "Other node log");
        await _logger.LogInfoAsync(executionId, nodeId, "Node log 2");

        var logs = await _logger.GetNodeLogsAsync(executionId, nodeId);

        Assert.Equal(2, logs.Count);
        Assert.All(logs, log => Assert.Equal(nodeId, log.NodeId));
    }

    [Fact]
    public async Task DeleteOldLogsAsync_RemovesOldLogs()
    {
        var executionId = Guid.NewGuid();

        await _logger.LogInfoAsync(executionId, null, "Old log");

        var log = await _dbContext.ExecutionLogs.FirstAsync();
        log.Timestamp = DateTime.UtcNow.AddDays(-31);
        await _dbContext.SaveChangesAsync();

        await _logger.LogInfoAsync(executionId, null, "Recent log");

        await _logger.DeleteOldLogsAsync(TimeSpan.FromDays(30));

        var remainingLogs = await _dbContext.ExecutionLogs.ToListAsync();
        Assert.Single(remainingLogs);
        Assert.Contains("Recent log", remainingLogs[0].Message);
    }

    [Fact]
    public async Task LogExecutionStartAsync_MultipleExecutions_CreatesMultipleLogs()
    {
        var execution1 = Guid.NewGuid();
        var execution2 = Guid.NewGuid();

        await _logger.LogExecutionStartAsync(execution1, Guid.NewGuid());
        await _logger.LogExecutionStartAsync(execution2, Guid.NewGuid());

        var logs = await _dbContext.ExecutionLogs.ToListAsync();
        Assert.Equal(2, logs.Count);
        Assert.Contains(logs, l => l.ExecutionId == execution1);
        Assert.Contains(logs, l => l.ExecutionId == execution2);
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }
}