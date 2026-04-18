using Microsoft.EntityFrameworkCore;
using PROHUB.API.Server.Common;
using PROHUB.API.Server.Data;
using PROHUB.API.Server.Dtos;
using PROHUB.API.Server.Entities;

namespace PROHUB.API.Server.Endpoints;

public static class TagEndpoints
{
    public static IEndpointRouteBuilder MapTagEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/projects/{projectId:guid}/tags")
            .WithTags("Tags")
            .RequireAuthorization();

        group.MapGet("/", GetTags);
        group.MapPost("/", AddTag);
        group.MapDelete("/{tag}", DeleteTag);

        return app;
    }

    static async Task<IResult> GetTags(Guid projectId, ProhubDbContext db)
    {
        var exists = await db.Projects.AnyAsync(p => p.Id == projectId && !p.IsDeleted);
        if (!exists)
            return Results.NotFound(ApiResponse.Fail(AppErrors.NotFound("Project")));

        var tags = await db.ProjectTags
            .Where(t => t.ProjectId == projectId)
            .OrderBy(t => t.Tag)
            .Select(t => new TagDto(t.Id, t.ProjectId, t.Tag))
            .ToListAsync();

        return Results.Ok(ApiResponse<List<TagDto>>.Success(tags));
    }

    static async Task<IResult> AddTag(Guid projectId, AddTagRequest req, ProhubDbContext db)
    {
        if (string.IsNullOrWhiteSpace(req.Tag))
            return Results.BadRequest(ApiResponse.Fail("Tag is required."));

        var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == projectId && !p.IsDeleted);
        if (project is null)
            return Results.NotFound(ApiResponse.Fail(AppErrors.NotFound("Project")));

        var normalized = req.Tag.Trim().ToLowerInvariant();
        var exists = await db.ProjectTags.AnyAsync(t => t.ProjectId == projectId && t.Tag == normalized);
        if (exists)
            return Results.Conflict(ApiResponse.Fail(AppErrors.Conflict($"Tag '{normalized}' already exists.")));

        var tag = new ProjectTag
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Tag = normalized
        };

        db.ProjectTags.Add(tag);
        await db.SaveChangesAsync();

        return Results.Created(
            $"/api/projects/{projectId}/tags",
            ApiResponse<TagDto>.Success(new(tag.Id, tag.ProjectId, tag.Tag)));
    }

    static async Task<IResult> DeleteTag(Guid projectId, string tag, ProhubDbContext db)
    {
        var normalized = tag.Trim().ToLowerInvariant();
        var entity = await db.ProjectTags
            .FirstOrDefaultAsync(t => t.ProjectId == projectId && t.Tag == normalized);

        if (entity is null)
            return Results.NotFound(ApiResponse.Fail(AppErrors.NotFound("Tag")));

        db.ProjectTags.Remove(entity);
        await db.SaveChangesAsync();

        return Results.Ok(ApiResponse.Success());
    }
}
