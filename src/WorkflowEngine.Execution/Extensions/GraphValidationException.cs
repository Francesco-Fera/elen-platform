namespace WorkflowEngine.Execution.Exceptions;

public class GraphValidationException : Exception
{
    public List<string> ValidationErrors { get; }

    public GraphValidationException(string message, List<string> errors) : base(message)
    {
        ValidationErrors = errors ?? new List<string>();
    }

    public GraphValidationException(string message) : base(message)
    {
        ValidationErrors = new List<string> { message };
    }
}