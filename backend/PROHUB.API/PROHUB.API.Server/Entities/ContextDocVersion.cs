namespace PROHUB.API.Server.Entities;

public class ContextDocVersion
{
    public Guid Id { get; set; }
    public Guid ContextDocId { get; set; }
    public Guid ProjectId { get; set; }
    public string Content { get; set; } = "";
    public int Version { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }

    public ContextDoc? ContextDoc { get; set; }
}
