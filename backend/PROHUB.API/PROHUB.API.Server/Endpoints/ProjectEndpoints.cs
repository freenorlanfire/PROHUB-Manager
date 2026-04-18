using Microsoft.EntityFrameworkCore;
using PROHUB.API.Server.Common;
using PROHUB.API.Server.Data;
using PROHUB.API.Server.Dtos;
using PROHUB.API.Server.Entities;

namespace PROHUB.API.Server.Endpoints;

public static class ProjectEndpoints
{
    public static IEndpointRouteBuilder MapProjectEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/projects")
            .WithTags("Projects")
            .RequireAuthorization();

        group.MapGet("/", GetProjects);
        group.MapPost("/", CreateProject);
        group.MapGet("/{id:guid}", GetProject);
        group.MapPut("/{id:guid}", UpdateProject);
        group.MapDelete("/{id:guid}", DeleteProject);
        group.MapPost("/{id:guid}/archive", ArchiveProject);

        return app;
    }

    static async Task<IResult> GetProjects(HttpRequest request, ProhubDbContext db, Guid? companyId)
    {
        var cid = companyId ?? GetCompanyIdFromHeader(request);
        if (cid is null)
            return Results.BadRequest(ApiResponse.Fail(AppErrors.CompanyRequired));

        var list = await db.Projects
            .Where(p => p.CompanyId == cid && !p.IsDeleted)
            .OrderByDescending(p => p.UpdatedAtUtc)
            .Select(p => ToDto(p))
            .ToListAsync();

        return Results.Ok(ApiResponse<List<ProjectDto>>.Success(list));
    }

    static async Task<IResult> GetProject(Guid id, ProhubDbContext db)
    {
        var p = await db.Projects.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (p is null)
            return Results.NotFound(ApiResponse.Fail(AppErrors.NotFound("Project")));

        return Results.Ok(ApiResponse<ProjectDto>.Success(ToDto(p)));
    }

    static async Task<IResult> CreateProject(CreateProjectRequest req, ProhubDbContext db)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            return Results.BadRequest(ApiResponse.Fail("Name is required."));
        if (req.CompanyId == Guid.Empty)
            return Results.BadRequest(ApiResponse.Fail("CompanyId is required."));

        var company = await db.Companies.FirstOrDefaultAsync(c => c.Id == req.CompanyId && !c.IsDeleted);
        if (company is null)
            return Results.NotFound(ApiResponse.Fail(AppErrors.NotFound("Company")));

        var project = new Project
        {
            Id = Guid.NewGuid(),
            CompanyId = req.CompanyId,
            Name = req.Name.Trim(),
            Slug = Slugify(req.Name),
            Description = req.Description?.Trim(),
            Status = "active",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            IsDeleted = false
        };

        db.Projects.Add(project);
        await db.SaveChangesAsync();

        return Results.Created($"/api/projects/{project.Id}", ApiResponse<ProjectDto>.Success(ToDto(project)));
    }

    static async Task<IResult> UpdateProject(Guid id, UpdateProjectRequest req, ProhubDbContext db)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            return Results.BadRequest(ApiResponse.Fail("Name is required."));

        var p = await db.Projects.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (p is null)
            return Results.NotFound(ApiResponse.Fail(AppErrors.NotFound("Project")));

        p.Name = req.Name.Trim();
        p.Slug = Slugify(req.Name);
        p.Description = req.Description?.Trim();
        if (!string.IsNullOrWhiteSpace(req.Status))
            p.Status = req.Status;
        p.UpdatedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return Results.Ok(ApiResponse<ProjectDto>.Success(ToDto(p)));
    }

    static async Task<IResult> DeleteProject(Guid id, ProhubDbContext db)
    {
        var p = await db.Projects.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (p is null)
            return Results.NotFound(ApiResponse.Fail(AppErrors.NotFound("Project")));

        p.IsDeleted = true;
        p.DeletedAtUtc = DateTime.UtcNow;
        p.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Results.Ok(ApiResponse.Success());
    }

    static async Task<IResult> ArchiveProject(Guid id, ProhubDbContext db)
    {
        var p = await db.Projects.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (p is null)
            return Results.NotFound(ApiResponse.Fail(AppErrors.NotFound("Project")));

        p.Status = "archived";
        p.ArchivedAtUtc = DateTime.UtcNow;
        p.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Results.Ok(ApiResponse<ProjectDto>.Success(ToDto(p)));
    }

    static ProjectDto ToDto(Project p) => new(
        p.Id, p.CompanyId, p.Name, p.Slug, p.Description,
        p.Status, p.CreatedAtUtc, p.UpdatedAtUtc, p.ArchivedAtUtc);

    static Guid? GetCompanyIdFromHeader(HttpRequest request) =>
        request.Headers.TryGetValue("X-Company-Id", out var v) && Guid.TryParse(v, out var g) ? g : null;

    static string Slugify(string name) =>
        System.Text.RegularExpressions.Regex.Replace(
            name.Trim().ToLowerInvariant(), @"[^a-z0-9]+", "-").Trim('-');
}
