namespace PROHUB.API.Server.Entities;

public class ProjectLink
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Label { get; set; } = "";
    public string Url { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }

    public Project? Project { get; set; }
}
