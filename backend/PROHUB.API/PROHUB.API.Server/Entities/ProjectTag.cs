namespace PROHUB.API.Server.Entities;

public class ProjectTag
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Tag { get; set; } = "";

    public Project? Project { get; set; }
}
