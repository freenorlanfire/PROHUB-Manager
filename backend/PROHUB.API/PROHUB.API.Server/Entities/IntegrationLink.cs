namespace PROHUB.API.Server.Entities;

/// <summary>Typed integration links: repo, ci, staging, prod, docs, etc.</summary>
public class IntegrationLink
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Type { get; set; } = "";   // repo | ci | staging | prod | docs
    public string Url { get; set; } = "";
    public string? Label { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public Project? Project { get; set; }
}
