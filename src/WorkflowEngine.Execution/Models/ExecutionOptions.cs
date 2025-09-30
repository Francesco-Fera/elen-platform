namespace WorkflowEngine.Execution.Models;

public class ExecutionOptions
{
    public bool ContinueOnError { get; set; } = false;
    public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(5);
    public int MaxRetries { get; set; } = 0;
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(5);
    public bool EnableParallelExecution { get; set; } = true;
    public TimeSpan NodeTimeout { get; set; } = TimeSpan.FromMinutes(1);
}