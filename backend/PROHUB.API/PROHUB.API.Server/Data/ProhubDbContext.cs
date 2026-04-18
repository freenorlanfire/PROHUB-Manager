using Microsoft.EntityFrameworkCore;
using PROHUB.API.Server.Entities;

namespace PROHUB.API.Server.Data;

public class ProhubDbContext(DbContextOptions<ProhubDbContext> options) : DbContext(options)
{
    // DbSet names must match table names when converted to snake_case by EFCore.NamingConventions:
    // Companies -> companies, ProjectStatusEntries -> project_status_entries, etc.
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Membership> Memberships => Set<Membership>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectEnvironment> ProjectEnvironments => Set<ProjectEnvironment>();
    public DbSet<ProjectStatusEntry> ProjectStatusEntries => Set<ProjectStatusEntry>();
    public DbSet<ProjectTag> ProjectTags => Set<ProjectTag>();
    public DbSet<ContextDoc> ContextDocs => Set<ContextDoc>();
    public DbSet<ContextDocVersion> ContextDocVersions => Set<ContextDocVersion>();
    public DbSet<IntegrationLink> IntegrationLinks => Set<IntegrationLink>();
    public DbSet<ProjectLink> ProjectLinks => Set<ProjectLink>();
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Company
        modelBuilder.Entity<Company>(e =>
        {
            e.HasKey(c => c.Id);
            e.HasMany(c => c.Projects).WithOne(p => p.Company).HasForeignKey(p => p.CompanyId);
            e.HasMany(c => c.Memberships).WithOne(m => m.Company).HasForeignKey(m => m.CompanyId);
        });

        // User
        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.HasIndex(u => u.Email).IsUnique();
        });

        // Membership
        modelBuilder.Entity<Membership>(e =>
        {
            e.HasKey(m => m.Id);
            e.HasOne(m => m.User).WithMany(u => u.Memberships).HasForeignKey(m => m.UserId);
        });

        // Project
        modelBuilder.Entity<Project>(e =>
        {
            e.HasKey(p => p.Id);
            e.HasMany(p => p.StatusEntries).WithOne(s => s.Project).HasForeignKey(s => s.ProjectId);
            e.HasMany(p => p.IntegrationLinks).WithOne(i => i.Project).HasForeignKey(i => i.ProjectId);
            e.HasMany(p => p.Tags).WithOne(t => t.Project).HasForeignKey(t => t.ProjectId);
            e.HasMany(p => p.Links).WithOne(l => l.Project).HasForeignKey(l => l.ProjectId);
            e.HasOne(p => p.ContextDoc).WithOne(c => c.Project).HasForeignKey<ContextDoc>(c => c.ProjectId);
        });

        // ProjectEnvironment
        modelBuilder.Entity<ProjectEnvironment>(e =>
        {
            e.HasKey(pe => pe.Id);
            e.HasOne(pe => pe.Project).WithMany().HasForeignKey(pe => pe.ProjectId);
        });

        // ContextDoc
        modelBuilder.Entity<ContextDoc>(e =>
        {
            e.HasKey(c => c.Id);
            e.HasMany(c => c.Versions).WithOne(v => v.ContextDoc).HasForeignKey(v => v.ContextDocId);
        });

        // ContextDocVersion
        modelBuilder.Entity<ContextDocVersion>(e =>
        {
            e.HasKey(v => v.Id);
        });

        // ApiKey
        modelBuilder.Entity<ApiKey>(e =>
        {
            e.HasKey(k => k.Id);
            e.HasOne(k => k.Company).WithMany().HasForeignKey(k => k.CompanyId);
        });

        // RefreshToken
        modelBuilder.Entity<RefreshToken>(e =>
        {
            e.HasKey(r => r.Id);
            e.HasOne(r => r.User).WithMany(u => u.RefreshTokens).HasForeignKey(r => r.UserId);
        });

        // PasswordResetToken
        modelBuilder.Entity<PasswordResetToken>(e =>
        {
            e.HasKey(p => p.Id);
            e.HasOne(p => p.User).WithMany().HasForeignKey(p => p.UserId);
        });

        // AuditLog — no FK navigation (nullable references)
        modelBuilder.Entity<AuditLog>(e =>
        {
            e.HasKey(a => a.Id);
        });
    }
}
