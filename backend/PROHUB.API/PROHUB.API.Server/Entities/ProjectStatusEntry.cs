namespace PROHUB.API.Server.Entities;

public class ProjectStatusEntry
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Status { get; set; } = "";
    public string? Note { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public Project? Project { get; set; }
}
