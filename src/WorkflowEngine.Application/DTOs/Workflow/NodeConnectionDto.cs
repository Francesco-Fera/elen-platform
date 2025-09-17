namespace WorkflowEngine.Application.DTOs.Workflow;

public class NodeConnectionDto
{
    public string SourceNodeId { get; set; } = string.Empty;
    public string TargetNodeId { get; set; } = string.Empty;
    public string SourceOutput { get; set; } = "default";
    public string TargetInput { get; set; } = "default";
}
