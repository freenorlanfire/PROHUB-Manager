using Microsoft.EntityFrameworkCore;
using PROHUB.API.Server.Common;
using PROHUB.API.Server.Data;
using PROHUB.API.Server.Dtos;
using PROHUB.API.Server.Entities;

namespace PROHUB.API.Server.Endpoints;

public static class StatusEndpoints
{
    public static IEndpointRouteBuilder MapStatusEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/projects/{projectId:guid}/status")
            .WithTags("Status")
            .RequireAuthorization();

        group.MapGet("/history", GetHistory);
        group.MapPost("/", AddStatus);

        return app;
    }

    static async Task<IResult> GetHistory(Guid projectId, ProhubDbContext db)
    {
        var exists = await db.Projects.AnyAsync(p => p.Id == projectId && !p.IsDeleted);
        if (!exists)
            return Results.NotFound(ApiResponse.Fail(AppErrors.NotFound("Project")));

        var entries = await db.ProjectStatusEntries
            .Where(s => s.ProjectId == projectId)
            .OrderByDescending(s => s.CreatedAtUtc)
            .Select(s => new StatusEntryDto(s.Id, s.ProjectId, s.Status, s.Note, s.CreatedBy, s.CreatedAtUtc))
            .ToListAsync();

        return Results.Ok(ApiResponse<List<StatusEntryDto>>.Success(entries));
    }

    static async Task<IResult> AddStatus(Guid projectId, AddStatusRequest req, ProhubDbContext db)
    {
        if (string.IsNullOrWhiteSpace(req.Status))
            return Results.BadRequest(ApiResponse.Fail("Status is required."));

        var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == projectId && !p.IsDeleted);
        if (project is null)
            return Results.NotFound(ApiResponse.Fail(AppErrors.NotFound("Project")));

        var entry = new ProjectStatusEntry
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Status = req.Status.Trim(),
            Note = req.Note?.Trim(),
            CreatedBy = req.CreatedBy?.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };

        // Update current status on project
        project.Status = req.Status.Trim();
        project.UpdatedAtUtc = DateTime.UtcNow;

        db.ProjectStatusEntries.Add(entry);
        await db.SaveChangesAsync();

        return Results.Created(
            $"/api/projects/{projectId}/status/history",
            ApiResponse<StatusEntryDto>.Success(
                new(entry.Id, entry.ProjectId, entry.Status, entry.Note, entry.CreatedBy, entry.CreatedAtUtc)));
    }
}
