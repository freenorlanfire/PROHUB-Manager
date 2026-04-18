using Microsoft.EntityFrameworkCore;
using PROHUB.API.Server.Common;
using PROHUB.API.Server.Data;
using PROHUB.API.Server.Dtos;
using PROHUB.API.Server.Entities;

namespace PROHUB.API.Server.Endpoints;

public static class ContextDocEndpoints
{
    public static IEndpointRouteBuilder MapContextDocEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/projects/{projectId:guid}/context-doc")
            .WithTags("ContextDoc")
            .RequireAuthorization();

        group.MapGet("/", GetDoc);
        group.MapPut("/", SaveDoc);
        group.MapGet("/versions", GetVersions);

        return app;
    }

    static async Task<IResult> GetDoc(Guid projectId, ProhubDbContext db)
    {
        var doc = await db.ContextDocs.FirstOrDefaultAsync(d => d.ProjectId == projectId);
        if (doc is null)
        {
            // Return empty doc — project exists but no content yet
            var projectExists = await db.Projects.AnyAsync(p => p.Id == projectId && !p.IsDeleted);
            if (!projectExists)
                return Results.NotFound(ApiResponse.Fail(AppErrors.NotFound("Project")));

            return Results.Ok(ApiResponse<ContextDocDto>.Success(
                new(Guid.Empty, projectId, "", DateTime.UtcNow, null)));
        }

        return Results.Ok(ApiResponse<ContextDocDto>.Success(
            new(doc.Id, doc.ProjectId, doc.Content, doc.UpdatedAtUtc, doc.UpdatedBy)));
    }

    static async Task<IResult> SaveDoc(Guid projectId, SaveContextDocRequest req, ProhubDbContext db)
    {
        var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == projectId && !p.IsDeleted);
        if (project is null)
            return Results.NotFound(ApiResponse.Fail(AppErrors.NotFound("Project")));

        var now = DateTime.UtcNow;
        var doc = await db.ContextDocs.FirstOrDefaultAsync(d => d.ProjectId == projectId);

        if (doc is null)
        {
            doc = new ContextDoc
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                Content = req.Content ?? "",
                UpdatedAtUtc = now,
                UpdatedBy = req.UpdatedBy
            };
            db.ContextDocs.Add(doc);
        }
        else
        {
            // Snapshot current content as a new version before overwriting
            var latestVersion = await db.ContextDocVersions
                .Where(v => v.ContextDocId == doc.Id)
                .MaxAsync(v => (int?)v.Version) ?? 0;

            var version = new ContextDocVersion
            {
                Id = Guid.NewGuid(),
                ContextDocId = doc.Id,
                ProjectId = projectId,
                Content = doc.Content,   // snapshot of previous content
                Version = latestVersion + 1,
                CreatedAtUtc = now,
                CreatedBy = req.UpdatedBy
            };
            db.ContextDocVersions.Add(version);

            doc.Content = req.Content ?? "";
            doc.UpdatedAtUtc = now;
            doc.UpdatedBy = req.UpdatedBy;
        }

        await db.SaveChangesAsync();

        return Results.Ok(ApiResponse<ContextDocDto>.Success(
            new(doc.Id, doc.ProjectId, doc.Content, doc.UpdatedAtUtc, doc.UpdatedBy)));
    }

    static async Task<IResult> GetVersions(Guid projectId, ProhubDbContext db)
    {
        var doc = await db.ContextDocs.FirstOrDefaultAsync(d => d.ProjectId == projectId);
        if (doc is null)
            return Results.Ok(ApiResponse<List<ContextDocVersionDto>>.Success([]));

        var versions = await db.ContextDocVersions
            .Where(v => v.ContextDocId == doc.Id)
            .OrderByDescending(v => v.Version)
            .Select(v => new ContextDocVersionDto(
                v.Id, v.ContextDocId, v.ProjectId, v.Content,
                v.Version, v.CreatedAtUtc, v.CreatedBy))
            .ToListAsync();

        return Results.Ok(ApiResponse<List<ContextDocVersionDto>>.Success(versions));
    }
}
