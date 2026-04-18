namespace PROHUB.API.Server.Entities;

public class Project
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? Description { get; set; }
    public string Status { get; set; } = "active";
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public DateTime? ArchivedAtUtc { get; set; }

    public Company? Company { get; set; }
    public ICollection<ProjectStatusEntry> StatusEntries { get; set; } = [];
    public ICollection<IntegrationLink> IntegrationLinks { get; set; } = [];
    public ICollection<ProjectTag> Tags { get; set; } = [];
    public ICollection<ProjectLink> Links { get; set; } = [];
    public ContextDoc? ContextDoc { get; set; }
}
