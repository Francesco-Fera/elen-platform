namespace WorkflowEngine.Core.Entities;

public enum WorkflowStatus
{
    Draft = 0,
    Active = 1,
    Inactive = 2,
    Archived = 3
}

public enum ExecutionStatus
{
    Pending = 0,
    Running = 1,
    Success = 2,
    Failed = 3,
    Cancelled = 4
}

public enum ExecutionTrigger
{
    Manual = 0,
    Webhook = 1,
    Schedule = 2,
    Event = 3
}

public enum LogLevel
{
    Debug = 0,
    Info = 1,
    Warning = 2,
    Error = 3
}