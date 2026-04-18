using Microsoft.EntityFrameworkCore;
using PROHUB.API.Server.Data;
using PROHUB.API.Server.Entities;

namespace PROHUB.API.Server.Services;

public static class DataSeeder
{
    /// <summary>
    /// Idempotent seed: creates one company + two admin users + memberships.
    /// Skips everything if admin@prohub.dev already exists.
    /// Credentials: admin@prohub.dev / Admin123!  |  norlan@prohub.dev / Admin123!
    /// </summary>
    public static async Task SeedAsync(IServiceProvider services, ILogger logger)
    {
        await using var scope = services.CreateAsyncScope();
        var db   = scope.ServiceProvider.GetRequiredService<ProhubDbContext>();
        var auth = scope.ServiceProvider.GetRequiredService<AuthService>();

        const string email1    = "admin@prohub.dev";
        const string email2    = "norlan@prohub.dev";
        const string password  = "Admin123!";

        // Idempotent: skip if primary seed user already exists
        if (await db.Users.AnyAsync(u => u.Email == email1))
        {
            logger.LogInformation("[Seed] Users already seeded — skipping.");
            return;
        }

        logger.LogInformation("[Seed] Creating company and admin users...");

        var now = DateTime.UtcNow;

        var company = new Company
        {
            Id            = Guid.NewGuid(),
            Name          = "PROHUB Dev",
            Slug          = "prohub-dev",
            Description   = "Auto-seeded company",
            CreatedAtUtc  = now,
            UpdatedAtUtc  = now,
            IsDeleted     = false
        };

        var admin = new User
        {
            Id           = Guid.NewGuid(),
            Email        = email1,
            Name         = "Admin",
            PasswordHash = auth.HashPassword(password),
            Role         = "owner",
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            IsDeleted    = false
        };

        var norlan = new User
        {
            Id           = Guid.NewGuid(),
            Email        = email2,
            Name         = "Norlan",
            PasswordHash = auth.HashPassword(password),
            Role         = "owner",
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            IsDeleted    = false
        };

        db.Companies.Add(company);
        db.Users.AddRange(admin, norlan);
        db.Memberships.AddRange(
            new Membership { Id = Guid.NewGuid(), CompanyId = company.Id, UserId = admin.Id,  Role = "owner", CreatedAtUtc = now },
            new Membership { Id = Guid.NewGuid(), CompanyId = company.Id, UserId = norlan.Id, Role = "owner", CreatedAtUtc = now }
        );

        await db.SaveChangesAsync();

        logger.LogInformation(
            "[Seed] Done — company: {Company} | users: {E1}, {E2} | password: {Pwd}",
            company.Name, email1, email2, password);
    }
}
