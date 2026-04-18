namespace PROHUB.API.Server.Dtos;

public record IntegrationLinkDto(
    Guid Id,
    Guid ProjectId,
    string Type,
    string Url,
    string? Label,
    DateTime UpdatedAtUtc
);

/// <summary>
/// Upsert integration links for a project.
/// Types: repo | ci | staging | prod | docs
/// </summary>
public record UpsertIntegrationsRequest(
    List<IntegrationLinkUpsertItem> Links
);

public record IntegrationLinkUpsertItem(
    string Type,
    string Url,
    string? Label
);
