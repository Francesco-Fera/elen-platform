namespace WorkflowEngine.Application.DTOs.Workflow;

public class WorkflowNodeDto
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public NodePositionDto Position { get; set; } = new();
    public Dictionary<string, object> Parameters { get; set; } = new();
    public Dictionary<string, object> Configuration { get; set; } = new();
}

public class NodePositionDto
{
    public double X { get; set; }
    public double Y { get; set; }
}
