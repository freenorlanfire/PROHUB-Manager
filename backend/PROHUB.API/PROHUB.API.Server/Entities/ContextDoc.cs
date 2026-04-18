namespace PROHUB.API.Server.Entities;

public class ContextDoc
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Content { get; set; } = "";
    public DateTime UpdatedAtUtc { get; set; }
    public string? UpdatedBy { get; set; }

    public Project? Project { get; set; }
    public ICollection<ContextDocVersion> Versions { get; set; } = [];
}
