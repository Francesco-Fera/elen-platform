using WorkflowEngine.Core.Enums;

namespace WorkflowEngine.Core.Entities;

public class ExecutionLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ExecutionId { get; set; }

    public LogLevel Level { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? NodeId { get; set; }
    public string? DataJson { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual WorkflowExecution Execution { get; set; } = null!;
}