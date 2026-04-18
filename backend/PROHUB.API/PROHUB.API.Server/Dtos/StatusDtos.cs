namespace PROHUB.API.Server.Dtos;

public record StatusEntryDto(
    Guid Id,
    Guid ProjectId,
    string Status,
    string? Note,
    string? CreatedBy,
    DateTime CreatedAtUtc
);

public record AddStatusRequest(
    string Status,
    string? Note,
    string? CreatedBy
);
