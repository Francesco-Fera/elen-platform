using Microsoft.EntityFrameworkCore;
using WorkflowEngine.Core.Entities;
using WorkflowEngine.Core.Enums;
using WorkflowEngine.Execution.Models;
using WorkflowEngine.Infrastructure.Data;

namespace WorkflowEngine.IntegrationTests.Helpers;

public static class TestExecutionHelper
{
    public static async Task AssertExecutionCompletedSuccessfully(
        WorkflowEngineDbContext dbContext,
        ExecutionResult result)
    {
        Assert.True(result.IsSuccess, $"Execution failed: {result.ErrorMessage}");
        Assert.Equal(ExecutionStatus.Completed, result.Status);
        Assert.NotEqual(TimeSpan.Zero, result.Duration);

        var execution = await dbContext.WorkflowExecutions
            .FirstOrDefaultAsync(e => e.Id == result.ExecutionId);

        Assert.NotNull(execution);
        Assert.Equal(ExecutionStatus.Completed, execution.Status);
        Assert.NotNull(execution.CompletedAt);
        Assert.NotNull(execution.OutputDataJson);
        Assert.True(execution.Duration.HasValue);
    }

    public static async Task AssertExecutionFailed(
        WorkflowEngineDbContext dbContext,
        ExecutionResult result,
        string? expectedErrorMessageContains = null)
    {
        Assert.False(result.IsSuccess);
        Assert.Equal(ExecutionStatus.Failed, result.Status);
        Assert.NotNull(result.ErrorMessage);

        if (expectedErrorMessageContains != null)
        {
            Assert.Contains(expectedErrorMessageContains, result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        }

        var execution = await dbContext.WorkflowExecutions
            .FirstOrDefaultAsync(e => e.Id == result.ExecutionId);

        Assert.NotNull(execution);
        Assert.Equal(ExecutionStatus.Failed, execution.Status);
        Assert.NotNull(execution.ErrorDataJson);
    }

    public static async Task AssertExecutionCancelled(
        WorkflowEngineDbContext dbContext,
        ExecutionResult result)
    {
        Assert.Equal(ExecutionStatus.Cancelled, result.Status);

        var execution = await dbContext.WorkflowExecutions
            .FirstOrDefaultAsync(e => e.Id == result.ExecutionId);

        Assert.NotNull(execution);
        Assert.Equal(ExecutionStatus.Cancelled, execution.Status);
        Assert.NotNull(execution.CompletedAt);
    }

    public static async Task AssertExecutionTimeout(
        WorkflowEngineDbContext dbContext,
        ExecutionResult result)
    {
        Assert.Equal(ExecutionStatus.Timeout, result.Status);

        var execution = await dbContext.WorkflowExecutions
            .FirstOrDefaultAsync(e => e.Id == result.ExecutionId);

        Assert.NotNull(execution);
        Assert.Equal(ExecutionStatus.Timeout, execution.Status);
        Assert.NotNull(execution.CompletedAt);
    }

    public static void AssertNodeExecuted(ExecutionResult result, string nodeId, bool shouldSucceed = true)
    {
        var nodeExecution = result.NodeExecutions.FirstOrDefault(n => n.NodeId == nodeId);
        Assert.NotNull(nodeExecution);
        Assert.Equal(shouldSucceed, nodeExecution.Success);
        Assert.NotEqual(TimeSpan.Zero, nodeExecution.Duration);
    }

    public static void AssertNodeNotExecuted(ExecutionResult result, string nodeId)
    {
        var nodeExecution = result.NodeExecutions.FirstOrDefault(n => n.NodeId == nodeId);
        Assert.Null(nodeExecution);
    }

    public static void AssertNodesExecutedInOrder(ExecutionResult result, params string[] nodeIds)
    {
        Assert.Equal(nodeIds.Length, result.NodeExecutions.Count);

        var executedNodes = result.NodeExecutions
            .OrderBy(n => n.StartedAt)
            .Select(n => n.NodeId)
            .ToList();

        for (int i = 0; i < nodeIds.Length; i++)
        {
            Assert.Equal(nodeIds[i], executedNodes[i]);
        }
    }

    public static async Task<List<ExecutionLog>> GetExecutionLogs(
        WorkflowEngineDbContext dbContext,
        Guid executionId)
    {
        return await dbContext.ExecutionLogs
            .Where(l => l.ExecutionId == executionId)
            .OrderBy(l => l.Timestamp)
            .ToListAsync();
    }

    public static async Task AssertExecutionLogsExist(
        WorkflowEngineDbContext dbContext,
        Guid executionId,
        int minimumLogCount = 1)
    {
        var logs = await GetExecutionLogs(dbContext, executionId);
        Assert.True(logs.Count >= minimumLogCount,
            $"Expected at least {minimumLogCount} logs, but found {logs.Count}");
    }

    public static async Task AssertLogLevelExists(
        WorkflowEngineDbContext dbContext,
        Guid executionId,
        LogLevel level)
    {
        var logs = await GetExecutionLogs(dbContext, executionId);
        var hasLevel = logs.Any(l => l.Level == level);
        Assert.True(hasLevel, $"No log with level {level} found");
    }

    public static async Task AssertNodeLogExists(
        WorkflowEngineDbContext dbContext,
        Guid executionId,
        string nodeId)
    {
        var logs = await GetExecutionLogs(dbContext, executionId);
        var nodeLog = logs.Any(l => l.NodeId == nodeId);
        Assert.True(nodeLog, $"No log for node {nodeId} found");
    }

    public static void AssertOutputDataContains(ExecutionResult result, string key)
    {
        Assert.True(result.OutputData.ContainsKey(key),
            $"Output data does not contain key: {key}");
    }

    public static void AssertOutputDataValue<T>(ExecutionResult result, string key, T expectedValue)
    {
        Assert.True(result.OutputData.ContainsKey(key),
            $"Output data does not contain key: {key}");

        var actualValue = result.OutputData[key];
        Assert.Equal(expectedValue, actualValue);
    }

    public static async Task<WorkflowExecution?> GetExecution(
        WorkflowEngineDbContext dbContext,
        Guid executionId)
    {
        return await dbContext.WorkflowExecutions
            .Include(e => e.Logs)
            .FirstOrDefaultAsync(e => e.Id == executionId);
    }

    public static async Task<int> CountExecutionsForWorkflow(
        WorkflowEngineDbContext dbContext,
        Guid workflowId)
    {
        return await dbContext.WorkflowExecutions
            .CountAsync(e => e.WorkflowId == workflowId);
    }

    public static async Task<int> CountExecutionsByStatus(
        WorkflowEngineDbContext dbContext,
        Guid workflowId,
        ExecutionStatus status)
    {
        return await dbContext.WorkflowExecutions
            .CountAsync(e => e.WorkflowId == workflowId && e.Status == status);
    }

    public static void AssertExecutionDuration(ExecutionResult result, TimeSpan maxDuration)
    {
        Assert.True(result.Duration <= maxDuration,
            $"Execution took {result.Duration.TotalSeconds}s, expected max {maxDuration.TotalSeconds}s");
    }

    public static void AssertAllNodesSucceeded(ExecutionResult result)
    {
        Assert.All(result.NodeExecutions, ne => Assert.True(ne.Success,
            $"Node {ne.NodeId} failed: {ne.ErrorMessage}"));
    }

    public static void AssertAtLeastOneNodeFailed(ExecutionResult result)
    {
        Assert.True(result.HasFailedNodes,
            "Expected at least one node to fail, but all succeeded");
    }
}