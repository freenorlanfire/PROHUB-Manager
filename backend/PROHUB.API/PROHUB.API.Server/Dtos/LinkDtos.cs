namespace PROHUB.API.Server.Dtos;

public record ProjectLinkDto(Guid Id, Guid ProjectId, string Label, string Url, DateTime CreatedAtUtc);

public record AddLinkRequest(string Label, string Url);
