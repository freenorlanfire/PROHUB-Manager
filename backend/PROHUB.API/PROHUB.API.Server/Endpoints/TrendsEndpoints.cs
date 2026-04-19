using PROHUB.API.Server.Common;
using PROHUB.API.Server.Data;
using PROHUB.API.Server.Dtos;
using PROHUB.API.Server.Services;
using Microsoft.EntityFrameworkCore;

namespace PROHUB.API.Server.Endpoints;

public static class TrendsEndpoints
{
    public static IEndpointRouteBuilder MapTrendsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/trends")
            .WithTags("Trends")
            .RequireAuthorization();

        // GET /api/trends?tags=react,dotnet,typescript
        group.MapGet("/", GetTrends);

        // GET /api/trends/project/{projectId} — auto-uses project tags
        group.MapGet("/project/{projectId:guid}", GetProjectTrends);

        return app;
    }

    static async Task<IResult> GetTrends(
        TrendsService trends,
        string? tags,
        CancellationToken ct)
    {
        var tagList = (tags ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(t => t.ToLowerInvariant())
            .Distinct()
            .Take(5)
            .ToArray();

        if (tagList.Length == 0)
            tagList = ["software", "programming", "devops"];

        var items = await trends.GetTrendsAsync(tagList, ct);

        return Results.Ok(ApiResponse<TrendsResult>.Success(new(
            items,
            tagList,
            DateTime.UtcNow.ToString("O"))));
    }

    static async Task<IResult> GetProjectTrends(
        Guid projectId,
        TrendsService trends,
        ProhubDbContext db,
        CancellationToken ct)
    {
        var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == projectId && !p.IsDeleted, ct);
        if (project is null)
            return Results.NotFound(ApiResponse.Fail(AppErrors.NotFound("Project")));

        var projectTags = await db.ProjectTags
            .Where(t => t.ProjectId == projectId)
            .Select(t => t.Tag)
            .ToListAsync(ct);

        // Also include project status as a keyword
        var tagList = projectTags
            .Concat([project.Name.Split(' ')[0].ToLowerInvariant()])
            .Select(t => t.ToLowerInvariant())
            .Distinct()
            .Take(5)
            .ToArray();

        if (tagList.Length == 0)
            tagList = ["software", "programming"];

        var items = await trends.GetTrendsAsync(tagList, ct);

        return Results.Ok(ApiResponse<TrendsResult>.Success(new(
            items,
            tagList,
            DateTime.UtcNow.ToString("O"))));
    }
}
