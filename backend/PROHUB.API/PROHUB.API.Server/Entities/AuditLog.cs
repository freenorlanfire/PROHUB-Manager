namespace PROHUB.API.Server.Entities;

public class AuditLog
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? UserId { get; set; }
    public string EntityType { get; set; } = "";
    public Guid? EntityId { get; set; }
    public string Action { get; set; } = "";
    public string? Details { get; set; }   // JSON string
    public DateTime CreatedAtUtc { get; set; }
}
