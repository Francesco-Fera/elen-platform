using WorkflowEngine.Application.DTOs.Workflow;

namespace WorkflowEngine.Application.Exceptions;

public class WorkflowNotFoundException : Exception
{
    public WorkflowNotFoundException(Guid workflowId)
        : base($"Workflow with ID {workflowId} was not found.")
    {
    }
}

public class WorkflowAccessDeniedException : Exception
{
    public WorkflowAccessDeniedException(Guid workflowId)
        : base($"Access denied to workflow with ID {workflowId}.")
    {
    }
}

public class WorkflowValidationException : Exception
{
    public List<ValidationError> ValidationErrors { get; }

    public WorkflowValidationException(List<ValidationError> errors)
        : base("Workflow validation failed.")
    {
        ValidationErrors = errors;
    }
}

public class InvalidWorkflowOperationException : Exception
{
    public InvalidWorkflowOperationException(string message) : base(message)
    {
    }
}