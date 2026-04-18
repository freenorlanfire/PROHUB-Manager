namespace PROHUB.API.Server.Dtos;

public record ContextDocDto(
    Guid Id,
    Guid ProjectId,
    string Content,
    DateTime UpdatedAtUtc,
    string? UpdatedBy
);

public record ContextDocVersionDto(
    Guid Id,
    Guid ContextDocId,
    Guid ProjectId,
    string Content,
    int Version,
    DateTime CreatedAtUtc,
    string? CreatedBy
);

public record SaveContextDocRequest(
    string Content,
    string? UpdatedBy
);
