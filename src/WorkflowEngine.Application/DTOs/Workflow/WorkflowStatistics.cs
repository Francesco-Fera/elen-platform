namespace WorkflowEngine.Application.DTOs.Workflow;

public class WorkflowStatistics
{
    public Guid WorkflowId { get; set; }
    public string WorkflowName { get; set; } = string.Empty;

    // Execution statistics
    public int TotalExecutions { get; set; }
    public int SuccessfulExecutions { get; set; }
    public int FailedExecutions { get; set; }
    public double SuccessRate { get; set; }

    // Performance metrics
    public double? AverageExecutionTimeMs { get; set; }
    public double? MedianExecutionTimeMs { get; set; }
    public double? MinExecutionTimeMs { get; set; }
    public double? MaxExecutionTimeMs { get; set; }

    // Recent activity
    public DateTime? LastExecutedAt { get; set; }
    public DateTime? FirstExecutedAt { get; set; }

    // Time-based statistics
    public int ExecutionsLast24Hours { get; set; }
    public int ExecutionsLast7Days { get; set; }
    public int ExecutionsLast30Days { get; set; }

    // Node statistics
    public List<NodeExecutionStats> NodeStats { get; set; } = new();
}

public class NodeExecutionStats
{
    public string NodeId { get; set; } = string.Empty;
    public string NodeType { get; set; } = string.Empty;
    public string NodeName { get; set; } = string.Empty;
    public int ExecutionCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public double SuccessRate { get; set; }
    public double? AverageExecutionTimeMs { get; set; }
}