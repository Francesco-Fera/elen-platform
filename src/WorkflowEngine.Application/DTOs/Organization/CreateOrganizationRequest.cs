using System.ComponentModel.DataAnnotations;

namespace WorkflowEngine.Application.DTOs.Organization;

public class CreateOrganizationRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }
}
