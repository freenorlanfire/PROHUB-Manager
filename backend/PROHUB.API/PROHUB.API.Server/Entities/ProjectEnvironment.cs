namespace PROHUB.API.Server.Entities;

public class ProjectEnvironment
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = "";
    public string? Url { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public Project? Project { get; set; }
}
