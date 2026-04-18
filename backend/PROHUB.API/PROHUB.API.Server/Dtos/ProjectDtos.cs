namespace PROHUB.API.Server.Dtos;

public record ProjectDto(
    Guid Id,
    Guid CompanyId,
    string Name,
    string Slug,
    string? Description,
    string Status,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    DateTime? ArchivedAtUtc
);

public record CreateProjectRequest(
    Guid CompanyId,
    string Name,
    string? Description
);

public record UpdateProjectRequest(
    string Name,
    string? Description,
    string? Status
);
