using Microsoft.EntityFrameworkCore;
using PROHUB.API.Server.Common;
using PROHUB.API.Server.Data;
using PROHUB.API.Server.Dtos;
using PROHUB.API.Server.Entities;

namespace PROHUB.API.Server.Endpoints;

public static class LinkEndpoints
{
    public static IEndpointRouteBuilder MapLinkEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/projects/{projectId:guid}/links")
            .WithTags("Links")
            .RequireAuthorization();

        group.MapGet("/", GetLinks);
        group.MapPost("/", AddLink);
        group.MapDelete("/{linkId:guid}", DeleteLink);

        return app;
    }

    static async Task<IResult> GetLinks(Guid projectId, ProhubDbContext db)
    {
        var exists = await db.Projects.AnyAsync(p => p.Id == projectId && !p.IsDeleted);
        if (!exists)
            return Results.NotFound(ApiResponse.Fail(AppErrors.NotFound("Project")));

        var links = await db.ProjectLinks
            .Where(l => l.ProjectId == projectId)
            .OrderBy(l => l.CreatedAtUtc)
            .Select(l => new ProjectLinkDto(l.Id, l.ProjectId, l.Label, l.Url, l.CreatedAtUtc))
            .ToListAsync();

        return Results.Ok(ApiResponse<List<ProjectLinkDto>>.Success(links));
    }

    static async Task<IResult> AddLink(Guid projectId, AddLinkRequest req, ProhubDbContext db)
    {
        if (string.IsNullOrWhiteSpace(req.Label) || string.IsNullOrWhiteSpace(req.Url))
            return Results.BadRequest(ApiResponse.Fail("Label and Url are required."));

        var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == projectId && !p.IsDeleted);
        if (project is null)
            return Results.NotFound(ApiResponse.Fail(AppErrors.NotFound("Project")));

        var link = new ProjectLink
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Label = req.Label.Trim(),
            Url = req.Url.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };

        db.ProjectLinks.Add(link);
        await db.SaveChangesAsync();

        return Results.Created(
            $"/api/projects/{projectId}/links",
            ApiResponse<ProjectLinkDto>.Success(
                new(link.Id, link.ProjectId, link.Label, link.Url, link.CreatedAtUtc)));
    }

    static async Task<IResult> DeleteLink(Guid projectId, Guid linkId, ProhubDbContext db)
    {
        var link = await db.ProjectLinks
            .FirstOrDefaultAsync(l => l.Id == linkId && l.ProjectId == projectId);

        if (link is null)
            return Results.NotFound(ApiResponse.Fail(AppErrors.NotFound("Link")));

        db.ProjectLinks.Remove(link);
        await db.SaveChangesAsync();

        return Results.Ok(ApiResponse.Success());
    }
}
