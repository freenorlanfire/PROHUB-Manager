using Microsoft.EntityFrameworkCore;
using PROHUB.API.Server.Common;
using PROHUB.API.Server.Data;
using PROHUB.API.Server.Dtos;
using PROHUB.API.Server.Entities;

namespace PROHUB.API.Server.Endpoints;

public static class IntegrationEndpoints
{
    public static IEndpointRouteBuilder MapIntegrationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/projects/{projectId:guid}/integrations")
            .WithTags("Integrations")
            .RequireAuthorization();

        group.MapGet("/", GetIntegrations);
        group.MapPut("/", UpsertIntegrations);

        return app;
    }

    static async Task<IResult> GetIntegrations(Guid projectId, ProhubDbContext db)
    {
        var exists = await db.Projects.AnyAsync(p => p.Id == projectId && !p.IsDeleted);
        if (!exists)
            return Results.NotFound(ApiResponse.Fail(AppErrors.NotFound("Project")));

        var links = await db.IntegrationLinks
            .Where(l => l.ProjectId == projectId)
            .OrderBy(l => l.Type)
            .Select(l => new IntegrationLinkDto(l.Id, l.ProjectId, l.Type, l.Url, l.Label, l.UpdatedAtUtc))
            .ToListAsync();

        return Results.Ok(ApiResponse<List<IntegrationLinkDto>>.Success(links));
    }

    static async Task<IResult> UpsertIntegrations(
        Guid projectId, UpsertIntegrationsRequest req, ProhubDbContext db)
    {
        var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == projectId && !p.IsDeleted);
        if (project is null)
            return Results.NotFound(ApiResponse.Fail(AppErrors.NotFound("Project")));

        if (req.Links is null)
            return Results.BadRequest(ApiResponse.Fail(AppErrors.BadRequest));

        var now = DateTime.UtcNow;
        var existing = await db.IntegrationLinks
            .Where(l => l.ProjectId == projectId)
            .ToListAsync();

        foreach (var item in req.Links)
        {
            if (string.IsNullOrWhiteSpace(item.Type) || string.IsNullOrWhiteSpace(item.Url))
                continue;

            var link = existing.FirstOrDefault(l => l.Type == item.Type);
            if (link is null)
            {
                link = new IntegrationLink
                {
                    Id = Guid.NewGuid(),
                    ProjectId = projectId,
                    Type = item.Type,
                    CreatedAtUtc = now
                };
                db.IntegrationLinks.Add(link);
            }

            link.Url = item.Url.Trim();
            link.Label = item.Label?.Trim();
            link.UpdatedAtUtc = now;
        }

        await db.SaveChangesAsync();

        var result = await db.IntegrationLinks
            .Where(l => l.ProjectId == projectId)
            .OrderBy(l => l.Type)
            .Select(l => new IntegrationLinkDto(l.Id, l.ProjectId, l.Type, l.Url, l.Label, l.UpdatedAtUtc))
            .ToListAsync();

        return Results.Ok(ApiResponse<List<IntegrationLinkDto>>.Success(result));
    }
}
