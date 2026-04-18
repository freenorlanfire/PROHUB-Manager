namespace PROHUB.API.Server.Entities;

public class Membership
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid UserId { get; set; }
    public string Role { get; set; } = "member";
    public DateTime CreatedAtUtc { get; set; }

    public Company? Company { get; set; }
    public User? User { get; set; }
}
