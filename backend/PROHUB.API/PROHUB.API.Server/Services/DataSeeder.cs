using Microsoft.EntityFrameworkCore;
using PROHUB.API.Server.Data;
using PROHUB.API.Server.Entities;

namespace PROHUB.API.Server.Services;

public static class DataSeeder
{
    /// <summary>
    /// Idempotent dev seed: creates one company + admin user + membership.
    /// Only runs in Development environment.
    /// Credentials: admin@prohub.dev / Admin123!
    /// </summary>
    public static async Task SeedAsync(IServiceProvider services, ILogger logger)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ProhubDbContext>();
        var auth = scope.ServiceProvider.GetRequiredService<AuthService>();

        const string devEmail = "admin@prohub.dev";
        const string devPassword = "Admin123!";

        if (await db.Users.AnyAsync(u => u.Email == devEmail))
        {
            logger.LogInformation("[Seed] Dev user already exists — skipping.");
            return;
        }

        logger.LogInformation("[Seed] Creating dev user and company...");

        var now = DateTime.UtcNow;

        var company = new Company
        {
            Id = Guid.NewGuid(),
            Name = "PROHUB Dev",
            Slug = "prohub-dev",
            Description = "Auto-seeded development company",
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            IsDeleted = false
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = devEmail,
            Name = "Admin",
            PasswordHash = auth.HashPassword(devPassword),
            Role = "owner",
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            IsDeleted = false
        };

        var membership = new Membership
        {
            Id = Guid.NewGuid(),
            CompanyId = company.Id,
            UserId = user.Id,
            Role = "owner",
            CreatedAtUtc = now
        };

        db.Companies.Add(company);
        db.Users.Add(user);
        db.Memberships.Add(membership);
        await db.SaveChangesAsync();

        logger.LogInformation(
            "[Seed] Done. Login → email: {Email} | password: {Password} | companyId: {CompanyId}",
            devEmail, devPassword, company.Id);
    }
}
